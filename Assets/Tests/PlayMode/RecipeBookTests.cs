using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tarif defteri: keşif kaydı, geriye dönük doldurma, sürüm göçü ve panel.
///
/// Oyun kodu Assembly-CSharp'ta olduğu için <see cref="Reflect"/> üzerinden
/// erişilir; kalıcı veri (<c>SaveService</c>) ve sözlük (<c>Loc</c>) kendi
/// asmdef'lerinde olduğu için doğrudan kullanılır. Kalıcı depolamaya TEK
/// KAPIDAN dokunulur: <c>SaveService.ExportRaw / ImportRaw</c>.
/// </summary>
public class RecipeBookTests
{
    private const string Tag = "NEXUM_RECIPEBOOK_TEST";
    private const string Book = "RecipeBook";
    private const string Catalog = "RecipeCatalog";

    private string yedek;

    [SetUp]
    public void Yedekle()
    {
        yedek = SaveService.ExportRaw();
    }

    [TearDown]
    public void GeriYukle()
    {
        SaveService.ImportRaw(yedek);
    }

    // --------------------------------------------------------------- yardımcı

    private static object Katalog()
    {
        object katalog = Reflect.Call(Catalog, "Load");
        Assert.IsNotNull(katalog, "RecipeCatalog yok (NEXUM -> Tarif Kataloğunu Kur)");
        return katalog;
    }

    private static List<object> Girdiler()
    {
        var liste = (System.Collections.IList)Reflect.Get(Katalog(), "entries");
        return liste.Cast<object>().ToList();
    }

    private static bool Kesfet(string key)
    {
        return Reflect.Call<bool>(Book, "Discover", key);
    }

    private static List<string> Doldurma(int acilanBolum)
    {
        return Reflect.Call<List<string>>(Book, "ComputeBackfill", acilanBolum);
    }

    // ---------------------------------------------------------- keşif kaydı

    // İlk birleşmede kaydedilir; aynı tarif ikinci kez keşif sayılmaz.
    [UnityTest]
    public IEnumerator KesifBirKezSayilir()
    {
        SaveService.Data.discoveredRecipes.Clear();

        const string key = "Tile_H_Tile_H";
        bool ilk = Kesfet(key);
        bool ikinci = Kesfet(key);
        bool kayitli = Reflect.Call<bool>(Book, "IsDiscovered", key);
        int sayac = Reflect.Prop<int>(Book, "DiscoveredCount");

        Debug.Log($"{Tag}_KESIF: ilk={ilk} ikinci={ikinci} kayitli={kayitli} sayac={sayac}");

        Assert.IsTrue(ilk, "ilk birleşme keşif saymalı");
        Assert.IsFalse(ikinci, "aynı tarif ikinci kez keşif sayılmamalı");
        Assert.IsTrue(kayitli, "tarif kayıtlı olmalı");
        Assert.AreEqual(1, sayac, "sayaç ikinci kezde artmamalı");
        yield return null;
    }

    // Oyunda gerçek birleşme yapınca tarif deftere yazılır.
    [UnityTest]
    public IEnumerator BirlesmeTarifiDeftereYazar()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.Data.discoveredRecipes.Clear();
        SaveService.Data.recipeBackfillDone = 1;      // doldurma karışmasın
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        var cells = Reflect.Get<List<Transform>>(grid, "cells");
        var basics = Reflect.Get<GameObject[]>(grid, "basicElements");

        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform c = cell.GetChild(i);
                c.SetParent(null);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        GameObject h = basics.First(x => x.name == "Tile_H");
        foreach (int index in new[] { 0, 1 })
        {
            GameObject tile = Object.Instantiate(h, cells[index]);
            tile.name = h.name;
            tile.transform.localScale = Vector3.one;
            tile.transform.localPosition = Vector3.zero;
        }

        int once = SaveService.Data.discoveredRecipes.Count;
        Reflect.Call(grid, "Shift", Vector2.left);      // H + H -> H2
        yield return null;

        List<string> defter = SaveService.Data.discoveredRecipes;
        Debug.Log($"{Tag}_OYUN: H+H sonrasi defter {once} -> {defter.Count} " +
                  $"[{string.Join(", ", defter)}]");

        Assert.Contains("Tile_H_Tile_H", defter, "birleşme tarifi deftere yazmalı");
    }

    // ------------------------------------------------- geriye dönük doldurma

    // Doldurma yalnızca katalogdaki tarifleri verir ve bölüm sayısıyla büyür.
    [UnityTest]
    public IEnumerator DoldurmaGraftenTuretilir()
    {
        List<object> girdiler = Girdiler();
        HashSet<string> anahtarlar = new HashSet<string>(girdiler.Select(g => Reflect.Get<string>(g, "key")));
        Debug.Log($"{Tag}_DOLDURMA: katalogda {girdiler.Count} tarif");

        int onceki = 0;
        for (int acilan = 0; acilan <= 7; acilan++)
        {
            List<string> sonuc = Doldurma(acilan);
            Debug.Log($"{Tag}_DOLDURMA: acilan bolum {acilan} -> zorunlu {sonuc.Count} " +
                      $"[{string.Join(", ", sonuc)}]");

            foreach (string key in sonuc)
            {
                Assert.IsTrue(anahtarlar.Contains(key), "katalogda olmayan anahtar: " + key);
            }

            Assert.GreaterOrEqual(sonuc.Count, onceki, $"bolum {acilan}: zorunlu küme küçülmemeli");
            onceki = sonuc.Count;
        }

        Assert.IsEmpty(Doldurma(0), "hiç bölüm açılmamışsa defter boş kalmalı");
        yield return null;
    }

    // Muhafazakârlık: birden fazla üreticisi olan bileşik için tarif verilmez.
    [UnityTest]
    public IEnumerator DoldurmaBelirsizTarifiVermez()
    {
        List<object> girdiler = Girdiler();

        // Ürün başına kaç tarif var?
        Dictionary<string, int> ureticiSayisi = new Dictionary<string, int>();
        foreach (object g in girdiler)
        {
            string urun = Reflect.Get<string>(g, "productName");
            ureticiSayisi[urun] = ureticiSayisi.TryGetValue(urun, out int n) ? n + 1 : 1;
        }

        List<string> belirsiz = ureticiSayisi.Where(p => p.Value > 1).Select(p => p.Key).ToList();
        List<string> doldurma = Doldurma(7);

        Debug.Log($"{Tag}_BELIRSIZ: birden fazla ureticisi olan bilesik {belirsiz.Count} " +
                  $"[{string.Join(", ", belirsiz)}]");

        foreach (string key in doldurma)
        {
            object girdi = girdiler.First(g => Reflect.Get<string>(g, "key") == key);
            string urun = Reflect.Get<string>(girdi, "productName");
            Assert.IsFalse(belirsiz.Contains(urun),
                $"belirsiz ürün için tarif verilmemeli: {urun} ({key})");
        }
        yield return null;
    }

    // Doldurma bir kez çalışır; ikinci çağrı defteri değiştirmez.
    [UnityTest]
    public IEnumerator DoldurmaBirKezCalisir()
    {
        SaveService.Data.discoveredRecipes.Clear();
        SaveService.Data.recipeBackfillDone = 0;
        SaveService.Data.maxLevelUnlocked = 4;

        int ilk = Reflect.Call<int>(Book, "RunBackfillOnce");
        int defterIlk = SaveService.Data.discoveredRecipes.Count;
        int ikinci = Reflect.Call<int>(Book, "RunBackfillOnce");

        Debug.Log($"{Tag}_DOLDURMA_BIRKEZ: maxLevelUnlocked=4 -> ilk +{ilk} " +
                  $"(defter {defterIlk}), ikinci +{ikinci}");

        Assert.AreEqual(0, ikinci, "doldurma ikinci kez çalışmamalı");
        Assert.AreEqual(defterIlk, SaveService.Data.discoveredRecipes.Count, "defter değişmemeli");
        Assert.AreEqual(1, SaveService.Data.recipeBackfillDone);
        yield return null;
    }

    // ------------------------------------------------------------------ göç

    // Sürüm 1 kaydı: çökmez, eski veri korunur, doldurma devreye girer.
    [UnityTest]
    public IEnumerator SurumIkiGocuEskiVeriyiKorur()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":1,\"playerName\":\"Marie\",\"totalSynthesis\":678," +
            "\"maxLevelUnlocked\":5,\"unlockedLevel\":4,\"musicOn\":0,\"quizCorrect\":11," +
            "\"quizAttempts\":14,\"tutorialReadLevels\":[0,2]}");

        SaveData d = SaveService.Data;      // ilk erişim göçü tetikler

        Debug.Log($"{Tag}_GOC2: surum={d.saveVersion} ad=\"{d.playerName}\" sentez={d.totalSynthesis} " +
                  $"maxBolum={d.maxLevelUnlocked} muzik={d.musicOn} foy={d.tutorialReadLevels.Count} " +
                  $"defter={d.discoveredRecipes.Count} doldurmaYapildi={d.recipeBackfillDone}");

        Assert.AreEqual(SaveService.CurrentVersion, d.saveVersion, "sürüm 2'ye yükselmeli");
        Assert.AreEqual("Marie", d.playerName, "eski veri korunmalı");
        Assert.AreEqual(678, d.totalSynthesis);
        Assert.AreEqual(5, d.maxLevelUnlocked);
        Assert.AreEqual(0, d.musicOn);
        Assert.AreEqual(2, d.tutorialReadLevels.Count);
        Assert.IsNotNull(d.discoveredRecipes, "defter alanı kurulmalı");
        Assert.AreEqual(0, d.recipeBackfillDone, "doldurma bayrağı açık kalmalı");

        // Doldurma göçten sonra çalışır ve eski oyuncuya boş defter bırakmaz
        int eklenen = Reflect.Call<int>(Book, "RunBackfillOnce");
        Debug.Log($"{Tag}_GOC2_DOLDURMA: maxLevelUnlocked=5 -> +{eklenen} tarif, " +
                  $"defter {SaveService.Data.discoveredRecipes.Count}");
        Assert.Greater(eklenen, 0, "5 bölüm açmış oyuncu boş defterle kalmamalı");
        yield return null;
    }

    // Bozuk kayıt: defter alanı eksikse onarılır, çökmez.
    [UnityTest]
    public IEnumerator BozukKayitOnarilir()
    {
        SaveService.ImportRaw("{\"saveVersion\":2,\"playerName\":\"Ada\",\"recipeBackfillDone\":7}");

        SaveData d = SaveService.Data;
        Debug.Log($"{Tag}_ONARIM: defter={(d.discoveredRecipes == null ? "null" : d.discoveredRecipes.Count.ToString())} " +
                  $"doldurmaBayragi={d.recipeBackfillDone}");

        Assert.IsNotNull(d.discoveredRecipes, "eksik liste onarılmalı");
        Assert.AreEqual(1, d.recipeBackfillDone, "aralık dışı bayrak 0..1'e çekilmeli");
        yield return null;
    }

    // ---------------------------------------------------------------- panel

    // Panel açılışında: 21 satır, kaydırma en üstte, kilitler doğru.
    [UnityTest]
    public IEnumerator PanelAcilistaEnUsttenVe21Satir()
    {
        SaveService.Data.discoveredRecipes.Clear();
        SaveService.Data.recipeBackfillDone = 1;
        SaveService.Data.discoveredRecipes.Add(Reflect.Get<string>(Girdiler()[0], "key"));
        SaveService.SaveNow();

        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");

        Reflect.Call(menu, "OpenRecipeBook");
        yield return new WaitForSeconds(1f);

        MonoBehaviour panel = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "RecipeBookPanel");
        Assert.IsNotNull(panel, "RecipeBookPanel yok (NEXUM -> Tarif Defterini Kur)");

        ScrollRect scroll = Reflect.Get<ScrollRect>(panel, "scrollRect");
        Assert.IsNotNull(scroll, "ScrollRect bağlı değil");
        Assert.IsNotNull(scroll.content, "ScrollRect içeriği bağlı değil");

        int satir = Reflect.Get<int>(panel, "RowCount");
        string ilkSatir = Reflect.Call<string>(panel, "RowText", 0);
        string ikinciSatir = Reflect.Call<string>(panel, "RowText", 1);
        bool ilkKilitli = Reflect.Call<bool>(panel, "RowLocked", 0);
        bool ikinciKilitli = Reflect.Call<bool>(panel, "RowLocked", 1);
        float kaydirma = scroll.content.anchoredPosition.y;

        TextMeshProUGUI sayac = Reflect.Get<TextMeshProUGUI>(panel, "counterText");
        Image halka = Reflect.Get<Image>(panel, "progressRing");

        Debug.Log($"{Tag}_PANEL: satir={satir} kaydirma y={kaydirma:F1}");
        Debug.Log($"{Tag}_PANEL: satir0=\"{ilkSatir}\" kilit={ilkKilitli}");
        Debug.Log($"{Tag}_PANEL: satir1=\"{ikinciSatir}\" kilit={ikinciKilitli}");
        Debug.Log($"{Tag}_PANEL: sayac=\"{(sayac != null ? sayac.text : "-")}\" " +
                  $"halka={(halka != null ? halka.fillAmount : -1f):F3}");

        Assert.AreEqual(Girdiler().Count, satir, "katalogdaki her tarif listelenmeli");
        Assert.AreEqual(21, satir, "21 tarif satırı olmalı");
        Assert.AreEqual(0f, kaydirma, 0.5f, "panel en üstten başlamalı");

        Assert.IsFalse(ilkKilitli, "keşfedilen satırda kilit görünmemeli");
        Assert.IsTrue(ikinciKilitli, "keşfedilmeyen satırda kilit görünmeli");
        StringAssert.Contains("?", ikinciSatir, "keşfedilmeyen satır '?' yazmalı");
        StringAssert.DoesNotContain("?", ilkSatir, "keşfedilen satır sembolleri göstermeli");

        Assert.IsNotNull(sayac, "sayaç bağlı değil");
        StringAssert.Contains("1", sayac.text, "sayaç keşfedilen sayısını yazmalı");
        Assert.AreEqual(1f / 21f, halka.fillAmount, 0.01f, "ilerleme halkası oranı");
    }

    // Panel ikinci açılışta da en üstten başlar (kapat-aç).
    [UnityTest]
    public IEnumerator PanelIkinciAcilistaDaEnUstten()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");

        List<float> olculen = new List<float>();

        for (int tur = 0; tur < 3; tur++)
        {
            Reflect.Call(menu, "OpenRecipeBook");
            yield return new WaitForSeconds(0.8f);

            MonoBehaviour panel = Object.FindObjectsOfType<MonoBehaviour>(true)
                .FirstOrDefault(m => m.GetType().Name == "RecipeBookPanel");
            ScrollRect scroll = Reflect.Get<ScrollRect>(panel, "scrollRect");

            // Oyuncu aşağı kaydırmış gibi davran
            scroll.content.anchoredPosition = new Vector2(0f, 900f);
            yield return null;

            Reflect.Call(menu, "BackToMainMenu");
            yield return new WaitForSeconds(0.8f);

            Reflect.Call(menu, "OpenRecipeBook");
            yield return new WaitForSeconds(0.8f);

            olculen.Add(scroll.content.anchoredPosition.y);
        }

        Debug.Log($"{Tag}_PANEL_TEKRAR: kaydirma [{string.Join(", ", olculen.Select(v => v.ToString("F1")))}]");
        foreach (float v in olculen) Assert.AreEqual(0f, v, 0.5f, "her açılışta en üstten başlamalı");
    }

    // ------------------------------------------------- bildirim çakışması

    /// <summary>Bir TMP'nin ekran dikdörtgeni (overlay canvas: dünya = piksel).</summary>
    private static Rect EkranKutusu(TMP_Text tmp)
    {
        Vector3[] c = new Vector3[4];
        tmp.rectTransform.GetWorldCorners(c);
        return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
    }

    private static List<TMP_Text> MetniOlanlar(string metin)
    {
        return Object.FindObjectsOfType<TMP_Text>(true)
            .Where(t => t.gameObject.activeInHierarchy && t.text == metin)
            .ToList();
    }

    // Yeni tarif bildirimi, kombo yazısı ve tepkime denklemi AYNI karede
    // doğduğunda üçünün dikdörtgenleri kesişmemeli.
    [UnityTest]
    public IEnumerator BildirimKomboVeDenklemleCakismaz()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.Data.showReactionEquations = 1;
        SaveService.Data.discoveredRecipes.Clear();      // birleşme YENİ keşif olsun
        SaveService.Data.recipeBackfillDone = 1;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        MonoBehaviour display = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "ReactionEquationDisplay");
        Assert.IsNotNull(grid, "GridManager yok");
        Assert.IsNotNull(display, "ReactionEquationDisplay yok");

        var cells = Reflect.Get<List<Transform>>(grid, "cells");
        var basics = Reflect.Get<GameObject[]>(grid, "basicElements");

        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform c = cell.GetChild(i);
                c.SetParent(null);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        GameObject h = basics.First(x => x.name == "Tile_H");
        foreach (int index in new[] { 0, 1 })
        {
            GameObject tile = Object.Instantiate(h, cells[index]);
            tile.name = h.name;
            tile.transform.localScale = Vector3.one;
            tile.transform.localPosition = Vector3.zero;
        }

        Reflect.Call(grid, "Shift", Vector2.left);       // H + H -> H2 (yeni keşif + kombo)
        yield return null;

        List<TMP_Text> bildirimler = MetniOlanlar(Loc.Get(CodeStrings.RecipeDiscovered));
        List<TMP_Text> kombolar = MetniOlanlar(Loc.Get(CodeStrings.Combo1));

        var denklemler = (System.Collections.IList)Reflect.Get(display, "ActiveScreenRects");

        Assert.AreEqual(1, bildirimler.Count, "yeni tarif bildirimi görünmeli");
        Assert.AreEqual(1, kombolar.Count, "kombo yazısı görünmeli");
        Assert.AreEqual(1, denklemler.Count, "tepkime denklemi görünmeli");

        Rect bildirim = EkranKutusu(bildirimler[0]);
        Rect kombo = EkranKutusu(kombolar[0]);
        Rect denklem = (Rect)denklemler[0];

        Debug.Log($"{Tag}_CAKISMA: bildirim  x {bildirim.xMin:F0}..{bildirim.xMax:F0} y {bildirim.yMin:F0}..{bildirim.yMax:F0}");
        Debug.Log($"{Tag}_CAKISMA: kombo     x {kombo.xMin:F0}..{kombo.xMax:F0} y {kombo.yMin:F0}..{kombo.yMax:F0}");
        Debug.Log($"{Tag}_CAKISMA: denklem   x {denklem.xMin:F0}..{denklem.xMax:F0} y {denklem.yMin:F0}..{denklem.yMax:F0}");

        Assert.IsFalse(bildirim.Overlaps(kombo), "bildirim kombo yazısıyla çakışıyor");
        Assert.IsFalse(bildirim.Overlaps(denklem), "bildirim tepkime denklemiyle çakışıyor");
        Assert.IsFalse(kombo.Overlaps(denklem), "kombo yazısı tepkime denklemiyle çakışıyor");

        // Bildirim kendi bandında: tahtanın altında
        RectTransform board = Reflect.Get<RectTransform>(grid, "BoardRect");
        Vector3[] corners = new Vector3[4];
        board.GetWorldCorners(corners);
        Debug.Log($"{Tag}_CAKISMA: tahta alt kenari y={corners[0].y:F0}");
        Assert.Less(bildirim.yMax, corners[0].y + 1f, "bildirim tahtanın altında kalmalı");
    }
}
