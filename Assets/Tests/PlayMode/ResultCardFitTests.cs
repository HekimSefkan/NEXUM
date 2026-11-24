using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Sonuç kartındaki metinlerin kutularına sığması (1.6b).
///
/// Nesne listesi ELLE YAZILMAZ: kartın alt ağacı gezilir, yani karta yeni
/// bir metin eklendiği gün ölçüme kendiliğinden girer.
///
/// İki kural birlikte:
///  - <b>%85 yükseklik doluluğu</b> (çeviri + büyük yazı payı)
///  - <b>genişlik sığması</b>, yalnızca TEK SATIR olması gereken
///    etiketlerde ve satır kırma KAPALI ölçülerek. Çok satırlı kutular
///    (tepkime listesi) bu kuralın dışında; orada yükseklik ölçülür.
///
/// Kart <c>SafeAreaRoot</c>'un DIŞINDA ve sabit ölçülü, yani sığma ekran
/// boyutundan bağımsız. Test bunu VARSAYMAZ, ölçer: dört canvas boyutunda
/// kutu ölçülerinin değişmediğini doğrular.
/// </summary>
public class ResultCardFitTests
{
    private const string Tag = "NEXUM_SONUCKARTI_FIT";

    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish, Loc.Language.English };

    /// <summary>Çok satırlı olmasına izin verilen kutular (genişlik kuralı dışı).</summary>
    private static readonly string[] CokSatirli = { "ReactionsText" };

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
        yield return new WaitForSeconds(0.5f);

        card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");

        // Ölçüm için kart AÇIK olmalı: TMP kapalı hiyerarşide satır üretmiyor.
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
    public IEnumerator KartMetinleriKutularinaSigiyor()
    {
        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        Loc.Language onceki = Loc.Current;
        List<string> asanlar = new List<string>();
        List<string> tablo = new List<string>();
        HashSet<string> olculer = new HashSet<string>();

        foreach (Vector2 canvas in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, canvas);
            yield return null;

            foreach (Loc.Language dil in Diller)
            {
                Loc.SetLanguage(dil);
                Reflect.Call(card, "Fill");
                yield return null;

                foreach (TMP_Text tmp in card.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null || string.IsNullOrWhiteSpace(tmp.text)) continue;

                    string ad = tmp.name;
                    RectTransform rect = tmp.rectTransform;

                    // AD YETMEZ: kartta iki ayrı "Label" var (sayı kutusu
                    // etiketi ve buton etiketi), ölçüleri de farklı. Ölçü
                    // kaydı TAM YOLLA anahtarlanır.
                    olculer.Add($"{Yol(tmp.transform)}:{rect.rect.width:F0}x{rect.rect.height:F0}");

                    float yukseklik = LayoutCheck.FillRatio(tmp, tmp.text);
                    tablo.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {ad,-16} y%{yukseklik * 100f:F0}");

                    if (yukseklik > LayoutCheck.MaxFillRatio)
                    {
                        asanlar.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {ad} yükseklik " +
                                    $"%{yukseklik * 100f:F0}");
                    }

                    if (CokSatirli.Contains(ad)) continue;

                    float genislik = LayoutCheck.FillRatioWidth(tmp, tmp.text);
                    int satir = LayoutCheck.LineCount(tmp, tmp.text);
                    if (genislik > LayoutCheck.MaxFillRatio)
                    {
                        asanlar.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {ad} genişlik " +
                                    $"%{genislik * 100f:F0}");
                    }
                    if (satir > 1)
                    {
                        asanlar.Add($"{canvas.x:F0}x{canvas.y:F0} [{dil}] {ad} {satir} satıra kırıldı");
                    }
                }
            }
        }

        Loc.SetLanguage(onceki);

        Debug.Log($"{Tag}: ölçülen {tablo.Count} metin, aşan {asanlar.Count}, " +
                  $"sınır %{LayoutCheck.MaxFillRatio * 100f:F0}");
        foreach (string s in tablo.OrderByDescending(Doluluk).Take(6))
            Debug.Log($"{Tag}_EN_KOTU: {s}");

        // Kutu ölçüleri ekran boyutundan bağımsız mı?
        int tekilAd = olculer.Select(o => o.Substring(0, o.LastIndexOf(':'))).Distinct().Count();
        Debug.Log($"{Tag}: {tekilAd} metin kutusu, {olculer.Count} farklı ölçü kaydı");
        Assert.AreEqual(tekilAd, olculer.Count,
            "bir kutu canvas boyutuna göre ölçü değiştiriyor: " +
            string.Join(" | ", olculer.OrderBy(x => x)));

        Assert.IsEmpty(asanlar, "sığmayan metin: " + string.Join(" | ", asanlar.Take(8)));
    }

    private static string Yol(Transform t)
    {
        string yol = t.name;
        while (t.parent != null && t.parent.name != "ResultCard")
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
