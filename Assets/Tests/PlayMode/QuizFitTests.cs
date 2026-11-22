using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Quiz metinlerinin kutuya sığması (1.5).
///
/// İki kutu ölçülür, ikisi de AYNI TMP nesnesi (<c>questionTextUI</c>):
///  - SORU düzeni: 800x300, panelin üstü (şıklar görünürken)
///  - GERİ BİLDİRİM düzeni: 800x700, panelin ortası (şıklar gizlendikten
///    sonra; başlık + AÇIKLAMA + "dokun" ipucu burada gösterilir)
///
/// Kural: hiçbir metin kutusunun %85'ini geçmez (çeviri + büyük yazı payı).
/// Tek satır olması gereken "dokun" ipucunda ayrıca GENİŞLİK sığma kuralı
/// uygulanır (kırma kapalı ölçülür).
///
/// Punto PİŞMİŞ olmalı: bu TMP'de çalışma zamanı auto-size yasak
/// (CLAUDE.md). Auto-size açıkken uzun sorular sessizce 34 puntoya iniyordu
/// ve "sığıyor" görünüyordu; ölçüm de o küçülmeyi ölçtüğü için kural
/// anlamsızlaşıyordu.
///
/// DÖRT CANVAS BOYUTU: quiz paneli SafeAreaRoot'un DIŞINDA ve kutuları sabit
/// ölçülü, yani metin sığması ekran boyutundan bağımsız. Test bunu varsaymaz,
/// ÖLÇER: dört boyutta kutu ölçüsünün değişmediğini doğrular ve dolulukları
/// her boyutta yeniden hesaplar.
/// </summary>
public class QuizFitTests
{
    private const string Tag = "NEXUM_QUIZ_FIT";

    private static readonly Vector2 SoruKutusu = new Vector2(800f, 300f);
    private static readonly Vector2 GeriBildirimKutusu = new Vector2(800f, 700f);

    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish, Loc.Language.English };

    private MonoBehaviour ui;
    private TextMeshProUGUI soruMetni;
    private IList banka;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        ui = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        soruMetni = Reflect.Get<TextMeshProUGUI>(ui, "questionTextUI");
        Assert.IsNotNull(soruMetni, "questionTextUI atanmamış");

        banka = Reflect.Get(ui, "allQuizzes") as IList;
        Assert.IsNotNull(banka, "banka okunamadı");

        // Ölçüm için panel AÇIK olmalı: TMP kapalı hiyerarşide satır
        // üretmiyor (ilk koşuda satır sayısı 0 çıktı ve kırılma kuralı
        // sessizce ölçülemez hâldeydi).
        GameObject panel = Reflect.Get<GameObject>(ui, "quizPanel");
        Assert.IsNotNull(panel, "quizPanel atanmamış");
        panel.SetActive(true);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PuntoPismis()
    {
        Debug.Log($"{Tag}: punto={soruMetni.fontSize} autoSize={soruMetni.enableAutoSizing} " +
                  $"({soruMetni.fontSizeMin}-{soruMetni.fontSizeMax})");

        Assert.IsFalse(soruMetni.enableAutoSizing,
            "quiz soru metninde çalışma zamanı auto-size açık (punto ölçülüp pişirilmeli)");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SoruVeAciklamaKutuyaSigiyor()
    {
        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        Loc.Language onceki = Loc.Current;
        List<string> asanlar = new List<string>();
        List<string> tablo = new List<string>();
        HashSet<string> kutuOlculeri = new HashSet<string>();

        foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, canvas);
            yield return null;

            foreach (Loc.Language dil in Diller)
            {
                Loc.SetLanguage(dil);

                // --- soru düzeni
                Olc(SoruKutusu);
                kutuOlculeri.Add($"{soruMetni.rectTransform.rect.width:F0}x{soruMetni.rectTransform.rect.height:F0}");

                foreach (object q in banka)
                {
                    string ad = ((ScriptableObject)q).name;
                    string govde = Reflect.Get<string>(q, "questionText");
                    float oran = LayoutCheck.FillRatio(soruMetni, govde);

                    Kaydet(tablo, asanlar, canvas, dil, ad + " soru", oran);
                }

                // --- geri bildirim düzeni (başlık + açıklama + ipucu)
                Olc(GeriBildirimKutusu);

                foreach (object q in banka)
                {
                    string ad = ((ScriptableObject)q).name;
                    string metin = GeriBildirim(q);
                    float oran = LayoutCheck.FillRatio(soruMetni, metin);

                    Kaydet(tablo, asanlar, canvas, dil, ad + " cevap", oran);
                }
            }
        }

        Loc.SetLanguage(onceki);

        tablo.Sort();
        Debug.Log($"{Tag}: ölçülen {tablo.Count} metin, aşan {asanlar.Count}, " +
                  $"sınır %{LayoutCheck.MaxFillRatio * 100f:F0}");
        Debug.Log($"{Tag}: kutu ölçüleri dört canvas boyutunda -> " +
                  $"{string.Join(" | ", kutuOlculeri)}");

        // En kötü beş doluluk her zaman loglanır (punto pişirme kararının izi)
        foreach (string s in tablo.OrderByDescending(Doluluk).Take(5))
            Debug.Log($"{Tag}_EN_KOTU: {s}");

        Assert.AreEqual(1, kutuOlculeri.Count,
            "soru kutusunun ölçüsü canvas boyutuna göre değişiyor: " + string.Join(" | ", kutuOlculeri));
        Assert.IsEmpty(asanlar, "sığmayan metin: " + string.Join(" | ", asanlar.Take(8)));
    }

    [UnityTest]
    public IEnumerator DokunIpucuTekSatirdaSigiyor()
    {
        Loc.Language onceki = Loc.Current;
        List<string> asanlar = new List<string>();

        Olc(GeriBildirimKutusu);

        foreach (Loc.Language dil in Diller)
        {
            Loc.SetLanguage(dil);

            string ipucu = "<size=58%>" + Loc.Get(CodeStrings.QuizContinueHint) + "</size>";
            float genislik = LayoutCheck.FillRatioWidth(soruMetni, ipucu);
            int satir = LayoutCheck.LineCount(soruMetni, ipucu);

            Debug.Log($"{Tag}_IPUCU: [{dil}] genişlik %{genislik * 100f:F0} satır={satir}");

            if (genislik > LayoutCheck.MaxFillRatio)
                asanlar.Add($"[{dil}] genişlik %{genislik * 100f:F0}");
            if (satir != 1)
                asanlar.Add($"[{dil}] {satir} satıra kırıldı");
        }

        Loc.SetLanguage(onceki);
        Assert.IsEmpty(asanlar, "dokun ipucu sığmıyor: " + string.Join(" | ", asanlar));
        yield return null;
    }

    /// <summary>
    /// Punto merdiveni: pişirilen puntonun NEDEN bu değer olduğunun izi.
    /// Hata vermez, yalnızca ölçer — kararı okunur kılar.
    /// </summary>
    [UnityTest]
    public IEnumerator PuntoMerdiveniOlculuyor()
    {
        float asil = soruMetni.fontSize;
        float[] adaylar = { 50f, 46f, 44f, 42f, 40f, 38f, 36f, 34f };

        foreach (float punto in adaylar)
        {
            soruMetni.fontSize = punto;

            Olc(SoruKutusu);
            float enKotuSoru = banka.Cast<object>()
                .Max(q => LayoutCheck.FillRatio(soruMetni, Reflect.Get<string>(q, "questionText")));

            Olc(GeriBildirimKutusu);
            float enKotuCevap = banka.Cast<object>()
                .Max(q => LayoutCheck.FillRatio(soruMetni, GeriBildirim(q)));

            Debug.Log($"{Tag}_MERDIVEN: punto {punto,2:F0} -> soru %{enKotuSoru * 100f:F0}, " +
                      $"cevap %{enKotuCevap * 100f:F0}" +
                      (enKotuSoru <= LayoutCheck.MaxFillRatio && enKotuCevap <= LayoutCheck.MaxFillRatio
                          ? "  GEÇER" : ""));
        }

        soruMetni.fontSize = asil;
        yield return null;
    }

    // --------------------------------------------------------------- yardımcı

    private void Olc(Vector2 kutu)
    {
        soruMetni.rectTransform.sizeDelta = kutu;
        soruMetni.ForceMeshUpdate();
    }

    /// <summary>UIManager.BuildCorrectFeedback ile AYNI metin.</summary>
    private static string GeriBildirim(object quiz)
    {
        return Reflect.Call<string>("UIManager", "BuildCorrectFeedback", quiz);
    }

    private static void Kaydet(List<string> tablo, List<string> asanlar,
                               Vector2 canvas, Loc.Language dil, string etiket, float oran)
    {
        tablo.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {etiket,-18} %{oran * 100f:F0}");
        if (oran > LayoutCheck.MaxFillRatio)
            asanlar.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {etiket} %{oran * 100f:F0}");
    }

    private static float Doluluk(string satir)
    {
        int i = satir.LastIndexOf('%');
        return i < 0 ? 0f : float.TryParse(satir.Substring(i + 1), out float v) ? v : 0f;
    }
}
