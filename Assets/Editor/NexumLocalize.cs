using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sahnedeki sabit TMP metinlerini sözlüğe bağlar ve sözlüğü derler.
//
// Yaptıkları:
//  1. Her sahnedeki TMP metinlerini tarar. KOD TARAFINDAN YAZILANLARI atlar
//     (bir script alanından referans alınan metinler; bunların içeriği çalışma
//     zamanında değişiyor, sabit çeviriye bağlanamaz).
//  2. Kalanlara LocalizedText ekler ve hiyerarşiden türetilmiş bir anahtar verir.
//  3. Anahtar + Türkçe metni Assets/Data/Localization/tr.json'a işler; en.json'u
//     aynı anahtarlarla, Türkçe metinle ve "çevrilmedi" işaretiyle doldurur.
//  4. İki JSON'u çalışma zamanı asset'ine derler
//     (Assets/Resources/Localization/LocalizationTable.asset).
//
// IDEMPOTENT: ikinci çalıştırmada hiçbir şey değişmez. Anahtarı olan bileşenlere
// dokunulmaz, var olan çeviriler ezilmez.
//
// Menüden: NEXUM -> Metinleri Sözlüğe Bağla
// Batch mode: -executeMethod NexumLocalize.Run
public static class NexumLocalize
{
    private const string Tag = "NEXUM_LOCALIZE";
    private const string DataDir = "Assets/Data/Localization";
    private const string TrPath = DataDir + "/tr.json";
    private const string EnPath = DataDir + "/en.json";
    private const string ResourceDir = "Assets/Resources/Localization";
    private const string TablePath = ResourceDir + "/LocalizationTable.asset";
    private const string UntranslatedMark = "ÇEVRİLMEDİ";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    [MenuItem("NEXUM/Metinleri Sözlüğe Bağla")]
    public static void Run()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ResourceDir);

        Dictionary<string, string> turkish = ReadFile(TrPath).ToMap();
        int bound = 0, skipped = 0;

        foreach (string scenePath in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HashSet<Object> codeDriven = CollectScriptReferences(scene);

            bool dirty = false;
            string sceneKey = Path.GetFileNameWithoutExtension(scenePath).ToLowerInvariant();
            HashSet<string> usedKeys = new HashSet<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (codeDriven.Contains(text) || codeDriven.Contains(text.gameObject))
                    {
                        skipped++;
                        continue;
                    }

                    string content = text.text;
                    if (string.IsNullOrWhiteSpace(content) || content == "New Text")
                    {
                        skipped++;
                        continue;
                    }

                    LocalizedText loc = text.GetComponent<LocalizedText>();
                    if (loc == null)
                    {
                        loc = Undo.AddComponent<LocalizedText>(text.gameObject);
                        dirty = true;
                    }

                    if (string.IsNullOrEmpty(loc.key))
                    {
                        loc.key = UniqueKey(sceneKey, text.transform, usedKeys);
                        EditorUtility.SetDirty(loc);
                        dirty = true;
                    }
                    usedKeys.Add(loc.key);

                    // Sözlükte yoksa sahnedeki metin kaynak kabul edilir; varsa EZİLMEZ
                    if (!turkish.ContainsKey(loc.key)) turkish[loc.key] = content;
                    bound++;
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        // Koddan gelen anahtarlar da sözlükte olmalı
        foreach (KeyValuePair<string, string> pair in CodeStrings.All)
        {
            if (!turkish.ContainsKey(pair.Key)) turkish[pair.Key] = pair.Value;
        }

        WriteFiles(turkish);
        BakeTable(turkish);

        Debug.Log($"{Tag}: bağlanan metin {bound}, atlanan {skipped}, sözlük anahtarı {turkish.Count}");
        Debug.Log($"{Tag}: OK");
    }

    // ------------------------------------------------------------- yardımcılar

    /// <summary>Sahnedeki scriptlerin serialize edilmiş alanlarından işaret edilen nesneler.</summary>
    private static HashSet<Object> CollectScriptReferences(Scene scene)
    {
        HashSet<Object> referenced = new HashSet<Object>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour is LocalizedText) continue;

                SerializedObject so = new SerializedObject(behaviour);
                SerializedProperty prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != null) referenced.Add(prop.objectReferenceValue);
                }
            }
        }

        return referenced;
    }

    private static string UniqueKey(string sceneKey, Transform t, HashSet<string> used)
    {
        string parent = t.parent != null ? Slug(t.parent.name) : "root";
        string self = Slug(t.name);
        string baseKey = $"{sceneKey}.{parent}.{self}";

        string key = baseKey;
        int n = 2;
        while (used.Contains(key)) key = baseKey + "_" + n++;
        return key;
    }

    private static string Slug(string raw)
    {
        StringBuilder sb = new StringBuilder(raw.Length);
        foreach (char c in raw.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
            else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
        }
        return sb.ToString().Trim('_');
    }

    private static LocFile ReadFile(string path)
    {
        if (!File.Exists(path)) return new LocFile();
        try
        {
            return JsonUtility.FromJson<LocFile>(File.ReadAllText(path)) ?? new LocFile();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{Tag}: {path} okunamadı ({e.Message}); boş kabul edildi");
            return new LocFile();
        }
    }

    private static void WriteFiles(Dictionary<string, string> turkish)
    {
        List<string> keys = turkish.Keys.ToList();
        keys.Sort(System.StringComparer.Ordinal);

        LocFile tr = new LocFile { language = "tr", translated = true };
        foreach (string k in keys) tr.entries.Add(new LocFile.Pair(k, turkish[k]));

        // EN: var olan çeviriler korunur; olmayanlar Türkçe metinle doldurulup
        // "çevrilmedi" listesine yazılır (Faz 6'da çevrilecek).
        LocFile eskiEn = ReadFile(EnPath);
        Dictionary<string, string> english = eskiEn.ToMap();
        HashSet<string> zatenCevrilmemis = new HashSet<string>(eskiEn.untranslated ?? new List<string>());

        LocFile en = new LocFile { language = "en", translated = false };
        foreach (string k in keys)
        {
            bool cevrilmis = english.TryGetValue(k, out string value)
                             && !string.IsNullOrEmpty(value)
                             && !zatenCevrilmemis.Contains(k);

            en.entries.Add(new LocFile.Pair(k, cevrilmis ? value : turkish[k]));
            if (!cevrilmis) en.untranslated.Add(k);
        }

        File.WriteAllText(TrPath, JsonUtility.ToJson(tr, true));
        File.WriteAllText(EnPath, JsonUtility.ToJson(en, true));
        AssetDatabase.ImportAsset(TrPath);
        AssetDatabase.ImportAsset(EnPath);

        Debug.Log($"{Tag}: tr.json {tr.entries.Count} anahtar, en.json {en.entries.Count} anahtar " +
                  $"({en.untranslated.Count} tanesi {UntranslatedMark})");
    }

    private static void BakeTable(Dictionary<string, string> turkish)
    {
        LocFile en = ReadFile(EnPath);
        Dictionary<string, string> english = en.ToMap();

        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, TablePath);
        }

        List<string> keys = turkish.Keys.ToList();
        keys.Sort(System.StringComparer.Ordinal);

        table.entries.Clear();
        foreach (string k in keys)
        {
            table.entries.Add(new LocalizationTable.Entry
            {
                key = k,
                turkish = turkish[k],
                english = english.TryGetValue(k, out string e) ? e : turkish[k],
            });
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
    }
}
