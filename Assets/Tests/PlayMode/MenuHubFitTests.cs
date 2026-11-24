using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Hub metinlerinin kutularına sığması ve kartların ÇAKIŞMAMASI (1.7).
///
/// Nesne listesi ELLE YAZILMAZ: hub ağacı gezilir. İki bayrak hâli de
/// ölçülür — bugünkü hâl ve HEPSİ AÇIK. İkincisi olmadan Faz 2/3/5 bir
/// bayrağı açtığı gün yerleşimi yeniden ölçmek zorunda kalırdı.
///
/// Çakışma ölçümü satır içi kartlar arasında: aynı satırdaki iki kart
/// üst üste binerse dokunma da yanlış kartta olur.
/// </summary>
public class MenuHubFitTests
{
    private const string Tag = "NEXUM_HUB_FIT";

    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish, Loc.Language.English };

    /// <summary>
    /// BİLEREK iki satıra kırılan kutular: genişlik kuralı (kırma kapalı
    /// ölçülür) bunlarda anlamsız. Kart adları dar olduğu için
    /// "HAFTANIN DENEYİ" gibi başlıklar iki satır tasarlandı; taşma riski
    /// yükseklik kuralıyla ölçülüyor.
    /// </summary>
    private static readonly string[] CokSatirli =
    {
        "ModeRow/BoilingPointCard/Label",
        "ModeRow/EndlessLabCard/Label",
        "ModeRow/WeeklyExperimentCard/Label",
        "DualRow/DailyPuzzleCard/Label",
    };

    private string yedek;
    private MonoBehaviour hub;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        hub = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "MenuHub");
        Assert.IsNotNull(hub, "MenuHub yok");
    }

    [TearDown]
    public void GeriYukle()
    {
        Reflect.Call("HubFeatures", "Varsayilana");
        SaveService.ImportRaw(yedek);
    }

    [UnityTest]
    public IEnumerator HubMetinleriSigiyorVeKartlarCakismiyor()
    {
        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        Loc.Language onceki = Loc.Current;
        List<string> asanlar = new List<string>();
        List<string> tablo = new List<string>();
        int olculen = 0;

        foreach (bool tumBayraklar in new[] { false, true })
        {
            if (tumBayraklar) Reflect.Call("HubFeatures", "TumunuAc");
            else Reflect.Call("HubFeatures", "Varsayilana");

            string hal = tumBayraklar ? "hepsi" : "bugün";

            foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
            {
                LayoutCheck.SetCanvasSize(safeRoot, canvas);
                yield return null;

                foreach (Loc.Language dil in Diller)
                {
                    Loc.SetLanguage(dil);
                    Reflect.Call(hub, "Apply");
                    yield return null;
                    yield return null;

                    // --- metin sığması
                    foreach (TMP_Text tmp in hub.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (tmp == null || string.IsNullOrWhiteSpace(tmp.text)) continue;

                        olculen++;

                        // AD YETMEZ: hub'da bir sürü "Label" var. Hangi
                        // kutunun taştığı ancak yolla anlaşılıyor.
                        string yol = Yol(tmp.transform);

                        float y = LayoutCheck.FillRatio(tmp, tmp.text);
                        tablo.Add($"[{hal}] {canvas.x:F0}x{canvas.y:F0} [{dil}] {yol,-34} y%{y * 100f:F0}");

                        if (y > LayoutCheck.MaxFillRatio)
                            asanlar.Add($"[{hal}] [{dil}] {yol} yükseklik %{y * 100f:F0}");

                        if (CokSatirli.Contains(yol)) continue;

                        float w = LayoutCheck.FillRatioWidth(tmp, tmp.text);
                        if (w > LayoutCheck.MaxFillRatio)
                            asanlar.Add($"[{hal}] [{dil}] {yol} genişlik %{w * 100f:F0}");
                    }

                    // --- satır içi kart çakışması
                    asanlar.AddRange(Cakismalar(hal, canvas, dil));
                }
            }
        }

        Loc.SetLanguage(onceki);
        Reflect.Call("HubFeatures", "Varsayilana");

        Debug.Log($"{Tag}: ölçülen {olculen} metin, aşan/çakışan {asanlar.Count}, " +
                  $"sınır %{LayoutCheck.MaxFillRatio * 100f:F0}");
        foreach (string s in tablo.OrderByDescending(Doluluk).Take(6))
            Debug.Log($"{Tag}_EN_KOTU: {s}");

        Assert.IsEmpty(asanlar, "sığmayan/çakışan: " + string.Join(" | ", asanlar.Take(8)));
    }

    /// <summary>Aynı satırdaki etkin kartlar üst üste biniyor mu?</summary>
    private IEnumerable<string> Cakismalar(string hal, Vector2 canvas, Loc.Language dil)
    {
        RectTransform root = hub.transform as RectTransform;

        foreach (RectTransform row in root.GetComponentsInChildren<RectTransform>(false))
        {
            if (row.parent != root) continue;

            List<RectTransform> kartlar = new List<RectTransform>();
            foreach (Transform c in row)
            {
                RectTransform rt = c as RectTransform;
                if (rt != null && c.gameObject.activeSelf) kartlar.Add(rt);
            }
            if (kartlar.Count < 2) continue;

            for (int i = 0; i < kartlar.Count; i++)
            {
                for (int j = i + 1; j < kartlar.Count; j++)
                {
                    Vector4 a = LayoutCheck.LocalRect(root, kartlar[i]);
                    Vector4 b = LayoutCheck.LocalRect(root, kartlar[j]);
                    float alan = LayoutCheck.OverlapArea(a, b);
                    if (alan > 1f)
                    {
                        yield return $"[{hal}] {canvas.x:F0}x{canvas.y:F0} [{dil}] " +
                                     $"{row.name}: {kartlar[i].name} x {kartlar[j].name} " +
                                     $"{alan:F0} birim² çakışıyor";
                    }
                }
            }
        }
    }

    /// <summary>HubRoot'tan itibaren yol.</summary>
    private static string Yol(Transform t)
    {
        string yol = t.name;
        while (t.parent != null && t.parent.name != "HubRoot")
        {
            t = t.parent;
            yol = t.name + "/" + yol;
        }
        return yol;
    }

    private static float Doluluk(string satir)
    {
        int i = satir.LastIndexOf('%');
        return i < 0 ? 0f : float.TryParse(satir.Substring(i + 1), out float v) ? v : 0f;
    }
}
