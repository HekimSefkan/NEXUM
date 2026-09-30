using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Tarif ipuçlarının ipucu kutusuna sığıp sığmadığını GERÇEK kutuda ölçer.
// Metin, oyundaki gibi alt bilgi satırıyla birleştirilip HintMessageText'e
// yazılır; TMP'nin hesapladığı yükseklik kutunun yüksekliğiyle kıyaslanır.
//
// GÜVENLİK PAYI: metin kutunun %85'ini aşarsa test düşer — sığıyor olsa bile.
// Gerekçe: İngilizce çeviri ve büyük yazı boyutu seçeneği metni uzatacak
// (Faz 6); bugün tam dolan bir kutu o zaman taşar.
//
// Test derlemesi Assembly-CSharp'ı referans alamadığı için yansıma kullanılır.
public class HintFitTests
{
    private const string Tag = "NEXUM_HINT_FIT";

    // Kutunun en fazla bu kadarı doldurulabilir (çeviri + büyük yazı payı).
    private const float MaxFillRatio = 0.85f;

    // İki dilli ölçüm için yapı hazır: Faz 6'da çeviriler bitince buraya
    // Loc.Language.English eklemek yeterli; testin gövdesi değişmez.
    private static readonly Loc.Language[] Diller = { Loc.Language.Turkish };

    // GridManager.ActivateHint ile aynı alt bilgi satırı (en kötü durum:
    // iki haneli bedel, kalan hak gösteriliyor).
    private const string InfoFooter =
        "\n\n<size=80%><color=#F1C40F>Kalan İpucu Hakkın: 2 | Sonraki Bedel: 150 Puan</color></size>";

    private MonoBehaviour grid;
    private TextMeshProUGUI hintText;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        FieldInfo field = ui.GetType().GetField("hintMessageText",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "hintMessageText alanı yok");
        hintText = (TextMeshProUGUI)field.GetValue(ui);
        Assert.IsNotNull(hintText, "hintMessageText atanmamış");
    }

    [UnityTest]
    public IEnumerator IpuclariGuvenlikPayiIcindeKaliyor()
    {
        Loc.Language onceki = Loc.Current;
        var recipes = (IList)grid.GetType()
            .GetField("recipes", BindingFlags.Public | BindingFlags.Instance).GetValue(grid);

        RectTransform box = hintText.rectTransform;
        float boxHeight = box.rect.height;
        float boxWidth = box.rect.width;
        float limit = boxHeight * MaxFillRatio;

        hintText.gameObject.SetActive(true);

        List<string> asanlar = new List<string>();
        List<string> tablo = new List<string>();

        foreach (Loc.Language dil in Diller)
        {
        Loc.SetLanguage(dil);
        foreach (object r in recipes)
        {
            var e1 = (GameObject)r.GetType().GetField("element1").GetValue(r);
            var e2 = (GameObject)r.GetType().GetField("element2").GetValue(r);
            string hint = (string)r.GetType().GetField("scientificHint").GetValue(r);
            if (e1 == null || e2 == null) continue;

            string ad = $"{Kisa(e1.name)} + {Kisa(e2.name)}";
            string core = string.IsNullOrEmpty(hint)
                ? "Bu iki elementi birleştirmek harika bir fikir olabilir!"
                : hint;

            hintText.text = core + InfoFooter;
            hintText.ForceMeshUpdate();

            float yukseklik = hintText.GetPreferredValues(hintText.text, boxWidth, 0f).y;
            float doluluk = yukseklik / boxHeight * 100f;

            tablo.Add($"[{dil}] {ad,-14} {core.Length,3} krk  {yukseklik,3:F0}/{boxHeight:F0} birim  %{doluluk:F0}");

            if (yukseklik > limit)
            {
                asanlar.Add($"[{dil}] {ad} %{doluluk:F0} ({yukseklik:F0} > {limit:F0})");
            }
        }

        }
        Loc.SetLanguage(onceki);

        tablo.Sort();
        foreach (string s in tablo) Debug.Log($"{Tag}_SATIR: {s}");
        Debug.Log($"{Tag}: kutu {boxWidth:F0}x{boxHeight:F0}, sınır %{MaxFillRatio * 100:F0} " +
                  $"({limit:F0} birim), aşan {asanlar.Count}/{tablo.Count}");
        foreach (string s in asanlar) Debug.Log($"{Tag}_ASIM: {s}");

        Assert.IsEmpty(asanlar,
            $"ipucu metni kutunun %{MaxFillRatio * 100:F0} sınırını aşıyor: " + string.Join(" | ", asanlar));
        yield return null;
    }

    // Sistem bildirimleri (kombo prefab'ı kutusunda) de %85 sınırına uymalı.
    [UnityTest]
    public IEnumerator SistemBildirimleriKutuyaSigiyor()
    {
        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        GameObject prefab = (GameObject)ui.GetType()
            .GetField("comboTextPrefab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(ui);
        Assert.IsNotNull(prefab, "comboTextPrefab atanmamış");

        GameObject ornek = Object.Instantiate(prefab, hintText.transform.parent);
        TextMeshProUGUI tmp = ornek.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(tmp, "kombo prefab'ında TMP yok");

        RectTransform box = tmp.rectTransform;
        float boxHeight = box.rect.height;
        float boxWidth = box.rect.width;
        float limit = boxHeight * MaxFillRatio;

        string[] mesajlar =
        {
            "MATRİS YENİDEN DÜZENLENDİ",
            "ZİNCİRLEME REAKSİYON!",
            "BAŞARILI SENTEZ!",
            "ÇİFTE BAĞ!",
        };

        List<string> asanlar = new List<string>();
        foreach (string mesaj in mesajlar)
        {
            tmp.text = mesaj;
            tmp.ForceMeshUpdate();
            float yukseklik = tmp.GetPreferredValues(mesaj, boxWidth, 0f).y;
            float doluluk = yukseklik / boxHeight * 100f;
            Debug.Log($"{Tag}_BILDIRIM: {mesaj,-28} {yukseklik,3:F0}/{boxHeight:F0} birim  %{doluluk:F0}");
            if (yukseklik > limit) asanlar.Add($"{mesaj} %{doluluk:F0}");
        }

        Debug.Log($"{Tag}_BILDIRIM_SONUC: kutu {boxWidth:F0}x{boxHeight:F0}, " +
                  $"sınır %{MaxFillRatio * 100:F0} ({limit:F0} birim), aşan {asanlar.Count}/{mesajlar.Length}");

        Object.DestroyImmediate(ornek);
        Assert.IsEmpty(asanlar, "sistem bildirimi %85 sınırını aşıyor: " + string.Join(" | ", asanlar));
        yield return null;
    }

    private static string Kisa(string tileName)
    {
        return tileName.Replace("Tile_", "");
    }
}
