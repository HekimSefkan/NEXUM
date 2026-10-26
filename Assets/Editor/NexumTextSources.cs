using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ekranda GÖSTERİLEN tüm metinlerin tek toplama noktası.
///
/// Font atlası üretimi (<see cref="NexumFontAtlas"/>) ve atlas kapsam testi
/// (<c>AtlasCoversAllDisplayedTextTests</c>) AYNI listeyi kullanır; ikisi
/// ayrışırsa test yanlış güven verir.
///
/// NEDEN GEREKLİ: Orbitron'da `İ`, Oxanium'da `₅`–`₉` eksikti; ikisi de
/// ölçümle yakalandı. Üçüncüsü nadir görülen bir ekranda sessizce fallback'e
/// düşerse cihaz testinde fark edilmez.
///
/// Zengin metin etiketleri (&lt;color&gt;, &lt;sub&gt;, &lt;sprite&gt;, &lt;size&gt;…)
/// AYIKLANIR: onlar glif olarak çizilmez.
/// </summary>
public static class NexumTextSources
{
    private const string Tag = "NEXUM_TEXTSOURCE";

    /// <summary>TMP zengin metin etiketleri — glif olarak çizilmezler.</summary>
    private static readonly Regex Etiket = new Regex("<[^<>]{1,64}>", RegexOptions.Compiled);

    /// <summary>
    /// Benzersiz karakter kümesi. <paramref name="ornek"/> her karakter için
    /// nerede geçtiğini verir (rapor ve test mesajı için).
    /// </summary>
    public static string Topla(out Dictionary<char, string> ornek)
    {
        // Yerel fonksiyon out parametresine dokunamaz; yerel degiskenler
        // uzerinden calisilip sonunda disari verilir.
        Dictionary<char, string> kaynaklar = new Dictionary<char, string>();
        SortedSet<char> kume = new SortedSet<char>();

        void Ekle(string metin, string kaynak)
        {
            if (string.IsNullOrEmpty(metin)) return;

            string temiz = Etiket.Replace(metin, "");
            foreach (char c in temiz)
            {
                // Glif gerektirmeyenler: kontrol karakterleri ve sıfır
                // genişlikli boşluk. TMP bunları gliften bağımsız işler;
                // atlastan beklenmezler.
                if (char.IsControl(c) || c == '​' || c == '﻿') continue;
                if (kume.Add(c))
                {
                    string kisa = temiz.Length > 40 ? temiz.Substring(0, 40) + "…" : temiz;
                    kaynaklar[c] = kaynak + ": \"" + kisa + "\"";
                }
            }
        }

        // --- sözlük (iki dil, tüm değerler)
        foreach (string yol in new[] { "Assets/Data/Localization/tr.json",
                                       "Assets/Data/Localization/en.json" })
        {
            if (!File.Exists(yol)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(yol), "\"value\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
            {
                string ham = m.Groups[1].Value
                    .Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
                ham = Regex.Replace(ham, @"\\u([0-9a-fA-F]{4})",
                    g => ((char)System.Convert.ToInt32(g.Groups[1].Value, 16)).ToString());
                Ekle(ham, Path.GetFileName(yol));
            }
        }

        // --- element / bileşik verileri
        foreach (string guid in AssetDatabase.FindAssets("t:ElementData"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            ElementData d = AssetDatabase.LoadAssetAtPath<ElementData>(yol);
            if (d == null) continue;
            string k = "ElementData " + d.name;
            Ekle(d.elementName, k);
            Ekle(d.symbol, k);
            Ekle(d.recipe, k);
            Ekle(d.description, k);
        }

        // --- bilgi kartları
        foreach (string guid in AssetDatabase.FindAssets("t:ChemistryFact"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            ChemistryFact f = AssetDatabase.LoadAssetAtPath<ChemistryFact>(yol);
            if (f != null) Ekle(f.factText, "ChemistryFact " + f.name);
        }

        // --- quiz soruları ve şıkları
        foreach (string guid in AssetDatabase.FindAssets("t:ChemistryQuiz"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            ChemistryQuiz q = AssetDatabase.LoadAssetAtPath<ChemistryQuiz>(yol);
            if (q == null) continue;
            Ekle(q.questionText, "Quiz " + q.name);
            if (q.options == null) continue;
            foreach (string o in q.options) Ekle(o, "Quiz " + q.name);
        }

        // --- sahnelerde yazılı duran metinler + bölüm adları + tarif ipuçları
        //
        // Sahne AÇMAK Play modunda yasak (EditorSceneManager.OpenScene hata
        // atar) ve bu kaynak PlayMode testinden de çağrılıyor. Bu yüzden
        // Play modunda YÜKLÜ sahneler taranır; test iki sahneyi sırayla
        // yükleyip çağırarak aynı kapsamı elde eder.
        if (Application.isPlaying)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                SahneyiTara(SceneManager.GetSceneAt(i), Ekle);
            }
        }
        else
        {
            foreach (string yol in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" })
            {
                SahneyiTara(EditorSceneManager.OpenScene(yol, OpenSceneMode.Single), Ekle);
            }
        }

        // --- prefab'larda yazılı duran metinler
        // LoadPrefabContents Play modunda kullanılamaz; asset'i doğrudan
        // okumak yeterli (örneklemeye gerek yok).
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            GameObject kok = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (kok == null) continue;

            foreach (TMPro.TMP_Text t in kok.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                Ekle(t.text, Path.GetFileNameWithoutExtension(yol) + "/" + t.name);
            }
        }

        ornek = kaynaklar;
        return new string(kume.ToArray());
    }

    /// <summary>Bir sahnedeki metinleri, bölüm adlarını ve tarif ipuçlarını tarar.</summary>
    private static void SahneyiTara(Scene scene, System.Action<string, string> ekle)
    {
        string sahne = scene.name;

        foreach (GameObject kok in scene.GetRootGameObjects())
        {
            foreach (TMPro.TMP_Text t in kok.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                ekle(t.text, sahne + "/" + t.name);
            }

            LevelManager lm = kok.GetComponentInChildren<LevelManager>(true);
            if (lm != null && lm.levels != null)
            {
                foreach (LevelData l in lm.levels) ekle(l.levelName, sahne + " bölüm adı");
            }

            GridManager gm = kok.GetComponentInChildren<GridManager>(true);
            if (gm != null && gm.recipes != null)
            {
                foreach (MergeRecipe r in gm.recipes) ekle(r.scientificHint, sahne + " tarif ipucu");
            }
        }
    }

    /// <summary>Menüden çalıştırılınca kümeyi ve kaynaklarını yazdırır.</summary>
    [MenuItem("NEXUM/Metin Karakter Kümesini Listele")]
    public static void Listele()
    {
        Dictionary<char, string> ornek;
        string kume = Topla(out ornek);

        Debug.Log($"{Tag}: {kume.Length} benzersiz karakter");
        Debug.Log($"{Tag}_KUME: {kume}");

        string ozel = new string(kume.Where(c => c > 0x7E).ToArray());
        Debug.Log($"{Tag}_ASCII_DISI: {ozel}");
        foreach (char c in ozel)
        {
            Debug.Log($"{Tag}_NEREDE: '{c}' U+{((int)c):X4}  {ornek[c]}");
        }
    }
}
