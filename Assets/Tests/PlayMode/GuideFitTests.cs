using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// "Nasıl Oynanır" rehber metinlerinin pop-up kutusuna sığdığını GERÇEK kutuda ölçer.
//
// GÜVENLİK PAYI: metin kutunun %85'ini aşarsa test düşer — sığıyor olsa bile
// (Faz 6'da İngilizce çeviri ve büyük yazı boyutu metni uzatacak).
public class GuideFitTests
{
    private const string Tag = "NEXUM_GUIDE_FIT";
    private const float MaxFillRatio = 0.85f;

    // İki dilli ölçüm için yapı hazır: Faz 6'da buraya Loc.Language.English
    // eklemek yeterli.
    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish };

    private MonoBehaviour guide;
    private TextMeshProUGUI descText;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        guide = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "HowToPlayManager");
        Assert.IsNotNull(guide, "HowToPlayManager yok");

        FieldInfo field = guide.GetType().GetField("popupDescText",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "popupDescText alanı yok");
        descText = (TextMeshProUGUI)field.GetValue(guide);
        Assert.IsNotNull(descText, "popupDescText atanmamış");
    }

    [UnityTest]
    public IEnumerator RehberMetinleriGuvenlikPayiIcindeKaliyor()
    {
        var kategoriler = (System.Array)guide.GetType()
            .GetField("guideCategories", BindingFlags.Public | BindingFlags.Instance).GetValue(guide);
        Assert.IsNotNull(kategoriler, "guideCategories yok");

        RectTransform box = descText.rectTransform;
        float boxHeight = box.rect.height;
        float boxWidth = box.rect.width;
        float limit = boxHeight * MaxFillRatio;

        List<string> asanlar = new List<string>();
        Loc.Language onceki = Loc.Current;

        foreach (Loc.Language dil in Diller)
        {
        Loc.SetLanguage(dil);
        for (int i = 0; i < kategoriler.Length; i++)
        {
            object k = kategoriler.GetValue(i);
            string baslik = (string)k.GetType().GetField("title").GetValue(k);
            string metin = (string)k.GetType().GetField("description").GetValue(k);

            descText.text = metin;
            descText.ForceMeshUpdate();

            float yukseklik = descText.GetPreferredValues(metin, boxWidth, 0f).y;
            float doluluk = yukseklik / boxHeight * 100f;

            Debug.Log($"{Tag}_SATIR: [{dil}] {i + 1}. {baslik,-38} {metin.Length,3} krk  " +
                      $"{yukseklik,3:F0}/{boxHeight:F0} birim  %{doluluk:F0}");

            if (yukseklik > limit) asanlar.Add($"[{dil}] {baslik} %{doluluk:F0}");
        }

        }
        Loc.SetLanguage(onceki);

        Debug.Log($"{Tag}: kutu {boxWidth:F0}x{boxHeight:F0}, sınır %{MaxFillRatio * 100:F0} " +
                  $"({limit:F0} birim), aşan {asanlar.Count}/{kategoriler.Length}");
        foreach (string s in asanlar) Debug.Log($"{Tag}_ASIM: {s}");

        Assert.IsEmpty(asanlar, "rehber metni %85 sınırını aşıyor: " + string.Join(" | ", asanlar));
        yield return null;
    }
}
