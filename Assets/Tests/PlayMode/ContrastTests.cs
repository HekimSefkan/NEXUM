using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// KONTRAST, GERÇEK RENDER PİKSELİNDEN (2d-3a düzeltmesi, madde 4.5).
///
/// Token tablosu bir metnin okunup okunmadığını söyleyemez: okunabilirlik
/// metnin rengiyle değil ARKASINDAKİ YÜZEYLE olan farkıyla belirlenir.
/// Metal kiti geçişi yüzeyleri açıktan koyuya çevirdi, metin renkleri olduğu
/// yerde kaldı ve ana menüdeki TARİF DEFTERİ etiketi 1,12:1'e düştü.
///
/// Ölçüm <see cref="PixelProbe"/> ile: bütün TMP'ler kapatılıp bir kare
/// alınır (yüzey), her metnin dikdörtgeni o kareden örneklenir.
///
/// NOT: bir modal açıkken arkadaki HUD de karartmanın ALTINDA kalır; metin de
/// yüzey de aynı oranda kararır, oranları değişmez. Bu yüzden her panel
/// KENDİ ekranında, yalnızca kendi metinleriyle ölçülür.
/// </summary>
public class ContrastTests
{
    private const string Tag = "NEXUM_KONTRAST";
    private static readonly Vector2Int Boyut = new Vector2Int(1080, 1920);

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator MenuMetinleriOkunuyor()
    {
        yield return Olc("MainMenu");
    }

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator OyunMetinleriOkunuyor()
    {
        yield return Olc("Game");
    }

    private IEnumerator Olc(string sahne)
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene(sahne);
        yield return null;
        yield return new WaitForSeconds(1f);

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        if (safeRoot != null) LayoutCheck.SetCanvasSize(safeRoot, LayoutCheck.CanvasSizes[0]);
        yield return null;

        List<Transform> paneller = PixelProbe.Panels(canvas);
        List<string> hatalar = new List<string>();
        int olculen = 0, atlanan = 0;
        float enKotu = float.MaxValue;
        string enKotuAd = "-";

        // Ekranlar: HUD (panel yok) + her panel tek tek
        List<Transform> ekranlar = new List<Transform> { null };
        ekranlar.AddRange(paneller);

        foreach (Transform ekran in ekranlar)
        {
            foreach (Transform p in paneller) p.gameObject.SetActive(false);
            if (ekran != null)
            {
                ekran.gameObject.SetActive(true);
                ekran.localScale = Vector3.one;
            }

            yield return null;
            yield return null;

            Transform kapsam = ekran != null ? ekran : canvas;
            List<TMP_Text> metinler = kapsam.GetComponentsInChildren<TMP_Text>(false)
                .Where(t => t != null && t.rectTransform.rect.width > 1f
                            && t.rectTransform.rect.height > 1f)
                .ToList();
            if (metinler.Count == 0) continue;

            PixelProbe.Session oturum = PixelProbe.Begin(canvas.gameObject, Boyut, true);
            if (oturum == null) continue;
            yield return null;                       // canvas yeniden kurulumu kare sonunda
            yield return null;   // WaitForEndOfFrame batch mode'da güvenilir değil
            PixelProbe.Render(oturum);
            Assert.IsFalse(PixelProbe.IsBlank(oturum),
                "render boş geldi: süit -nographics ile koşulmuş olabilir");

            foreach (TMP_Text tmp in metinler)
            {
                Color? yuzey = PixelProbe.Sample(oturum, tmp.rectTransform);
                if (yuzey == null) { atlanan++; continue; }

                olculen++;
                float k = PixelProbe.Contrast(tmp.color, yuzey.Value);
                if (k < enKotu)
                {
                    enKotu = k;
                    enKotuAd = $"{(ekran == null ? "HUD" : ekran.name)} {PixelProbe.Path(tmp.transform)}";
                }

                if (k < PixelProbe.MinContrast)
                {
                    hatalar.Add($"{(ekran == null ? "HUD" : ekran.name)} " +
                                $"{PixelProbe.Path(tmp.transform)}: " +
                                $"{PixelProbe.Hex(tmp.color)} / yüzey {PixelProbe.Hex(yuzey.Value)} " +
                                $"= {k:F2}:1");
                }
            }

            PixelProbe.End(oturum);
        }

        Debug.Log($"{Tag}_SONUC: {sahne} ölçülen {olculen}, atlanan {atlanan}, " +
                  $"en kötü {enKotu:F2}:1 ({enKotuAd}), sorun {hatalar.Count}");
        foreach (string h in hatalar.Take(30)) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar.Take(8)));
    }
}
