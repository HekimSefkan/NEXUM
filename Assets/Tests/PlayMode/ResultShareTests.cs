using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Paylaşım görseli (1.6b).
///
/// Neyi koruyor:
///  1. <b>Görsel gerçekten üretiliyor ve 1080x1920.</b> Oyun canvas'ı
///     Screen Space Overlay; kamera onu GÖREMEZ. Canvas bir kareliğine
///     Screen Space Camera'ya alınmazsa RenderTexture BOŞ çıkar — ve boş
///     kare sessizce "çalışıyor" görünür.
///  2. <b>Kare boş değil.</b> <c>PixelProbe.IsBlank</c> ile ölçülür.
///  3. <b>KİŞİSEL VERİ YOK.</b> Kayda sentinel bir ad ve e-posta yazılır,
///     sonra canvas altındaki TÜM metinler taranır. Oyun sahnesinde
///     oyuncu adı/e-postası hiçbir yerde yazmamalı.
///  4. <b>Canvas eski hâline dönüyor.</b> Çizim bittikten sonra render
///     modu değişmiş kalırsa oyun ekranı bozulur.
/// </summary>
public class ResultShareTests
{
    private const string Tag = "NEXUM_PAYLASIM_TEST";

    private const string SentinelAd = "KimyagerSentinelAdi";
    private const string SentinelPosta = "sentinel@ornek-test.invalid";

    private string yedek;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SaveService.Data.selectedLevel = 0;
        SaveService.Data.playerName = SentinelAd;
        SaveService.Data.playerEmail = SentinelPosta;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.5f);
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    [UnityTest]
    public IEnumerator GorselUretiliyorVeBosDegil()
    {
        // Kart açık olsun: paylaşım bölüm sonunda yapılıyor.
        var card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");

        GameObject panel = card.transform.parent.gameObject;
        panel.SetActive(true);
        Reflect.Call(card, "Fill");
        yield return null;

        Canvas canvas = Object.FindObjectsOfType<Canvas>(true)
            .FirstOrDefault(c => c != null && c.isRootCanvas && c.name == "Canvas");
        Assert.IsNotNull(canvas, "kök Canvas yok");
        RenderMode onceki = canvas.renderMode;

        Texture2D png = Reflect.Call<Texture2D>("ResultShare", "Render");
        Assert.IsNotNull(png, "görsel üretilemedi");

        try
        {
            int w = Reflect.Prop<int>("ResultShare", "Width");
            int h = Reflect.Prop<int>("ResultShare", "Height");

            Debug.Log($"{Tag}: {png.width}x{png.height} (beklenen {w}x{h})");
            Assert.AreEqual(w, png.width, "genişlik 1080 değil");
            Assert.AreEqual(h, png.height, "yükseklik 1920 değil");

            // Boş kare muhafızı: overlay canvas kameraya alınmazsa burası düşer
            Color32[] pikseller = png.GetPixels32();
            int farkli = pikseller.Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Take(64).Count();
            float ortalama = pikseller.Average(p => (p.r + p.g + p.b) / 3f);

            Debug.Log($"{Tag}: {farkli} farklı renk (ilk 64'e kadar), ortalama parlaklık {ortalama:F1}");
            Assert.Greater(farkli, 8, "kare neredeyse tek renk — boş render (canvas kameraya alınmadı mı?)");
            Assert.Greater(ortalama, 3f, "kare kapkara");

            // Kaydetme de çalışmalı
            string yol = Reflect.Call<string>("ResultShare", "Save", png);
            Assert.IsFalse(string.IsNullOrEmpty(yol), "görsel diske yazılamadı");
            Assert.IsTrue(File.Exists(yol), "dosya yok: " + yol);

            long boyut = new FileInfo(yol).Length;
            Debug.Log($"{Tag}: dosya {boyut} bayt -> {yol}");
            Assert.Greater(boyut, 10000, "PNG şüpheli biçimde küçük");
        }
        finally
        {
            Object.DestroyImmediate(png);
        }

        Assert.AreEqual(onceki, canvas.renderMode,
            "canvas render modu eski hâline dönmedi; oyun ekranı bozulur");
    }

    [UnityTest]
    public IEnumerator GorseldeKisiselVeriYok()
    {
        Assert.AreEqual(SentinelAd, SaveService.Data.playerName, "sentinel ad kayda yazılmadı");
        Assert.AreEqual(SentinelPosta, SaveService.Data.playerEmail, "sentinel e-posta yazılmadı");

        var card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");

        GameObject panel = card.transform.parent.gameObject;
        panel.SetActive(true);
        Reflect.Call(card, "Fill");
        yield return null;

        Canvas canvas = Object.FindObjectsOfType<Canvas>(true)
            .FirstOrDefault(c => c != null && c.isRootCanvas && c.name == "Canvas");
        Assert.IsNotNull(canvas, "kök Canvas yok");

        // Görselde ne varsa canvas altındaki GÖRÜNÜR metinlerdir.
        int taranan = 0;
        var sizinti = new System.Collections.Generic.List<string>();

        foreach (TMP_Text t in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (!t.enabled) continue;

            taranan++;
            string metin = t.text ?? string.Empty;
            if (metin.Contains(SentinelAd)) sizinti.Add($"ad -> {Yol(t.transform)}");
            if (metin.Contains(SentinelPosta)) sizinti.Add($"e-posta -> {Yol(t.transform)}");
            if (metin.Contains("@")) sizinti.Add($"'@' işareti -> {Yol(t.transform)}: \"{metin}\"");
        }

        Debug.Log($"{Tag}: {taranan} görünür metin tarandı, sızıntı {sizinti.Count}");
        Assert.Greater(taranan, 5, "hiç metin taranmadı, ölçüm anlamsız");
        Assert.IsEmpty(sizinti, "paylaşım görselinde kişisel veri: " + string.Join(" | ", sizinti));
    }

    private static string Yol(Transform t)
    {
        string yol = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            yol = t.name + "/" + yol;
        }
        return yol;
    }
}
