using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// TEK ARKA PLAN (3c/5): bütün ekranlar aynı zemine basar.
///
/// Oyunda altı ayrı tam ekran arka plan görseli vardı; ekrandan ekrana
/// geçerken zemin değişiyordu. Bu test "aynı görsel kullanılsın" kuralını
/// ÖLÇER: elle yazılmış bir obje listesi yoktur — alt ağaç gezilir ve
/// EKRANI KAPLAYAN (iki eksende de >= %90) her Image aday sayılır.
///
/// İşleme katmanı (<c>BackgroundTreatment</c> altındakiler) aday değildir:
/// onlar zeminin ÜSTÜNDEKİ karartma / ızgara / şerit / vinyet katmanları.
///
/// Ayrıca ölçülür: zemin görselinin çözünürlüğü, Envelope Parent oranı ve
/// ekranın ortalama parlaklığı (gerçek render'dan, token tablosundan değil).
/// </summary>
public class BackgroundSourceTests
{
    private const string Tag = "NEXUM_ZEMIN";

    /// <summary>Ekranı kaplama eşiği: iki eksende de bu oranın üstü.</summary>
    private const float Kaplama = 0.9f;

    /// <summary>Tek kaynak yüksek çözünürlüklü olmalı (1440 cihazda 2560 piksel).</summary>
    private const int EnAzYukseklik = 2000;

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator MenuZeminiTekKaynak()
    {
        yield return Olc("MainMenu");
    }

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator OyunZeminiTekKaynak()
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

        // Kapalı paneller de ölçülür: onların zemini de aynı olmalı
        foreach (Transform p in PixelProbe.Panels(canvas))
        {
            p.gameObject.SetActive(true);
            p.localScale = Vector3.one;
        }
        yield return null;
        yield return null;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float cw = canvasRect.rect.width, ch = canvasRect.rect.height;

        List<Image> adaylar = new List<Image>();
        foreach (Image img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img == null || img.sprite == null) continue;
            if (IslemeKatmani(img.transform)) continue;
            if (KaydirmaParcasi(img)) continue;
            if (img.color.a < 0.02f) continue;          // görünmez katman zemin değildir

            // 9-slice bir görsel ÇERÇEVEDİR, zemin değil: tam ekrana
            // gerilmiş olsa bile kenarlığı taşımak için çizilir.
            if (img.type != Image.Type.Simple) continue;

            RectTransform rt = img.rectTransform;
            if (rt.rect.width < cw * Kaplama || rt.rect.height < ch * Kaplama) continue;

            adaylar.Add(img);
        }

        Assert.IsNotEmpty(adaylar, $"{sahne}: ekranı kaplayan hiçbir zemin bulunamadı");

        List<string> dokum = adaylar
            .Select(i => $"{PixelProbe.Path(i.transform)} -> {i.sprite.name}")
            .Distinct().ToList();
        foreach (string d in dokum) Debug.Log($"{Tag}_ZEMIN: {sahne}: {d}");

        List<Sprite> farkli = adaylar.Select(i => i.sprite).Distinct().ToList();
        Assert.AreEqual(1, farkli.Count,
            $"{sahne}: zemin TEK KAYNAK olmalı, {farkli.Count} farklı sprite var -> " +
            string.Join(", ", farkli.Select(s => s.name)));

        Sprite kaynak = farkli[0];
        Assert.GreaterOrEqual(kaynak.texture.height, EnAzYukseklik,
            $"{sahne}: zemin dokusu {kaynak.texture.width}x{kaynak.texture.height}, " +
            $"1440 genişlikte büyütülür");

        float oran = kaynak.rect.width / kaynak.rect.height;
        foreach (Image img in adaylar)
        {
            AspectRatioFitter arf = img.GetComponent<AspectRatioFitter>();
            Assert.IsNotNull(arf, $"{PixelProbe.Path(img.transform)}: AspectRatioFitter yok");
            Assert.IsTrue(arf.enabled, $"{PixelProbe.Path(img.transform)}: ARF kapalı");
            Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, arf.aspectMode,
                $"{PixelProbe.Path(img.transform)}: Envelope Parent değil");
            Assert.AreEqual(oran, arf.aspectRatio, 0.001f,
                $"{PixelProbe.Path(img.transform)}: ARF oranı sprite oranı değil");
        }

        Debug.Log($"{Tag}_SONUC: {sahne}: {adaylar.Count} zemin, tek kaynak {kaynak.name} " +
                  $"({kaynak.texture.width}x{kaynak.texture.height}, oran {oran:F6})");

        yield return Parlaklik(sahne, canvas.gameObject);
    }

    /// <summary>Ekranın ortalama parlaklığı — GERÇEK render'dan (0-255).</summary>
    private IEnumerator Parlaklik(string sahne, GameObject canvasGo)
    {
        // Yalnızca ana ekran ölçülür: açılan paneller kapatılır
        foreach (Transform p in PixelProbe.Panels(canvasGo.transform))
        {
            p.gameObject.SetActive(false);
        }
        yield return null;

        PixelProbe.Session s = PixelProbe.Begin(canvasGo, new Vector2Int(360, 640), false);
        yield return null;
        yield return null;
        PixelProbe.Render(s);

        if (PixelProbe.IsBlank(s))
        {
            PixelProbe.End(s);
            Assert.Fail($"{sahne}: BOŞ KARE — süit -nographics ile koşuyor olabilir");
            yield break;
        }

        Color[] px = s.frame.GetPixels();
        double toplam = 0;
        foreach (Color c in px) toplam += (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) * 255f;
        double ortalama = toplam / px.Length;

        PixelProbe.End(s);

        Debug.Log($"{Tag}_PARLAKLIK: {sahne}: ortalama {ortalama:F1}/255 " +
                  $"(karartma alfası {BackgroundTreatmentInfo.KarartmaAlfa:F2})");
    }

    /// <summary>
    /// ScrollRect'in KENDİ parçaları zemin değildir: görüntü alanı maskesi
    /// tam ekran olabilir ama gösterdiği şey içeriktir, zemin değil.
    /// </summary>
    private static bool KaydirmaParcasi(Image img)
    {
        if (img.GetComponent<Mask>() != null || img.GetComponent<RectMask2D>() != null) return true;
        if (img.GetComponent<ScrollRect>() != null) return true;

        Transform ebeveyn = img.transform.parent;
        return ebeveyn != null && ebeveyn.GetComponent<ScrollRect>() != null;
    }

    private static bool IslemeKatmani(Transform t)
    {
        while (t != null)
        {
            if (t.name == "BackgroundTreatment") return true;
            t = t.parent;
        }
        return false;
    }
}

/// <summary>
/// Kurucunun karartma parametresi test derlemesinden görünsün diye küçük
/// bir köprü: <c>BackgroundBuilder</c> Editor derlemesinde, test PlayMode
/// derlemesinde.
/// </summary>
public static class BackgroundTreatmentInfo
{
    public static float KarartmaAlfa
    {
        get
        {
            System.Type t = System.Type.GetType("BackgroundBuilder, Assembly-CSharp-Editor");
            if (t == null) return -1f;
            System.Reflection.FieldInfo f = t.GetField("KarartmaAlfa");
            return f == null ? -1f : (float)f.GetValue(null);
        }
    }
}
