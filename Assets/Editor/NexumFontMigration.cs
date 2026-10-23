using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Metin bileşenlerini Oxanium'a taşır.
///
/// NEDEN (ölçüm, docs/DESIGN_TOKENS.md §6):
///  - 72 bileşen TMP varsayılanı `LiberationSans SDF` kullanıyordu; marka
///    fontu hiç atanmamıştı.
///  - 22 bileşen Orbitron kullanıyordu; Orbitron'un TTF'inde `ş Ş ğ Ğ İ`
///    YOK — atlası yeniden üretmek çözmez, font elenir.
///
/// EŞLEME AĞIRLIK KORUR:
///   Orbitron-Bold / SemiBold -> Oxanium-SemiBold
///   Orbitron-Medium          -> Oxanium-Medium
///   LiberationSans SDF       -> Oxanium-Regular   (gövde varsayılanı)
///
/// Punto, konum ve boyut DEĞİŞMEZ. Font genişlikleri değiştiği için %85
/// sığma kuralı ayrıca ölçülür (VariableTextFitTests / LayoutCheck).
///
/// IDEMPOTENT: hedef font zaten atanmışsa dosyaya dokunmaz.
///
/// Menüden: NEXUM → Fontları Oxanium'a Taşı
/// Batch:   -executeMethod NexumFontMigration.Run
/// </summary>
public static class NexumFontMigration
{
    private const string Tag = "NEXUM_FONTMIG";

    private static readonly Dictionary<string, string> Esleme = new Dictionary<string, string>
    {
        { "Orbitron-Bold SDF",     "Assets/Fonts/Oxanium-SemiBold SDF.asset" },
        { "Orbitron-SemiBold SDF", "Assets/Fonts/Oxanium-SemiBold SDF.asset" },
        { "Orbitron-Medium SDF",   "Assets/Fonts/Oxanium-Medium SDF.asset" },
        { "LiberationSans SDF",    "Assets/Fonts/Oxanium-Regular SDF.asset" },
    };

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    [MenuItem("NEXUM/Fontları Oxanium'a Taşı")]
    public static void Run()
    {
        Dictionary<string, TMP_FontAsset> hedefler = new Dictionary<string, TMP_FontAsset>();
        foreach (var kv in Esleme)
        {
            TMP_FontAsset f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(kv.Value);
            if (f == null)
            {
                Debug.LogError($"{Tag}: hedef font yok -> {kv.Value}");
                return;
            }
            hedefler[kv.Key] = f;
        }

        int degisen = 0, dokunulmayan = 0;
        Dictionary<string, int> dokum = new Dictionary<string, int>();

        // --- sahneler
        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            int d = 0, t = 0;

            foreach (GameObject kok in scene.GetRootGameObjects())
            {
                foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (Tasi(tmp, hedefler, dokum)) d++;
                    else t++;
                }
            }

            if (d > 0) EditorSceneManager.SaveScene(scene);
            degisen += d;
            dokunulmayan += t;
            Debug.Log($"{Tag}: {Path.GetFileName(yol)} -> taşınan {d}, dokunulmayan {t}");
        }

        // --- prefab'lar
        int pd = 0, pt = 0;
        foreach (string yol in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories))
        {
            string normal = yol.Replace("\\", "/");
            GameObject kok = PrefabUtility.LoadPrefabContents(normal);
            bool degisti = false;

            foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Tasi(tmp, hedefler, dokum)) { degisti = true; pd++; }
                else pt++;
            }

            if (degisti) PrefabUtility.SaveAsPrefabAsset(kok, normal);
            PrefabUtility.UnloadPrefabContents(kok);
        }

        degisen += pd;
        dokunulmayan += pt;
        Debug.Log($"{Tag}: prefab'lar -> taşınan {pd}, dokunulmayan {pt}");

        foreach (var kv in dokum.OrderByDescending(k => k.Value))
        {
            Debug.Log($"{Tag}_ESLEME: {kv.Key} x{kv.Value}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: TOPLAM taşınan {degisen}, dokunulmayan {dokunulmayan}");
        Debug.Log($"{Tag}: OK");
    }

    private static bool Tasi(TMP_Text tmp, Dictionary<string, TMP_FontAsset> hedefler,
                             Dictionary<string, int> dokum)
    {
        if (tmp == null || tmp.font == null) return false;

        string ad = tmp.font.name;
        TMP_FontAsset yeni;
        if (!hedefler.TryGetValue(ad, out yeni)) return false;   // zaten Oxanium
        if (tmp.font == yeni) return false;

        // font ataması materyali de birlikte günceller (TMP varsayılan materyali)
        tmp.font = yeni;
        tmp.fontSharedMaterial = yeni.material;
        EditorUtility.SetDirty(tmp);

        string anahtar = ad + " -> " + yeni.name;
        dokum[anahtar] = dokum.TryGetValue(anahtar, out int n) ? n + 1 : 1;
        return true;
    }

    /// <summary>Doğrulama: Oxanium dışında font kalmış mı?</summary>
    [MenuItem("NEXUM/Font Geçişini Doğrula")]
    public static void Verify()
    {
        Dictionary<string, int> sayim = new Dictionary<string, int>();

        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            foreach (GameObject kok in scene.GetRootGameObjects())
            {
                foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp.font == null) continue;
                    sayim[tmp.font.name] = sayim.TryGetValue(tmp.font.name, out int n) ? n + 1 : 1;
                }
            }
        }

        foreach (string yol in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories))
        {
            GameObject kok = PrefabUtility.LoadPrefabContents(yol.Replace("\\", "/"));
            foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.font == null) continue;
                sayim[tmp.font.name] = sayim.TryGetValue(tmp.font.name, out int n) ? n + 1 : 1;
            }
            PrefabUtility.UnloadPrefabContents(kok);
        }

        int oxanimDisi = 0;
        foreach (var kv in sayim.OrderByDescending(k => k.Value))
        {
            bool ok = kv.Key.StartsWith("Oxanium");
            if (!ok) oxanimDisi += kv.Value;
            Debug.Log($"{Tag}_DOGRULA: {kv.Key} x{kv.Value} {(ok ? "" : "<-- OXANIUM DEĞİL")}");
        }

        Debug.Log($"{Tag}_DOGRULA: {(oxanimDisi == 0 ? "OK" : "SORUN " + oxanimDisi + " bileşen")}");
    }
}
