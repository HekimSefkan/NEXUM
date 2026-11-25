using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// RÜTBE VE XP METİNLERİNİN SIĞMASI (Faz 2 / 2.1).
///
/// Dört canvas boyutu × iki dil, %85 doluluk + genişlik sığma kuralı
/// (CLAUDE.md). Metinler EN KÖTÜ hâlleriyle ölçülür:
///  - rütbe adı: tablodaki EN UZUN ad (ve Faz 6'da gelecek İngilizce
///    adayların en uzunu),
///  - XP değerleri: dört/beş haneli en geniş sayı.
///
/// Bugün sözlükte İngilizce metin YOK (çeviri Faz 6; en.json Türkçe
/// tohumla dolu), yani "iki dilde ölç" tek başına İngilizce'yi
/// kapsamıyordu. Bu yüzden planlanan İngilizce adlar da ÖLÇÜLEN küme
/// içinde: sığmayan bir çeviri bugünden düşer.
/// </summary>
public class ProgressionFitTests
{
    private const string Tag = "NEXUM_XP_FIT";

    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish, Loc.Language.English };

    /// <summary>
    /// Faz 6'da gelecek İngilizce rütbe adları — GENİŞLİK MUHAFIZI.
    /// Sözlüğe yazılmadılar (çeviri turunun işi); burada yalnızca kutuya
    /// sığdıkları ölçülüyor.
    /// </summary>
    private static readonly string[] IngilizceAdaylar =
    {
        "Apprentice", "Trainee", "Lab Technician", "Researcher",
        "Chemist", "Chief Chemist", "Nobel Candidate",
    };

    /// <summary>XP sayılarının en geniş hâli (beş hane).</summary>
    private const int EnGenisXp = 99999;

    private string yedek;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();
        SaveService.ImportRaw(null);
        SaveService.Data.xpBackfillDone = 1;
        yield return null;
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    // ================================================== hub + kutlama paneli

    [UnityTest]
    public IEnumerator HubSeridiVeKutlamaPaneli_MetinleriSigiyor()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(0.6f);

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        MonoBehaviour hub = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "MenuHub");
        Assert.IsNotNull(hub, "MenuHub yok");

        MonoBehaviour panel = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "RankUpPanel");
        Assert.IsNotNull(panel, "RankUpPanel MainMenu sahnesinde yok");

        TMP_Text rankText = (TMP_Text)Reflect.Get(hub, "rankText");
        Assert.IsNotNull(rankText, "hub rütbe metni bağlı değil");

        TMP_Text panelAd = (TMP_Text)Reflect.Get(panel, "rankNameText");
        TMP_Text panelXp = (TMP_Text)Reflect.Get(panel, "xpTotalText");
        TMP_Text panelSira = (TMP_Text)Reflect.Get(panel, "rankOrdinalText");
        Assert.IsNotNull(panelAd, "kutlama rütbe adı bağlı değil");

        // Panel ölçüm için AÇIK olmalı: TMP kapalı hiyerarşide satır üretmiyor.
        panel.gameObject.SetActive(true);
        yield return null;

        Loc.Language onceki = Loc.Current;
        List<string> asanlar = new List<string>();
        List<string> tablo = new List<string>();

        foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, canvas);
            yield return null;

            foreach (Loc.Language dil in Diller)
            {
                Loc.SetLanguage(dil);
                yield return null;

                foreach (string metin in RutbeAdlari(dil))
                {
                    Olc(asanlar, tablo, canvas, dil, "HubRankText", rankText, metin);
                    Olc(asanlar, tablo, canvas, dil, "PanelRankName", panelAd, metin);
                }

                string xpMetni = Loc.Format(CodeStrings.RankXpTotal, EnGenisXp);
                Olc(asanlar, tablo, canvas, dil, "PanelXp", panelXp, xpMetni);
                Olc(asanlar, tablo, canvas, dil, "PanelOrdinal", panelSira,
                    ProgressionTable.Ranks.Length.ToString());

                foreach (TMP_Text tmp in panel.GetComponentsInChildren<TMP_Text>(true))
                {
                    LocalizedTextliMi(tmp, asanlar, tablo, canvas, dil);
                }
            }
        }

        Loc.SetLanguage(onceki);
        panel.gameObject.SetActive(false);

        Rapor(tablo, asanlar);
        Assert.IsEmpty(asanlar, "sığmayan metin: " + string.Join(" | ", asanlar.Take(8)));
    }

    // ================================================== sonuç kartı XP satırı

    [UnityTest]
    public IEnumerator SonucKarti_XpSatiri_EnGenisDegerleSigiyor()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.6f);

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        MonoBehaviour card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");
        card.transform.parent.gameObject.SetActive(true);
        yield return null;

        TMP_Text deger = (TMP_Text)Reflect.Get(card, "xpValueText");
        Assert.IsNotNull(deger, "kartta XP değeri bağlı değil");

        TMP_Text etiket = card.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(t => t != null && t.transform.parent != null &&
                                 t.transform.parent.name == "XpRow" && t.name == "Label");
        Assert.IsNotNull(etiket, "kartta XP etiketi yok");

        Loc.Language onceki = Loc.Current;
        List<string> asanlar = new List<string>();
        List<string> tablo = new List<string>();

        foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, canvas);
            yield return null;

            foreach (Loc.Language dil in Diller)
            {
                Loc.SetLanguage(dil);
                yield return null;

                Olc(asanlar, tablo, canvas, dil, "XpValue", deger,
                    Loc.Format(CodeStrings.ResultXpValue, EnGenisXp));
                Olc(asanlar, tablo, canvas, dil, "XpLabel", etiket,
                    Loc.Get(CodeStrings.ResultXpLabel));
            }
        }

        Loc.SetLanguage(onceki);
        Rapor(tablo, asanlar);
        Assert.IsEmpty(asanlar, "sığmayan metin: " + string.Join(" | ", asanlar.Take(8)));
    }

    /// <summary>
    /// Kart 1600'e büyüdü: dört canvas boyutunda da güvenli alanın içinde
    /// kalıyor mu? (Kart SafeAreaRoot'un DIŞINDA ve sabit ölçülü.)
    /// </summary>
    [UnityTest]
    public IEnumerator SonucKarti_DortBoyuttaGuvenliAlandaKaliyor()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.6f);

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        MonoBehaviour card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");
        card.transform.parent.gameObject.SetActive(true);
        yield return null;

        RectTransform rect = card.transform as RectTransform;
        List<string> tasanlar = new List<string>();

        foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, canvas);
            yield return null;

            Vector4 k = LayoutCheck.LocalRect(safeRoot, rect);
            Vector4 sinir = new Vector4(-canvas.x * 0.5f, -canvas.y * 0.5f,
                                        canvas.x * 0.5f, canvas.y * 0.5f);
            float tasma = LayoutCheck.Overflow(k, sinir);

            Debug.Log($"{Tag}_KART: {canvas.x:F0}x{canvas.y:F0} kart " +
                      $"y {k.y:F0}..{k.w:F0} (yukseklik {k.w - k.y:F0}), tasma {tasma:F0}");

            if (tasma > 0f) tasanlar.Add($"{canvas.x:F0}x{canvas.y:F0} tasma {tasma:F0}");
        }

        Assert.IsEmpty(tasanlar, "kart güvenli alanı aşıyor: " + string.Join(" | ", tasanlar));
    }

    // ------------------------------------------------------------- yardımcı

    private static IEnumerable<string> RutbeAdlari(Loc.Language dil)
    {
        foreach (ProgressionTable.Rank r in ProgressionTable.Ranks) yield return Loc.Get(r.nameKey);
        foreach (string s in IngilizceAdaylar) yield return s;
    }

    private static void Olc(List<string> asanlar, List<string> tablo, Vector2 canvas,
                            Loc.Language dil, string etiket, TMP_Text tmp, string metin)
    {
        if (tmp == null || string.IsNullOrWhiteSpace(metin)) return;

        float y = LayoutCheck.FillRatio(tmp, metin);
        float x = LayoutCheck.FillRatioWidth(tmp, metin);
        int satir = LayoutCheck.LineCount(tmp, metin);

        tablo.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {etiket,-14} \"{metin}\" " +
                  $"y%{y * 100f:F0} x%{x * 100f:F0}");

        if (y > LayoutCheck.MaxFillRatio)
            asanlar.Add($"{etiket} \"{metin}\" yükseklik %{y * 100f:F0}");
        if (x > LayoutCheck.MaxFillRatio)
            asanlar.Add($"{etiket} \"{metin}\" genişlik %{x * 100f:F0}");
        if (satir > 1)
            asanlar.Add($"{etiket} \"{metin}\" {satir} satıra kırıldı");
    }

    /// <summary>Sözlüğe bağlı (sabit) metinler de ölçülür.</summary>
    private static void LocalizedTextliMi(TMP_Text tmp, List<string> asanlar, List<string> tablo,
                                          Vector2 canvas, Loc.Language dil)
    {
        if (tmp == null) return;
        MonoBehaviour loc = tmp.GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "LocalizedText");
        if (loc == null) return;

        string key = Reflect.Get<string>(loc, "key");
        if (string.IsNullOrEmpty(key)) return;

        Olc(asanlar, tablo, canvas, dil, tmp.name, tmp, Loc.Get(key));
    }

    private static void Rapor(List<string> tablo, List<string> asanlar)
    {
        Debug.Log($"{Tag}: ölçülen {tablo.Count} metin, aşan {asanlar.Count}, " +
                  $"sınır %{LayoutCheck.MaxFillRatio * 100f:F0}");
        foreach (string s in tablo.OrderByDescending(Doluluk).Take(6))
            Debug.Log($"{Tag}_EN_KOTU: {s}");
    }

    private static float Doluluk(string satir)
    {
        int i = satir.LastIndexOf("x%");
        float v;
        return i >= 0 && float.TryParse(satir.Substring(i + 2), out v) ? v : 0f;
    }
}
