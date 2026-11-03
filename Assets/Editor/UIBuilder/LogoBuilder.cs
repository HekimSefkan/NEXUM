using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Logo kilidi (N kutusu + EXUM) HER YERDE kendi sınırlarına göre ortalanır (3c/4).
///
/// İki ayrı kusur vardı:
///   1) Sprite'ın tuvalinde sağda 430 piksel boş pay vardı; içeriğin merkezi
///      tuvalin merkezinden %14 solda kalıyordu. Bir Image'ı ortalamak
///      logoyu ortalamıyordu — ortalanan şey boşluk dahil tuvaldi.
///      Pay <c>Tools/trim_logo.py</c> ile kırpıldı.
///   2) Logo Image'larının çoğunda <c>preserveAspect</c> KAPALIYDI: kilit
///      kutusunun oranına GERİLİYORDU (500x130 kutuda 4.27 oranlı sprite).
///
/// Bu kurucu ikinci kusuru kapatır: logoyu gösteren her Image aynı kurala
/// bağlanır — oran korunur, renk beyaz, tip Simple. Kutular DEĞİŞMEZ;
/// logo kutusunun içinde ortalanır.
///
/// Menüden: NEXUM → Logoyu Hizala
/// Batch:   -executeMethod LogoBuilder.Run
/// </summary>
public static class LogoBuilder
{
    private const string Tag = "NEXUM_LOGO";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    private static readonly string[] LogoPaths =
    {
        "Assets/Art/Figures/NEXUM_logo_metal.png",
        "Assets/Art/Figures/NEXUM_mark_metal.png",
    };

    [MenuItem("NEXUM/Logoyu Hizala")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        HashSet<Sprite> logolar = new HashSet<Sprite>();
        foreach (string yol in LogoPaths)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(yol);
            if (s != null) logolar.Add(s);
        }

        if (logolar.Count == 0)
        {
            Debug.LogError($"{Tag}: logo sprite'ı bulunamadı");
            return;
        }

        int duzeltilen = 0, dokunulmayan = 0;

        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            bool degisti = false;

            foreach (Image img in Object.FindObjectsOfType<Image>(true))
            {
                if (img == null || img.sprite == null || !logolar.Contains(img.sprite)) continue;

                bool fark = !img.preserveAspect
                            || img.type != Image.Type.Simple
                            || img.color != Color.white;
                if (!fark) { dokunulmayan++; continue; }

                Debug.Log($"{Tag}_DUZELTME: {Yol(img.transform)} " +
                          $"(preserveAspect {img.preserveAspect} -> True)");
                img.preserveAspect = true;
                img.type = Image.Type.Simple;
                img.color = Color.white;
                if (!dryRun) EditorUtility.SetDirty(img);
                duzeltilen++;
                degisti = true;
            }

            if (degisti && !dryRun) EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"{Tag}: düzeltilen {duzeltilen}, dokunulmayan {dokunulmayan} " +
                  $"({(dryRun ? "DRY-RUN" : "UYGULANDI")})");
        Debug.Log($"{Tag}: OK");
    }

    private static string Yol(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s.Replace("Canvas/SafeAreaRoot/", "").Replace("Canvas/", "");
    }
}
