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
// Test derlemesi Assembly-CSharp'ı referans alamadığı için yansıma kullanılır.
public class HintFitTests
{
    private const string Tag = "NEXUM_HINT_FIT";

    // GridManager.ActivateHint ile aynı alt bilgi satırı (en kötü durum:
    // iki haneli bedel, kalan hak gösteriliyor).
    private const string InfoFooter =
        "\n\n<size=80%><color=#F1C40F>Kalan İpucu Hakkın: 2 | Sonraki Bedel: 150 Puan</color></size>";

    private MonoBehaviour grid;
    private TextMeshProUGUI hintText;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        PlayerPrefs.SetInt("SelectedLevel", 0);
        PlayerPrefs.SetInt("TutorialRead_Level_0", 1);

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
    public IEnumerator IpuclariKutuyaSigiyor()
    {
        var recipes = (IList)grid.GetType()
            .GetField("recipes", BindingFlags.Public | BindingFlags.Instance).GetValue(grid);

        RectTransform box = hintText.rectTransform;
        float boxHeight = box.rect.height;
        float boxWidth = box.rect.width;

        // Kutu, ipucu paneli kapalıyken de ölçülebilsin diye panel açılır
        hintText.gameObject.SetActive(true);

        List<string> tasanlar = new List<string>();
        float enBuyuk = 0f;
        string enBuyukAd = "";

        foreach (object r in recipes)
        {
            var e1 = (GameObject)r.GetType().GetField("element1").GetValue(r);
            var e2 = (GameObject)r.GetType().GetField("element2").GetValue(r);
            string hint = (string)r.GetType().GetField("scientificHint").GetValue(r);
            if (e1 == null || e2 == null) continue;

            string ad = $"{e1.name} + {e2.name}";
            string core = string.IsNullOrEmpty(hint)
                ? "Bu iki elementi birleştirmek harika bir fikir olabilir!"
                : hint;

            hintText.text = core + InfoFooter;
            hintText.ForceMeshUpdate();

            float yukseklik = hintText.GetPreferredValues(hintText.text, boxWidth, 0f).y;
            int satir = hintText.textInfo.lineCount;

            if (yukseklik > enBuyuk) { enBuyuk = yukseklik; enBuyukAd = ad; }
            if (yukseklik > boxHeight)
            {
                tasanlar.Add($"{ad}: {yukseklik:F0} > {boxHeight:F0} ({satir} satır, {core.Length} karakter)");
            }
        }

        Debug.Log($"{Tag}: kutu {boxWidth:F0}x{boxHeight:F0}, en yüksek metin {enBuyuk:F0} ({enBuyukAd}), " +
                  $"taşan {tasanlar.Count}/{recipes.Count}");
        foreach (string s in tasanlar) Debug.Log($"{Tag}_TASMA: {s}");

        Assert.IsEmpty(tasanlar, "ipucu metni kutuya sığmıyor: " + string.Join(" | ", tasanlar));
        yield return null;
    }
}
