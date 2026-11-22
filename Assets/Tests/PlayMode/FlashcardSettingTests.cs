using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// "Bilgi Kartları" ayarının duraklat kartındaki karşılığı (1.5).
///
/// Ayar 1.4'te ÖLÜ-KAYIT olarak işaretlenmişti: oyuncu anahtarı çeviriyor,
/// oyunda hiçbir şey değişmiyordu. Artık duraklat kartındaki
/// "BİLİYOR MUYDUN?" bölümünü (alt başlık + bilgi metni) yönetiyor.
///
/// Neyi koruyor:
///  - Ayar KAPALI iken kart bölümü görünmüyor.
///  - Ayar AÇIK iken görünüyor VE bilgi metni dolu (boş kutu bırakılmıyor).
///  - Duraklat menüsünün GERİSİ iki durumda da aynı: başlık, Devam Et,
///    Ana Menü ve Tarif Defteri butonları her hâlde etkin.
///  - Alan sahneye BAĞLI: boş bir dizi testi sessizce geçirmemeli.
/// </summary>
public class FlashcardSettingTests
{
    private const string Tag = "NEXUM_FLASHCARD_TEST";
    private const string CardPath = "PausePanel/ContainerBox";

    private static readonly string[] HerZamanGorunur =
    {
        "TitleText", "ResumeButton", "MainMenuButton", "RecipeBookButton",
    };

    private string yedek;
    private MonoBehaviour ui;

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
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    [UnityTest]
    public IEnumerator AlanSahneyeBagli()
    {
        GameObject[] parcalar = Reflect.Get<GameObject[]>(ui, "flashcardObjects");

        Assert.IsNotNull(parcalar, "flashcardObjects null (NexumFlashcardBind.Run koşuldu mu?)");
        Assert.AreEqual(2, parcalar.Length, "bilgi kartı iki parçadan oluşmalı (alt başlık + metin)");

        foreach (GameObject go in parcalar)
        {
            Assert.IsNotNull(go, "flashcardObjects içinde null giriş var");
            Debug.Log($"{Tag}_PARCA: {go.name}");
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator AyarAcikkenKartGorunurVeDolu()
    {
        SaveService.Data.flashcardsOn = 1;

        Reflect.Call(ui, "TogglePause", true);
        yield return null;

        GameObject[] parcalar = Reflect.Get<GameObject[]>(ui, "flashcardObjects");
        foreach (GameObject go in parcalar)
        {
            Debug.Log($"{Tag}: açık -> {go.name} activeSelf={go.activeSelf}");
            Assert.IsTrue(go.activeSelf, $"ayar açıkken {go.name} gizli");
        }

        TextMeshProUGUI fact = Reflect.Get<TextMeshProUGUI>(ui, "factTextUI");
        Assert.IsNotNull(fact, "factTextUI atanmamış");
        Debug.Log($"{Tag}: bilgi metni = \"{Kisalt(fact.text)}\"");
        Assert.IsFalse(string.IsNullOrWhiteSpace(fact.text), "ayar açıkken bilgi metni boş");

        DogrulaGerisiAyni();

        Reflect.Call(ui, "TogglePause", false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AyarKapalikenKartGorunmez()
    {
        SaveService.Data.flashcardsOn = 0;

        Reflect.Call(ui, "TogglePause", true);
        yield return null;

        GameObject[] parcalar = Reflect.Get<GameObject[]>(ui, "flashcardObjects");
        foreach (GameObject go in parcalar)
        {
            Debug.Log($"{Tag}: kapalı -> {go.name} activeSelf={go.activeSelf}");
            Assert.IsFalse(go.activeSelf, $"ayar kapalıyken {go.name} görünüyor");
        }

        DogrulaGerisiAyni();

        Reflect.Call(ui, "TogglePause", false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AyarIkiYoneDeCalisir()
    {
        List<string> izler = new List<string>();

        for (int tur = 0; tur < 2; tur++)
        {
            foreach (int ayar in new[] { 0, 1 })
            {
                SaveService.Data.flashcardsOn = ayar;

                Reflect.Call(ui, "TogglePause", true);
                yield return null;

                GameObject[] parcalar = Reflect.Get<GameObject[]>(ui, "flashcardObjects");
                bool gorunur = parcalar.All(go => go.activeSelf);
                izler.Add($"ayar={ayar} görünür={gorunur}");

                Assert.AreEqual(ayar == 1, gorunur,
                    $"tur {tur}: ayar {ayar} iken kart görünürlüğü {gorunur}");

                Reflect.Call(ui, "TogglePause", false);
                yield return null;
            }
        }

        Debug.Log($"{Tag}: {string.Join(" | ", izler)}");
    }

    // --------------------------------------------------------------- yardımcı

    private static void DogrulaGerisiAyni()
    {
        foreach (string ad in HerZamanGorunur)
        {
            Transform t = LayoutCheck.Find(CardPath + "/" + ad);
            Assert.IsNotNull(t, "duraklat kartında obje yok: " + ad);
            Assert.IsTrue(t.gameObject.activeSelf,
                $"duraklat menüsünün gerisi değişti: {ad} gizlenmiş");
        }
    }

    private static string Kisalt(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= 48 ? s : s.Substring(0, 48) + "…";
    }
}
