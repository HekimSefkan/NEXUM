using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Ayarlar panelinin CAM KART üzerine oturup oturmadığını 4 canvas boyutunda ölçer.
//
// Ölçüm kodu burada değil, ortak yardımcıda: Assets/Tests/PlayMode/LayoutCheck.cs.
// Her yerleşim testi oradan geçer, kopya ölçüm kodu kalmaz.
public class SettingsLayoutTests
{
    private const string Tag = "NEXUM_SETTINGS";

    // Cam kart sprite'ı (SettingsPanel_.png) ve çerçevenin iç kenarları
    private const float SpriteW = 796f;
    private const float SpriteH = 1280f;
    private const float InnerLeft = 100f / SpriteW;
    private const float InnerRight = 690f / SpriteW;
    private const float InnerTop = 80f / SpriteH;
    private const float InnerBottom = 1190f / SpriteH;

    private static readonly string[] Content =
    {
        "Image", "Text (TMP)", "GameModeText", "ModeContainer",
        "AudioTitle", "MusicRow", "SfxRow",
        "LearningTitle", "FlashcardsRow", "HintsRow", "ResetProgressButton",
    };

    private RectTransform safeRoot;
    private RectTransform card;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");
        menu.GetType().GetMethod("OpenSettingsPanel", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(menu, null);
        yield return new WaitForSeconds(1f);

        safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        card = LayoutCheck.Find("SafeAreaRoot/SettingsPanel/GlassBackground") as RectTransform;
        Assert.IsNotNull(card, "GlassBackground yok");
    }

    [UnityTest]
    public IEnumerator AyarlarKartinIcindeKaliyor()
    {
        List<string> hatalar = new List<string>();

        foreach (Vector2 size in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, size);
            yield return null;
            yield return null;

            string ekran = $"{size.x:F0}x{size.y:F0}";
            Vector4 inner = LayoutCheck.AspectFittedInner(
                LayoutCheck.LocalRect(safeRoot, card),
                SpriteW, SpriteH, InnerLeft, InnerRight, InnerTop, InnerBottom);

            Debug.Log($"{Tag}_KART: {ekran} | ic alan x {inner.x:F0}..{inner.z:F0} y {inner.y:F0}..{inner.w:F0}");

            List<RectTransform> kardesler = new List<RectTransform>();

            foreach (string name in Content)
            {
                Transform t = FindContent(name);
                if (t == null) { Debug.Log($"{Tag}_SATIR: {ekran} | {name,-20} YOK"); continue; }

                RectTransform rt = t as RectTransform;
                kardesler.Add(rt);

                Vector4 r = LayoutCheck.LocalRect(safeRoot, rt);
                float tasma = LayoutCheck.Overflow(r, inner);

                string tmpBilgi = "";
                TMP_Text tmp = t.GetComponent<TMP_Text>();
                if (tmp != null && LayoutCheck.IsClipped(tmp, out tmpBilgi))
                {
                    hatalar.Add($"{ekran} {name} metin kirpiliyor");
                }

                Debug.Log($"{Tag}_SATIR: {ekran} | {LayoutCheck.Describe(name, r)} " +
                          $"| tasma {tasma,6:F0} | {tmpBilgi}");

                if (tasma > 1f) hatalar.Add($"{ekran} {name} kart disina {tasma:F0} birim");
            }

            foreach (string cakisma in LayoutCheck.FindOverlaps(safeRoot, kardesler))
            {
                hatalar.Add($"{ekran} cakisma: {cakisma}");
            }
        }

        Debug.Log($"{Tag}_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    private Transform FindContent(string name)
    {
        Transform kart = LayoutCheck.Find("SafeAreaRoot/SettingsPanel/GlassBackground");
        if (kart == null) return null;

        Transform icerik = kart.Find("CardContent");
        if (icerik != null)
        {
            Transform t = icerik.Find(name);
            if (t != null) return t;
        }
        return kart.Find(name);
    }
}
