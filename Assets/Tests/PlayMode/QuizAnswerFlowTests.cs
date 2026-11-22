using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Doğru cevap panelinin KAPANMA kuralı (1.5 düzeltmesi).
///
/// Panel artık kendiliğinden KAPANMIYOR: en az 2 saniye bekler, sonra
/// yalnızca ekrana dokununca kapanır. Önceki hâlde 10 saniyelik bir üst
/// sınır vardı; metni yavaş okuyan oyuncunun açıklaması ortasından
/// kesiliyordu.
///
/// Bu testin yakaladığı hata, gözle fark edilmesi en zor olanı: süre
/// sınırının geri gelmesi. Kimse 30 saniye bekleyip panele bakmaz.
/// </summary>
public class QuizAnswerFlowTests
{
    private const string Tag = "NEXUM_QUIZFLOW";

    /// <summary>Eski üst sınırın (10 sn) üç katından fazla.</summary>
    private const float Bekleme = 30f;

    private string yedek;
    private MonoBehaviour ui;
    private GameObject panel;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        ui = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        panel = Reflect.Get<GameObject>(ui, "quizPanel");
        Assert.IsNotNull(panel, "quizPanel atanmamış");
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    [UnityTest]
    public IEnumerator DokunulmazsaPanelAcikKalir()
    {
        Reflect.Call(ui, "ShowQuizPanel");
        yield return null;
        Assert.IsTrue(panel.activeSelf, "quiz paneli açılmadı");

        int dogruIndeks = Reflect.Get<int>(ui, "currentCorrectIndex");
        Reflect.Call(ui, "CheckAnswer", dogruIndeks);

        // En az bekleme süresi dolduktan hemen sonra da açık olmalı
        yield return new WaitForSeconds(3f);
        Assert.IsTrue(panel.activeSelf, "panel en az bekleme süresinden sonra kendiliğinden kapandı");

        TextMeshProUGUI soru = Reflect.Get<TextMeshProUGUI>(ui, "questionTextUI");
        string ipucu = Loc.Get(CodeStrings.QuizContinueHint);
        Debug.Log($"{Tag}: 3 sn -> panel açık, metin {soru.text.Length} karakter");
        Assert.IsTrue(soru.text.Contains(ipucu),
            "geri bildirim metninde \"dokun\" ipucu yok: " + soru.text);

        float gecen = 3f;
        while (gecen < Bekleme)
        {
            yield return new WaitForSeconds(3f);
            gecen += 3f;
            if (!panel.activeSelf) break;
        }

        Debug.Log($"{Tag}: {gecen:F0} sn dokunulmadı -> panel açık={panel.activeSelf}");
        Assert.IsTrue(panel.activeSelf,
            $"panel {gecen:F0} saniyede dokunulmadan kapandı (üst sınır geri mi geldi?)");
    }

    /// <summary>
    /// Üst sınır sabiti kodda da kalmamalı. Davranış testi 30 saniye
    /// sürüyor; birisi "testi hızlandırayım" diye süreyi kısaltırsa sınırın
    /// geri gelmesi fark edilmeyebilir. Bu kontrol saniye harcamaz.
    /// </summary>
    [UnityTest]
    public IEnumerator UstSinirSabitiKoddaYok()
    {
        System.Type tip = Reflect.Type("UIManager");
        System.Reflection.FieldInfo[] alanlar = tip.GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Instance);

        string[] sinirlar = alanlar
            .Select(f => f.Name)
            .Where(n => n.Contains("MaxHold") || n.Contains("AutoClose"))
            .ToArray();

        Debug.Log($"{Tag}: UIManager'da üst sınır sabiti = " +
                  (sinirlar.Length == 0 ? "yok" : string.Join(", ", sinirlar)));

        Assert.IsEmpty(sinirlar,
            "doğru cevap paneline kendiliğinden kapanma sınırı geri eklenmiş: " +
            string.Join(", ", sinirlar));
        yield return null;
    }
}
