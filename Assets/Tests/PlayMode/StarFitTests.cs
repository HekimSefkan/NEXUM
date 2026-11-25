using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// YILDIZ METİNLERİNİN SIĞMASI (Faz 2 / 2.2).
///
/// Dört canvas boyutu × iki dil, %85 doluluk + genişlik sığma kuralı.
/// Metinler EN KÖTÜ hâlleriyle ölçülür: en büyük hamle sınırı, üç
/// haneli hamle sayısı, en uzun yönlendirme satırı.
///
/// Sözlükte İngilizce metin YOK (çeviri Faz 6; en.json Türkçe tohumla
/// dolu), yani "iki dilde ölç" tek başına İngilizce'yi kapsamıyor. Faz
/// 6'da gelecek İngilizce karşılıklar da ÖLÇÜLEN küme içinde —
/// sığmayan bir çeviri bugünden düşer (rütbe adlarında aynı yöntem).
///
/// Ayrıca kartın YENİ dikey ritmi ölçülür: üst blok 2.2'de yukarı
/// alındı, bantlar çakışmamalı.
/// </summary>
public class StarFitTests
{
    private const string Tag = "NEXUM_YILDIZ_FIT";

    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish, Loc.Language.English };

    /// <summary>Faz 6'da gelecek İngilizce karşılıklar — GENİŞLİK MUHAFIZI.</summary>
    private static readonly string[] IngilizceAdaylar =
    {
        "Three stars! Flawless synthesis.",
        "Move limit 37 — you finished in 999",
        "Finish unaided: no hint, no paid Joker",
    };

    /// <summary>Üç haneli hamle: sayaç büyüdüğünde kutu patlamasın.</summary>
    private const int EnGenisHamle = 999;

    private string yedek;
    private MonoBehaviour card;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.6f);

        card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");

        card.transform.parent.gameObject.SetActive(true);
        yield return null;
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    [UnityTest]
    public IEnumerator YildizMetinleri_EnKotuHalleriyleSigiyor()
    {
        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        TMP_Text hint = (TMP_Text)Reflect.Get(card, "starHintText");
        TMP_Text moves = (TMP_Text)Reflect.Get(card, "movesValueText");
        Assert.IsNotNull(hint, "yıldız yönlendirme satırı bağlı değil");
        Assert.IsNotNull(moves, "hamle kutusu bağlı değil");

        int enBuyukSinir = StarRules.MoveLimits.Max();

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

                foreach (string metin in Adaylar(enBuyukSinir))
                {
                    Olc(asanlar, tablo, canvas, dil, "StarHint", hint, metin);
                }

                Olc(asanlar, tablo, canvas, dil, "MovesBox", moves,
                    Loc.Format(CodeStrings.ResultMovesWithLimit, EnGenisHamle, enBuyukSinir));
            }
        }

        Loc.SetLanguage(onceki);

        Debug.Log($"{Tag}: ölçülen {tablo.Count} metin, aşan {asanlar.Count}, " +
                  $"sınır %{LayoutCheck.MaxFillRatio * 100f:F0}");
        foreach (string s in tablo.OrderByDescending(Doluluk).Take(5))
            Debug.Log($"{Tag}_EN_KOTU: {s}");

        Assert.IsEmpty(asanlar, "sığmayan metin: " + string.Join(" | ", asanlar.Take(6)));
    }

    /// <summary>
    /// Kartın 2.2 ritmi: başlık · bölüm · yıldızlar · yönlendirme ·
    /// sayı kutuları ÇAKIŞMAMALI ve dördünün de kartın içinde kalmalı.
    /// Nesne listesi elle yazılmaz, kartın alt ağacından toplanır.
    /// </summary>
    [UnityTest]
    public IEnumerator KartinUstBlogu_CakismiyorVeKartinIcinde()
    {
        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        RectTransform cardRect = card.transform as RectTransform;

        string[] bantlar = { "TitleText", "LevelText", "StarsRow", "StarHintText",
                             "ScoreBox", "MovesBox" };

        List<string> sorunlar = new List<string>();

        foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, canvas);
            Reflect.Call(card, "Fill");
            yield return null;

            List<RectTransform> rects = new List<RectTransform>();
            foreach (string ad in bantlar)
            {
                Transform t = card.transform.Find(ad);
                Assert.IsNotNull(t, "kartta yok: " + ad);
                rects.Add(t as RectTransform);
            }

            Vector4 kart = LayoutCheck.LocalRect(safeRoot, cardRect);
            for (int i = 0; i < rects.Count; i++)
            {
                Vector4 r = LayoutCheck.LocalRect(safeRoot, rects[i]);
                float tasma = LayoutCheck.Overflow(r, kart);
                if (tasma > 0f)
                {
                    sorunlar.Add($"{canvas.x:F0}x{canvas.y:F0} {bantlar[i]} karttan {tasma:F0} taşıyor");
                }

                for (int j = i + 1; j < rects.Count; j++)
                {
                    // Sayı kutuları yan yana: yatayda ayrıklar, dikey bant
                    // çakışması sorun değil.
                    if (bantlar[i] == "ScoreBox" && bantlar[j] == "MovesBox") continue;

                    float alan = LayoutCheck.OverlapArea(r, LayoutCheck.LocalRect(safeRoot, rects[j]));
                    if (alan > 1f)
                    {
                        sorunlar.Add($"{canvas.x:F0}x{canvas.y:F0} {bantlar[i]} × {bantlar[j]} " +
                                     $"çakışma {alan:F0} birim²");
                    }
                }
            }

            Debug.Log($"{Tag}_RITIM: {canvas.x:F0}x{canvas.y:F0} " + string.Join("  ",
                bantlar.Select((ad, k) => $"{ad} {LayoutCheck.LocalRect(safeRoot, rects[k]).w:F0}" +
                                          $"..{LayoutCheck.LocalRect(safeRoot, rects[k]).y:F0}")));
        }

        Assert.IsEmpty(sorunlar, string.Join(" | ", sorunlar.Take(6)));
    }

    // ------------------------------------------------------------- yardımcı

    private static IEnumerable<string> Adaylar(int enBuyukSinir)
    {
        yield return Loc.Get(CodeStrings.ResultStarPerfect);
        yield return Loc.Format(CodeStrings.ResultStarNeedMoves, enBuyukSinir, EnGenisHamle);
        yield return Loc.Get(CodeStrings.ResultStarNeedNoHelp);

        foreach (string s in IngilizceAdaylar) yield return s;
    }

    private static void Olc(List<string> asanlar, List<string> tablo, Vector2 canvas,
                            Loc.Language dil, string etiket, TMP_Text tmp, string metin)
    {
        if (tmp == null || string.IsNullOrWhiteSpace(metin)) return;

        float y = LayoutCheck.FillRatio(tmp, metin);
        float x = LayoutCheck.FillRatioWidth(tmp, metin);
        int satir = LayoutCheck.LineCount(tmp, metin);

        tablo.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {etiket,-10} \"{metin}\" " +
                  $"y%{y * 100f:F0} x%{x * 100f:F0}");

        if (y > LayoutCheck.MaxFillRatio)
            asanlar.Add($"{etiket} \"{metin}\" yükseklik %{y * 100f:F0}");
        if (x > LayoutCheck.MaxFillRatio)
            asanlar.Add($"{etiket} \"{metin}\" genişlik %{x * 100f:F0}");
        if (satir > 1)
            asanlar.Add($"{etiket} \"{metin}\" {satir} satıra kırıldı");
    }

    private static float Doluluk(string satir)
    {
        int i = satir.LastIndexOf("x%");
        float v;
        return i >= 0 && float.TryParse(satir.Substring(i + 2), out v) ? v : 0f;
    }
}
