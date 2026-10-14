using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Değişken metinli alanları EN UZUN varyantıyla ölçer.
///
/// Neden ayrı test: <see cref="LayoutCheck"/> sahnedeki metni ölçüyor, oysa bu
/// alanların içeriği çalışma zamanında değişiyor. Katalizör etiketi sahnede
/// "KATALİZÖR ŞARJI 0/5" yazarken oyunda "BEDAVA PARÇALAMA ×3" oluyordu; sabit
/// metinle ölçen test bunu kaçırdı ve etiket cihazda iki satıra düşüp göstergenin
/// üstüne bindi.
///
/// Kural: her varyant kutusunun %85'ini aşmaz (CLAUDE.md metin uzunluğu kuralı),
/// hiçbir varyant kırpılmaz. Ölçüm iki dilde yapılır.
/// </summary>
public class VariableTextFitTests
{
    private const string Tag = "NEXUM_VARTEXT";

    private Loc.Language onceki;

    [SetUp]
    public void SetUp()
    {
        onceki = Loc.Current;
    }

    [TearDown]
    public void TearDown()
    {
        Loc.SetLanguage(onceki);
    }

    // ------------------------------------------------------------- yardımcı

    /// <summary>Bir kutuda bir dizi varyantı ölçer; en kötü doluluğu döndürür.</summary>
    private static float Olc(TMP_Text kutu, string ad, IEnumerable<string> varyantlar,
                             List<string> hatalar, string ekran)
    {
        string eskiMetin = kutu.text;
        float enKotu = 0f;

        foreach (string v in varyantlar)
        {
            kutu.text = v;
            kutu.ForceMeshUpdate();

            float doluluk = LayoutCheck.FillRatio(kutu, v);
            string detay;
            bool kirpik = LayoutCheck.IsClipped(kutu, out detay);

            if (doluluk > enKotu) enKotu = doluluk;

            Debug.Log($"{Tag}: {ekran} {ad} \"{Kisalt(v)}\" doluluk %{doluluk * 100f:F0} {detay}");

            if (kirpik) hatalar.Add($"{ekran} {ad}: \"{Kisalt(v)}\" kırpık ({detay})");
            if (doluluk > LayoutCheck.MaxFillRatio)
            {
                hatalar.Add($"{ekran} {ad}: \"{Kisalt(v)}\" doluluk %{doluluk * 100f:F0} " +
                            $"(sınır %{LayoutCheck.MaxFillRatio * 100f:F0})");
            }
        }

        kutu.text = eskiMetin;
        kutu.ForceMeshUpdate();
        return enKotu;
    }

    private static string Kisalt(string s)
    {
        string temiz = System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", "");
        return temiz.Length > 42 ? temiz.Substring(0, 42) + "…" : temiz;
    }

    private static IEnumerable<Loc.Language> Diller
    {
        get { return new[] { Loc.Language.Turkish, Loc.Language.English }; }
    }

    // -------------------------------------------------- oyun ekranı alanları

    // Katalizör etiketi + sistem bildirimi: tüm varyantlar, iki dil, 4 boyut.
    [UnityTest]
    public IEnumerator OyunEkraniDegiskenMetinleriSigar()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        TMP_Text katalizor = Reflect.Get<TextMeshProUGUI>(ui, "catalystLabelText");
        Assert.IsNotNull(katalizor, "catalystLabelText bağlı değil");

        GameObject comboPrefab = Reflect.Get<GameObject>(ui, "comboTextPrefab");
        Assert.IsNotNull(comboPrefab, "comboTextPrefab bağlı değil");

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        List<string> hatalar = new List<string>();
        float enKotu = 0f;
        string enKotuAd = "-";

        // Sistem bildirimi gerçek kutusuyla ölçülür (ShowSystemMessage kutuyu
        // 900x72'ye çeker); prefab kopyası üzerinde ölçüp yok ediyoruz.
        GameObject olcumObj = Object.Instantiate(comboPrefab);
        TMP_Text bildirim = olcumObj.GetComponent<TMP_Text>();
        bildirim.rectTransform.sizeDelta = new Vector2(900f, 72f);

        foreach (Loc.Language dil in Diller)
        {
            Loc.SetLanguage(dil);

            foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
            {
                LayoutCheck.SetCanvasSize(safeRoot, boyut);
                yield return null;

                string ekran = $"{dil} {boyut.x:F0}x{boyut.y:F0}";

                List<string> katalizorVaryantlari = new List<string>();
                for (int sarj = 0; sarj <= 5; sarj++)
                {
                    katalizorVaryantlari.Add(Loc.Format(CodeStrings.CatalystChargeLabel, sarj, 5));
                }
                for (int hak = 1; hak <= 3; hak++)
                {
                    katalizorVaryantlari.Add(Loc.Format(CodeStrings.CatalystReadyLabel, hak));
                }

                float k = Olc(katalizor, "KatalizorEtiketi", katalizorVaryantlari, hatalar, ekran);
                if (k > enKotu) { enKotu = k; enKotuAd = ekran + " KatalizorEtiketi"; }

                // ShowSystemMessage ile gösterilen metinler. CatalystReadyMessage
                // burada DEĞİL: o, duraklat kartındaki bilgi metnine yazılıyor
                // (ayrı testte ölçülüyor).
                string[] bildirimVaryantlari =
                {
                    Loc.Get(CodeStrings.RecipeDiscovered),
                    Loc.Get(CodeStrings.SystemMatrixReshuffled),
                };
                float bd = Olc(bildirim, "SistemBildirimi", bildirimVaryantlari, hatalar, ekran);
                if (bd > enKotu) { enKotu = bd; enKotuAd = ekran + " SistemBildirimi"; }

                string[] komboVaryantlari =
                {
                    Loc.Get(CodeStrings.Combo1), Loc.Get(CodeStrings.Combo2), Loc.Get(CodeStrings.Combo3),
                };
                bildirim.rectTransform.sizeDelta = new Vector2(700f, 100f);   // kombo prefab ölçüsü
                float km = Olc(bildirim, "KomboYazisi", komboVaryantlari, hatalar, ekran);
                bildirim.rectTransform.sizeDelta = new Vector2(900f, 72f);
                if (km > enKotu) { enKotu = km; enKotuAd = ekran + " KomboYazisi"; }
            }
        }

        Object.DestroyImmediate(olcumObj);

        Debug.Log($"{Tag}_SONUC: en kotu doluluk %{enKotu * 100f:F0} ({enKotuAd}), " +
                  $"sinir %{LayoutCheck.MaxFillRatio * 100f:F0}, sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Duraklat kartındaki bilgi metni: en uzun ChemistryFact sığmalı.
    [UnityTest]
    public IEnumerator DuraklatBilgiMetniEnUzunVaryantlaSigar()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        TMP_Text fact = Reflect.Get<TextMeshProUGUI>(ui, "factTextUI");
        Assert.IsNotNull(fact, "factTextUI bağlı değil");

        var facts = (System.Collections.IList)Reflect.Get(ui, "allFacts");
        Assert.IsNotNull(facts);
        Assert.Greater(facts.Count, 0, "bilgi kartı yok");

        List<string> varyantlar = new List<string>();
        foreach (object f in facts)
        {
            if (f == null) continue;
            varyantlar.Add(Reflect.Get<string>(f, "factText"));
        }
        varyantlar.Add(Loc.Get(CodeStrings.CatalystReadyMessage));   // kart bu metni de gösteriyor

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        List<string> hatalar = new List<string>();
        float enKotu = 0f;

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;

            float d = Olc(fact, "BilgiMetni", varyantlar, hatalar, $"{boyut.x:F0}x{boyut.y:F0}");
            if (d > enKotu) enKotu = d;
        }

        Debug.Log($"{Tag}_FACT_SONUC: {varyantlar.Count} varyant, en kotu %{enKotu * 100f:F0}, " +
                  $"sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Tarif defteri: sayaç, halka sayısı ve satır metinleri en uzun varyantla.
    [UnityTest]
    public IEnumerator TarifDefteriDegiskenMetinleriSigar()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Reflect.Call(menu, "OpenRecipeBook");
        yield return new WaitForSeconds(1f);

        MonoBehaviour panel = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "RecipeBookPanel");
        Assert.IsNotNull(panel, "RecipeBookPanel yok");

        TMP_Text sayac = Reflect.Get<TextMeshProUGUI>(panel, "counterText");
        TMP_Text halkaSayi = Reflect.Get<TextMeshProUGUI>(panel, "ringValueText");
        Assert.IsNotNull(sayac, "counterText bağlı değil");
        Assert.IsNotNull(halkaSayi, "ringValueText bağlı değil");

        object katalog = Reflect.Call("RecipeCatalog", "Load");
        int toplam = ((System.Collections.IList)Reflect.Get(katalog, "entries")).Count;

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        List<string> hatalar = new List<string>();
        float enKotu = 0f;
        string enKotuAd = "-";

        foreach (Loc.Language dil in Diller)
        {
            Loc.SetLanguage(dil);

            foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
            {
                LayoutCheck.SetCanvasSize(safeRoot, boyut);
                yield return null;

                string ekran = $"{dil} {boyut.x:F0}x{boyut.y:F0}";

                List<string> sayacVaryantlari = new List<string>();
                List<string> halkaVaryantlari = new List<string>();
                foreach (int n in new[] { 0, 7, toplam })
                {
                    sayacVaryantlari.Add(Loc.Format(CodeStrings.RecipeBookCounter, n, toplam));
                    halkaVaryantlari.Add(Loc.Format(CodeStrings.RecipeBookRingValue, n, toplam));
                }

                float a = Olc(sayac, "DefterSayaci", sayacVaryantlari, hatalar, ekran);
                if (a > enKotu) { enKotu = a; enKotuAd = ekran + " DefterSayaci"; }

                float c = Olc(halkaSayi, "HalkaSayisi", halkaVaryantlari, hatalar, ekran);
                if (c > enKotu) { enKotu = c; enKotuAd = ekran + " HalkaSayisi"; }
            }
        }

        Debug.Log($"{Tag}_DEFTER_SONUC: en kotu %{enKotu * 100f:F0} ({enKotuAd}), sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Menü butonlarının etiketleri (defter girişi dahil) iki dilde sığmalı.
    [UnityTest]
    public IEnumerator MenuButonEtiketleriSigar()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        Transform menuPanel = LayoutCheck.Find("SafeAreaRoot/MainMenuPanel");
        Assert.IsNotNull(menuPanel, "MainMenuPanel yok");

        Transform buton = menuPanel.Find("RecipeBookButton");
        Assert.IsNotNull(buton, "RecipeBookButton yok");
        TMP_Text etiket = buton.GetComponentInChildren<TMP_Text>(true);
        Assert.IsNotNull(etiket, "defter butonunun etiketi yok");

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        List<string> hatalar = new List<string>();
        float enKotu = 0f;

        foreach (Loc.Language dil in Diller)
        {
            Loc.SetLanguage(dil);

            foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
            {
                LayoutCheck.SetCanvasSize(safeRoot, boyut);
                yield return null;

                float d = Olc(etiket, "DefterButonuEtiketi",
                              new[] { Loc.Get(CodeStrings.RecipeBookButtonLabel) },
                              hatalar, $"{dil} {boyut.x:F0}x{boyut.y:F0}");
                if (d > enKotu) enKotu = d;
            }
        }

        Debug.Log($"{Tag}_BUTON_SONUC: en kotu %{enKotu * 100f:F0}, sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }
}
