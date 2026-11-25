using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

/// <summary>
/// Bölüm sonu sonuç kartının GÖSTERDİĞİ DEĞERLER (1.6b).
///
/// Neyi koruyor:
///  1. <b>Skor kazanılan puandır, kalan değil.</b> Geri alma ve Joker
///     harcaması <c>currentScore</c>'u düşürüyor; kartta onu göstermek
///     yardım kullanan oyuncuyu cezalandırırdı. Bu en sinsi hata: kart
///     "çalışıyor" görünür, yalnızca sayı yanlıştır.
///  2. <b>Geri alma kartı şişirmez.</b> Birleştir → geri al → birleştir
///     döngüsü puanı ve hamleyi iki kez saymamalı.
///  3. <b>Yeni tepkime sayısı doğru.</b> Yalnızca bu bölümde İLK KEZ
///     görülenler; ikinci kez aynı tepkime sayılmaz.
///  4. <b>Son bölümde "Sonraki bölüm" yok</b> ve kalan buton ortaya alınır.
///  5. Kart sahneye bağlı: alanları boş bir kart testi sessizce geçmemeli.
/// </summary>
public class ResultCardTests
{
    private const string Tag = "NEXUM_SONUCKARTI";

    private string yedek;
    private MonoBehaviour ui;
    private object card;
    private GameObject cardGo;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        card = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "ResultCard");
        Assert.IsNotNull(card, "ResultCard sahnede yok (ResultCardBuilder koşuldu mu?)");
        cardGo = ((MonoBehaviour)card).gameObject;
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    // --------------------------------------------------------------- bağlar

    [UnityTest]
    public IEnumerator KartSahneyeBagli()
    {
        Assert.AreSame(card, Reflect.Get(ui, "resultCard"),
            "UIManager.resultCard sahnedeki karta bağlı değil");

        foreach (string alan in new[] { "levelText", "scoreValueText", "movesValueText",
                                        "reactionsText", "starsRow", "nextLevelButton",
                                        "restartButton" })
        {
            Assert.IsNotNull(Reflect.Get(card, alan) as Object, "boş alan: " + alan);
        }

        Object[] icons = Reflect.Get<Object[]>(card, "goalIcons");
        Object[] symbols = Reflect.Get<Object[]>(card, "goalSymbolTexts");
        Object[] amounts = Reflect.Get<Object[]>(card, "goalAmountTexts");
        Debug.Log($"{Tag}: hedef yuvası {icons.Length}/{symbols.Length}/{amounts.Length}");

        Assert.Greater(icons.Length, 0, "hedef yuvası bağlanmamış");
        Assert.AreEqual(icons.Length, symbols.Length, "sembol dizisi eksik");
        Assert.AreEqual(icons.Length, amounts.Length, "adet dizisi eksik");

        // Yıldızlar 2.2'de AÇILDI; satır ve görselleri bağlı olmalı.
        Assert.IsTrue(Reflect.Get<bool>(card, "starsEnabled"),
            "yıldız bayrağı kapalı; 2.2'de açık olmalı");
        Assert.IsNotNull(Reflect.Get(card, "starsRow") as Object, "yıldız satırı bağlanmamış");
        Assert.AreEqual(3, Reflect.Get<Image[]>(card, "starImages").Length,
            "üç yıldız yuvası bağlanmalı");
        Assert.IsNotNull(Reflect.Get(card, "starFullSprite") as Object, "dolu yıldız görseli yok");
        Assert.IsNotNull(Reflect.Get(card, "starEmptySprite") as Object, "boş yıldız görseli yok");
        yield return null;
    }

    // ------------------------------------------------------------ değerler

    [UnityTest]
    public IEnumerator SkorKazanilanPuandirKalanDegil()
    {
        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "GameManager");
        Assert.IsNotNull(gm, "GameManager yok");

        Reflect.Call("LevelRun", "Reset");
        Reflect.Call(gm, "AddScore", 500);
        Reflect.Call(gm, "AddScore", 300);

        // Harcama: geri alma bedeli gibi. Kazanılan puan DÜŞMEMELİ.
        Reflect.Call<bool>(gm, "SpendScore", 250);

        int kalan = Reflect.Get<int>(gm, "currentScore");
        int kazanilan = Reflect.Prop<int>("LevelRun", "EarnedScore");

        Debug.Log($"{Tag}: kazanılan={kazanilan} kalan={kalan}");
        Assert.AreEqual(800, kazanilan, "kazanılan puan harcamadan etkilendi");
        Assert.AreNotEqual(kazanilan, kalan, "ölçüm anlamsız: harcama kalanı düşürmedi");

        Reflect.Call(card, "Fill");
        yield return null;

        TextMeshProUGUI skor = Reflect.Get<TextMeshProUGUI>(card, "scoreValueText");
        Assert.AreEqual("800", skor.text, "kart kazanılan puanı göstermiyor");
    }

    [UnityTest]
    public IEnumerator GeriAlmaKartiSisirmez()
    {
        var grid = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "GameManager");

        // TAHTA BELİRLİ KURULUR. Rastgele doğan üç taş zaten tepede
        // olabiliyor; o zaman yukarı kaydırma hiçbir şeyi oynatmıyor, anlık
        // görüntü alınmıyor ve geri alma hiç gerçekleşmiyor. Ölçüm o koşuda
        // "anlamsız" diye düşüyordu ve düşüp düşmemesi doğma rastgeleliğine
        // bağlıydı (2.1'de tam süit koşusunda yakalandı).
        TahtayiKur(grid);
        Reflect.Call("LevelRun", "Reset");

        // Dört kaydırma: her biri anlık görüntü alır. Sonra hepsini geri al.
        List<string> iz = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            Reflect.Call(grid, "Shift", Vector2.up);
            yield return new WaitForSeconds(0.35f);
            iz.Add($"{Reflect.Prop<int>("LevelRun", "EarnedScore")}/{Reflect.Prop<int>("LevelRun", "Moves")}");
        }

        int puanSonra = Reflect.Prop<int>("LevelRun", "EarnedScore");
        int hamleSonra = Reflect.Prop<int>("LevelRun", "Moves");

        // Geri alma bedeli skordan düşüyor; bedeli karşıla ki geri alma geçsin
        Reflect.Set(gm, "currentScore", 10000);

        int geriAlinan = 0;
        for (int i = 0; i < 4; i++)
        {
            int oncekiHamle = Reflect.Prop<int>("LevelRun", "Moves");
            Reflect.Call(grid, "ExecuteUndo");
            yield return new WaitForSeconds(0.35f);
            if (Reflect.Prop<int>("LevelRun", "Moves") < oncekiHamle) geriAlinan++;
        }

        int puanGeri = Reflect.Prop<int>("LevelRun", "EarnedScore");
        int hamleGeri = Reflect.Prop<int>("LevelRun", "Moves");

        Debug.Log($"{Tag}: kaydırma izi [{string.Join(" ", iz)}] -> " +
                  $"sonra {puanSonra}/{hamleSonra}, {geriAlinan} geri alma sonrası " +
                  $"{puanGeri}/{hamleGeri}");

        Assert.Greater(geriAlinan, 0, "hiç geri alma gerçekleşmedi, ölçüm anlamsız");
        Assert.LessOrEqual(hamleGeri, hamleSonra - geriAlinan,
            "geri alma hamle sayacını geri almadı");
        Assert.LessOrEqual(puanGeri, puanSonra, "geri alma kazanılan puanı geri almadı");
    }

    [UnityTest]
    public IEnumerator YeniTepkimeSayisiDogru()
    {
        Reflect.Call("LevelRun", "Reset");

        object catalog = Reflect.Call("RecipeCatalog", "Load");
        Assert.IsNotNull(catalog, "RecipeCatalog yok");

        IList entries = Reflect.Get(catalog, "entries") as IList;
        Assert.IsNotNull(entries, "katalog girdileri okunamadı");
        Assert.Greater(entries.Count, 2, "katalog çok küçük");

        string a = Reflect.Get<string>(entries[0], "key");
        string bKey = Reflect.Get<string>(entries[1], "key");

        Reflect.Call("LevelRun", "NoteDiscovery", a);
        Reflect.Call("LevelRun", "NoteDiscovery", bKey);
        Reflect.Call("LevelRun", "NoteDiscovery", a);   // yineleme

        var liste = Reflect.Prop("LevelRun", "NewRecipes") as IEnumerable;
        int sayi = liste.Cast<object>().Count();

        Debug.Log($"{Tag}: üç çağrıdan sonra yeni tepkime = {sayi}");
        Assert.AreEqual(2, sayi, "aynı tepkime iki kez sayıldı");

        Reflect.Call(card, "Fill");
        yield return null;

        TextMeshProUGUI metin = Reflect.Get<TextMeshProUGUI>(card, "reactionsText");
        Debug.Log($"{Tag}: tepkime metni = \"{metin.text.Replace('\n', '|')}\"");
        Assert.IsFalse(string.IsNullOrWhiteSpace(metin.text), "tepkime metni boş");
        Assert.IsFalse(metin.text.Contains(Loc.Get(CodeStrings.ResultNoReactions)),
            "iki tepkime varken \"yeni tepkime yok\" yazıyor");
    }

    [UnityTest]
    public IEnumerator TepkimeYokkenAciklayiciMetin()
    {
        Reflect.Call("LevelRun", "Reset");
        Reflect.Call(card, "Fill");
        yield return null;

        TextMeshProUGUI metin = Reflect.Get<TextMeshProUGUI>(card, "reactionsText");
        Debug.Log($"{Tag}: tepkimesiz metin = \"{metin.text}\"");
        Assert.AreEqual(Loc.Get(CodeStrings.ResultNoReactions), metin.text,
            "yeni tepkime yokken kutu boş kalıyor");
    }

    // ------------------------------------------------------------ butonlar

    [UnityTest]
    public IEnumerator SonBolumdeSonrakiButonuYok()
    {
        var lm = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "LevelManager");
        Assert.IsNotNull(lm, "LevelManager yok");

        var levels = Reflect.Get(lm, "levels") as IList;
        int son = levels.Count - 1;

        GameObject next = Reflect.Get<GameObject>(card, "nextLevelButton");
        GameObject restart = Reflect.Get<GameObject>(card, "restartButton");
        RectTransform restartRect = restart.transform as RectTransform;

        // --- ortadaki bir bölüm
        Reflect.Set(lm, "currentLevelIndex", 0);
        Reflect.Call(card, "Fill");
        yield return null;

        float ciftX = restartRect.anchoredPosition.x;
        Debug.Log($"{Tag}: bölüm 1 -> sonraki={next.activeSelf} tekrarX={ciftX:F0}");
        Assert.IsTrue(next.activeSelf, "ortadaki bölümde Sonraki bölüm gizli");

        // --- son bölüm
        Reflect.Set(lm, "currentLevelIndex", son);
        Reflect.Call(card, "Fill");
        yield return null;

        float tekX = restartRect.anchoredPosition.x;
        Debug.Log($"{Tag}: bölüm {son + 1} (son) -> sonraki={next.activeSelf} tekrarX={tekX:F0}");

        Assert.IsFalse(next.activeSelf, "son bölümde Sonraki bölüm hâlâ görünüyor");
        Assert.AreNotEqual(ciftX, tekX, "tek kalan buton ortaya alınmadı, kartta boşluk kalır");
        Assert.AreEqual(0f, tekX, 0.5f, "tek kalan buton ortada değil");
    }
    /// <summary>
    /// Tahtayı boşaltıp ALT SATIRA iki taş koyar: yukarı kaydırma kesinlikle
    /// bir şey oynatır, yani anlık görüntü alınır.
    /// </summary>
    private static void TahtayiKur(MonoBehaviour grid)
    {
        List<Transform> cells = Reflect.Get<List<Transform>>(grid, "cells");
        Assert.IsNotNull(cells, "grid hücreleri okunamadı");

        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform child = cell.GetChild(i);
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }

        GameObject[] basics = Reflect.Get<GameObject[]>(grid, "basicElements");
        GameObject prefab = basics.FirstOrDefault(b => b != null && b.name == "Tile_Fe");
        Assert.IsNotNull(prefab, "Tile_Fe yok");

        // İki taş AYRI sütuna konur: yukarı kaydırma birleşme değil YER
        // DEĞİŞTİRME olsun (hedef sayacı kirlenmesin).
        foreach (int index in new[] { 12, 14 })
        {
            GameObject tile = Object.Instantiate(prefab, cells[index]);
            tile.name = prefab.name;
            tile.transform.localScale = Vector3.one;
            tile.transform.localPosition = Vector3.zero;
        }
    }
}
