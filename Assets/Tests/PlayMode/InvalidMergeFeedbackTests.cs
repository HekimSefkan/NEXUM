using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// GEÇERSİZ BİRLEŞTİRME GERİ BİLDİRİMİ (Faz 1 / 1.3).
///
/// Tetikleme kuralı dört şey birden söylüyor; dördü de ayrı ayrı ölçülür:
///   · hamle SIFIR birleşme ürettiyse ÇIKAR,
///   · en az bir birleşme olduysa ÇIKMAZ,
///   · hiç taş hareket etmediyse ÇIKMAZ,
///   · aynı çift arka arkaya gelirse TEKRARLAMAZ.
///
/// Ayrıca mod/ayar bağlılığı (sınav kapalı, mentor ipuçları kapalı, serbest
/// açık) ve bandın katalizör göstergesi, katalizör etiketi ve sistem
/// bildirimiyle çakışmaması.
/// </summary>
public class InvalidMergeFeedbackTests
{
    private const string Tag = "NEXUM_GECERSIZ";

    /// <summary>İçerik sayılma eşiği: nesnenin kendi en parlak pikselinin oranı.</summary>
    private const float IcerikOrani = 0.25f;

    private MonoBehaviour grid;
    private MonoBehaviour feedback;
    private List<Transform> cells;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);
        SaveService.Data.mentorHintsOn = 1;
        SaveService.Data.gameMode = 0;

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        feedback = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "InvalidMergeFeedback");

        Assert.IsNotNull(grid, "GridManager yok");
        Assert.IsNotNull(feedback, "InvalidMergeFeedback sahnede kurulu değil");

        cells = (List<Transform>)Field(grid, "cells").GetValue(grid);
        Assert.AreEqual(16, cells.Count, "grid 16 hücre olmalı");

        Field(grid, "currentGameMode").SetValue(grid, 0);
    }

    // ------------------------------------------------------------ tetikleme

    [UnityTest]
    public IEnumerator SifirBirlesmedeCikar()
    {
        TahtaKur("H", "Fe");
        yield return Kaydir(Vector2.right);

        Assert.IsTrue(Gorundu(), "sıfır birleşme üreten hamlede mesaj çıkmalıydı");
        StringAssert.Contains("Fe", Metin(), "mesajda itilen çiftin sembolleri olmalı");
        Debug.Log($"{Tag}_MESAJ: {Metin()}");
    }

    [UnityTest]
    public IEnumerator BirlesmeVarsaCikmaz()
    {
        // H + H -> H2 (tarif tablosunda var): hamle birleşme üretir
        TahtaKur("H", "H");
        yield return Kaydir(Vector2.right);

        Assert.IsFalse(Gorundu(), "birleşme olduğunda mesaj ÇIKMAMALI");
    }

    [UnityTest]
    public IEnumerator HicTasHareketEtmediyseCikmaz()
    {
        // İki taş zaten sağ duvarda: sağa kaydırmak hiçbir şeyi oynatmaz
        Temizle();
        Koy(14, "H");
        Koy(15, "Fe");
        yield return Kaydir(Vector2.right);

        Assert.IsFalse(Gorundu(), "hiç taş hareket etmediyse mesaj ÇIKMAMALI");
    }

    [UnityTest]
    public IEnumerator AyniCiftArkaArkayaTekrarlamaz()
    {
        TahtaKur("H", "Fe");
        yield return Kaydir(Vector2.right);
        Assert.IsTrue(Gorundu(), "ilk gösterim olmalıydı");

        Sifirla();
        Rapor(Vector2.right);          // aynı çift, aynı yön
        yield return null;

        Assert.IsFalse(Gorundu(), "aynı çift arka arkaya TEKRARLAMAMALI");
        Debug.Log($"{Tag}_TEKRAR: aynı çift ikinci kez bastırıldı");
    }

    // ------------------------------------------------------------ mod/ayar

    [UnityTest]
    public IEnumerator SinavModundaCikmaz()
    {
        Field(grid, "currentGameMode").SetValue(grid, 1);
        TahtaKur("H", "Fe");
        yield return Kaydir(Vector2.right);

        Assert.IsFalse(Gorundu(), "sınav modunda yardım yok");
    }

    [UnityTest]
    public IEnumerator MentorIpuclariKapaliykenCikmaz()
    {
        SaveService.Data.mentorHintsOn = 0;
        TahtaKur("H", "Fe");
        yield return Kaydir(Vector2.right);

        Assert.IsFalse(Gorundu(), "mentor ipuçları kapalıyken mesaj çıkmamalı");
        SaveService.Data.mentorHintsOn = 1;
    }

    [UnityTest]
    public IEnumerator SerbestModdaCikar()
    {
        Field(grid, "currentGameMode").SetValue(grid, 2);
        TahtaKur("H", "Fe");
        yield return Kaydir(Vector2.right);

        Assert.IsTrue(Gorundu(), "serbest modda mesaj çıkmalı");
    }

    // ------------------------------------------------------------ çakışma

    /// <summary>
    /// Bant × katalizör göstergesi × katalizör etiketi × sistem bildirimi:
    /// kesişim SIFIR olmalı. Ölçüm GERÇEK RENDER'dan yapılır — katalizörün
    /// dekoratif çerçevesinin rect'i çiziminden çok daha büyük olduğu için
    /// rect karşılaştırmak yanıltırdı.
    /// </summary>
    [UnityTest]
    [Timeout(900000)]
    public IEnumerator BantlarCakismiyor()
    {
        TahtaKur("H", "Fe");
        yield return Kaydir(Vector2.right);

        Debug.Log($"{Tag}_DURUM: satır={(Satir() == null ? "YOK" : "var")} " +
                  $"metin=\"{Metin()}\" mentor={SaveService.Data.mentorHintsOn} " +
                  $"mod={Field(grid, "currentGameMode").GetValue(grid)}");
        Assert.IsTrue(Gorundu(), "ölçüm için mesaj görünür olmalı");

        // Sistem bildirimi de aynı anda ekranda olsun
        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");
        Reflect.Call(ui, "ShowSystemMessage", "YENİ TEPKİME KAYDEDİLDİ", 30f);
        yield return null;
        yield return null;

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        Dictionary<string, RectTransform> olculecek = new Dictionary<string, RectTransform>();
        Ekle(olculecek, canvas, "InvalidMergePanel");
        Ekle(olculecek, canvas, "PressureMeter");
        Ekle(olculecek, canvas, "MeterLabel");

        RectTransform sistem = SistemBildirimi(canvas);
        if (sistem != null) olculecek["SistemBildirimi"] = sistem;

        Assert.IsTrue(olculecek.ContainsKey("InvalidMergePanel"), "bant yok");
        Assert.IsTrue(olculecek.ContainsKey("PressureMeter"), "katalizör göstergesi yok");

        Dictionary<string, Rect> kutular = new Dictionary<string, Rect>();
        foreach (KeyValuePair<string, RectTransform> kv in olculecek)
        {
            yield return BoyaliKutu(canvas, kv.Key, kv.Value, kutular);
        }

        List<string> hatalar = new List<string>();
        List<string> adlar = kutular.Keys.ToList();
        for (int i = 0; i < adlar.Count; i++)
        {
            for (int j = i + 1; j < adlar.Count; j++)
            {
                // Ata-torun çifti atlanır: katalizör ETİKETİ göstergenin
                // ÇOCUĞU; kapsanmak çakışma değildir. Mesajın hiçbiriyle
                // ata-torun ilişkisi yok, yani asıl ölçüm etkilenmez.
                if (AtaTorun(olculecek, adlar[i], adlar[j])) continue;

                Rect a = kutular[adlar[i]], b = kutular[adlar[j]];
                float ox = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                float oy = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                if (ox <= 0f || oy <= 0f) continue;

                hatalar.Add($"{adlar[i]} × {adlar[j]} = {ox:F0}x{oy:F0} piksel");
            }
        }

        foreach (KeyValuePair<string, Rect> kv in kutular)
        {
            Debug.Log($"{Tag}_BANT: {kv.Key,-20} boyalı alan " +
                      $"x {kv.Value.xMin:F0}..{kv.Value.xMax:F0}  " +
                      $"y {kv.Value.yMin:F0}..{kv.Value.yMax:F0}");
        }

        Assert.IsEmpty(hatalar, "çakışma: " + string.Join(" | ", hatalar));
    }

    // ---------------------------------------------------------- yardımcılar

    /// <summary>İki hedeften biri diğerinin atası mı?</summary>
    private static bool AtaTorun(Dictionary<string, RectTransform> kaynak, string a, string b)
    {
        RectTransform ra, rb;
        if (!kaynak.TryGetValue(a, out ra) || !kaynak.TryGetValue(b, out rb)) return false;
        return ra.IsChildOf(rb) || rb.IsChildOf(ra);
    }

    private static void Ekle(Dictionary<string, RectTransform> hedef, Transform canvas, string ad)
    {
        RectTransform rt = canvas.GetComponentsInChildren<RectTransform>(true)
            .FirstOrDefault(r => r.name == ad);
        if (rt != null) hedef[ad] = rt;
    }

    /// <summary>Sistem bildirimi çalışma zamanında doğar: en yeni klonu bul.</summary>
    private static RectTransform SistemBildirimi(Transform canvas)
    {
        return canvas.GetComponentsInChildren<RectTransform>(true)
            .Where(r => r.name.Contains("(Clone)") && r.GetComponent<TMP_Text>() != null)
            .OrderByDescending(r => r.GetInstanceID())
            .FirstOrDefault();
    }

    /// <summary>
    /// Bir nesnenin BOYALI sınırları: yalnızca o nesne çizilir, kare
    /// okunur ve zeminden ayrışan pikseller taranır.
    /// </summary>
    private IEnumerator BoyaliKutu(Transform canvas, string ad, RectTransform hedef,
                                   Dictionary<string, Rect> cikti)
    {
        List<Graphic> kapatilan = new List<Graphic>();
        foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(true))
        {
            if (!g.enabled) continue;
            if (g.transform == hedef || g.transform.IsChildOf(hedef)) continue;
            g.enabled = false;
            kapatilan.Add(g);
        }

        yield return null;
        yield return null;

        PixelProbe.Session s = PixelProbe.Begin(canvas.gameObject, new Vector2Int(540, 960), false);
        yield return null;
        yield return null;
        PixelProbe.Render(s);

        Texture2D frame = s.frame;
        Color[] px = frame.GetPixels();

        // OKUNAN İÇERİK, HALE DEĞİL. Eşik nesnenin KENDİ en parlak pikseline
        // göre: katalizör göstergesinin etrafındaki hale (ReadyGlow) ekranın
        // yarısına yayılıyor ve mutlak eşikle "boyalı" sayılıyordu. Hale,
        // üstüne yazı gelmesini engellemez; gösterge ise engeller.
        float tepe = 0f;
        foreach (Color c in px) tepe = Mathf.Max(tepe, PixelProbe.Luminance(c));

        float zeminLum = PixelProbe.Luminance(frame.GetPixel(2, 2));
        float esik = Mathf.Max(zeminLum + 0.02f, tepe * IcerikOrani);

        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int y = 0; y < frame.height; y++)
        {
            for (int x = 0; x < frame.width; x++)
            {
                if (PixelProbe.Luminance(px[y * frame.width + x]) < esik) continue;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
        }

        PixelProbe.End(s);
        foreach (Graphic g in kapatilan) g.enabled = true;
        yield return null;

        if (x1 < x0 || y1 < y0)
        {
            Assert.Fail($"{ad}: karede hiç boyalı piksel yok " +
                        "(süit -nographics ile koşuyor olabilir)");
            yield break;
        }

        cikti[ad] = Rect.MinMaxRect(x0, y0, x1, y1);
    }

    private void TahtaKur(string sol, string sag)
    {
        Temizle();
        Koy(12, sol);
        Koy(13, sag);
        Sifirla();
    }

    private void Temizle()
    {
        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform child = cell.GetChild(i);
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 0);
    }

    private void Koy(int index, string ad)
    {
        GameObject[] basics = (GameObject[])Field(grid, "basicElements").GetValue(grid);
        GameObject prefab = basics.FirstOrDefault(b => b.name == "Tile_" + ad);
        Assert.IsNotNull(prefab, "temel element yok: " + ad);

        GameObject tile = Object.Instantiate(prefab, cells[index]);
        tile.name = prefab.name;
        tile.transform.localScale = Vector3.one;
        tile.transform.localPosition = Vector3.zero;
    }

    private IEnumerator Kaydir(Vector2 yon)
    {
        MethodInfo m = grid.GetType().GetMethod("Shift",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        m.Invoke(grid, new object[] { yon });
        yield return null;
        yield return new WaitForSeconds(0.3f);
    }

    private void Rapor(Vector2 yon)
    {
        MethodInfo m = feedback.GetType().GetMethod("Report",
            BindingFlags.Public | BindingFlags.Instance);
        m.Invoke(feedback, new object[] { yon });
    }

    /// <summary>Metni ve sönümlemeyi sıfırlar: "göründü mü" ölçülebilsin.</summary>
    private void Sifirla()
    {
        TMP_Text satir = Satir();
        if (satir != null) satir.text = string.Empty;

        CanvasGroup g = Grup();
        if (g != null) g.alpha = 0f;
    }

    private bool Gorundu()
    {
        TMP_Text satir = Satir();
        return satir != null && !string.IsNullOrEmpty(satir.text);
    }

    private string Metin()
    {
        TMP_Text satir = Satir();
        return satir != null ? satir.text : string.Empty;
    }

    private TMP_Text Satir()
    {
        return Field(feedback, "lineText").GetValue(feedback) as TMP_Text;
    }

    private CanvasGroup Grup()
    {
        return Field(feedback, "group").GetValue(feedback) as CanvasGroup;
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo f = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, "alan yok: " + name);
        return f;
    }
}
