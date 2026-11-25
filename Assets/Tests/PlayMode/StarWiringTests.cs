using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

/// <summary>
/// YILDIZLARIN OYUNA BAĞLANDIĞI NOKTALAR (Faz 2 / 2.2).
///
/// <c>StarTests</c> kuralı sahnesiz ölçüyor; burada ölçülen, o kuralın
/// GERÇEK oyunda doğru yerden beslenmesi:
///  - ipucu ve ÜCRETLİ Joker bayrağı yazılıyor, BEDAVA Joker yazmıyor,
///  - geri alma hamle sayısını geri alıyor ama yardım bayrağını almıyor,
///  - kazanma ekranı yıldızı değerlendirip kaydediyor ve karta yazıyor,
///  - serbest modda kazanma ekranı hiç gelmiyor (yıldız imkânsız).
/// </summary>
public class StarWiringTests
{
    private const string Tag = "NEXUM_YILDIZ_BAGLANTI";

    private MonoBehaviour grid;
    private MonoBehaviour ui;
    private MonoBehaviour gm;
    private List<Transform> cells;
    private string yedek;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();
        SaveService.ImportRaw(null);
        SaveService.Data.xpBackfillDone = 1;
        SaveService.Data.starBackfillDone = 1;
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);
        XpSources.Varsayilana();

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Find("GridManager");
        ui = Find("UIManager");
        gm = Find("GameManager");
        Assert.IsNotNull(grid, "GridManager yok");
        Assert.IsNotNull(ui, "UIManager yok");
        Assert.IsNotNull(gm, "GameManager yok");

        cells = (List<Transform>)Get(grid, "cells");
    }

    [TearDown]
    public void GeriYukle()
    {
        Time.timeScale = 1f;
        SaveService.ImportRaw(yedek);
    }

    // ------------------------------------------------------------ yardımcı

    private static MonoBehaviour Find(string tip)
    {
        return Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == tip);
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo f = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, "alan yok: " + name);
        return f;
    }

    private static object Get(object target, string name) { return Field(target, name).GetValue(target); }

    private void Invoke(object target, string method, params object[] args)
    {
        MethodInfo m = target.GetType().GetMethod(method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "metot yok: " + method);
        m.Invoke(target, args);
    }

    // LevelRun Assembly-CSharp'ta; test derlemesi onu referans ALAMAZ.
    private static int Hamle() { return Reflect.Prop<int>("LevelRun", "Moves"); }
    private static bool IpucuKullanildi() { return Reflect.Prop<bool>("LevelRun", "HintUsed"); }
    private static bool UcretliJoker() { return Reflect.Prop<bool>("LevelRun", "PaidJokerUsed"); }
    private static int Yildizlar() { return Reflect.Prop<int>("LevelRun", "Stars"); }

    private GameObject BasicElement(string name)
    {
        GameObject[] basics = (GameObject[])Get(grid, "basicElements");
        return basics.FirstOrDefault(b => b.name == name);
    }

    private void SetBoard(string[] layout)
    {
        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform child = cell.GetChild(i);
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }

        for (int i = 0; i < 16; i++)
        {
            if (string.IsNullOrEmpty(layout[i])) continue;
            GameObject prefab = BasicElement("Tile_" + layout[i]);
            Assert.IsNotNull(prefab, "temel element yok: " + layout[i]);
            GameObject tile = Object.Instantiate(prefab, cells[i]);
            tile.name = prefab.name;
            tile.transform.localScale = Vector3.one;
            tile.transform.localPosition = Vector3.zero;
        }
    }

    private void HedefiUzaklastir()
    {
        MonoBehaviour lm = Find("LevelManager");
        System.Collections.IList levels = (System.Collections.IList)Get(lm, "levels");
        int index = (int)Get(lm, "currentLevelIndex");
        System.Collections.IList goals =
            (System.Collections.IList)Get(levels[index], "levelGoals");

        foreach (object g in goals)
        {
            g.GetType().GetField("targetAmount").SetValue(g, 99);
            g.GetType().GetField("currentAmount").SetValue(g, 0);
        }
    }

    // ==================================================== yardım bayrakları

    [UnityTest]
    public IEnumerator IpucuKullanmak_UcuncuYildiziKacirir()
    {
        HedefiUzaklastir();
        Reflect.Call("LevelRun", "Reset");
        Assert.IsFalse(IpucuKullanildi(), "başlangıçta ipucu bayrağı açık");

        // Birleşebilir bir çift koy ki ipucu gerçekten gösterilsin
        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Reflect.Set(gm, "currentScore", 10000);
        Field(grid, "usedHints").SetValue(grid, 0);

        Invoke(grid, "RequestHint");
        yield return null;

        Debug.Log($"{Tag}_IPUCU: kullanildi={IpucuKullanildi()}, " +
                  $"usedHints={Get(grid, "usedHints")}");

        Assert.IsTrue(IpucuKullanildi(), "ipucu bayrağı yazılmadı");

        int mask = StarRules.Evaluate(0, 1, IpucuKullanildi(), UcretliJoker(), true);
        Assert.IsFalse(StarRules.Has(mask, StarRules.NoHelp));
    }

    /// <summary>
    /// GERİ ALMA hamle sayısını geri alır ama YARDIM bayrağını almaz:
    /// ipucunu gerçekten gördün.
    /// </summary>
    [UnityTest]
    public IEnumerator GeriAlma_HamleyiGeriAlir_YardimBayraginiAlmaz()
    {
        HedefiUzaklastir();
        Field(grid, "currentUndoLimit").SetValue(grid, 99);
        Field(grid, "usedUndos").SetValue(grid, 0);
        Field(grid, "usedHints").SetValue(grid, 0);
        Reflect.Set(gm, "currentScore", 100000);

        string[] layout =
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        };
        SetBoard(layout);
        Reflect.Call("LevelRun", "Reset");

        Invoke(grid, "RequestHint");           // yardım alındı
        yield return null;
        Assert.IsTrue(IpucuKullanildi(), "ölçüm anlamsız: ipucu gösterilmedi");

        Invoke(grid, "Shift", Vector2.left);
        yield return null;
        int hamleSonra = Hamle();

        Invoke(grid, "ExecuteUndo");
        yield return null;

        Debug.Log($"{Tag}_UNDO: hamle {hamleSonra} -> {Hamle()}, " +
                  $"ipucu bayragi {IpucuKullanildi()}");

        Assert.Greater(hamleSonra, 0, "ölçüm anlamsız: hamle sayılmadı");
        Assert.Less(Hamle(), hamleSonra, "geri alma hamle sayısını geri almadı");
        Assert.IsTrue(IpucuKullanildi(), "geri alma yardım bayrağını sildi");
    }

    /// <summary>
    /// Katalizörün BEDAVA Joker hakkı üçüncü yıldızı bozmaz; ücretli
    /// Joker bozar. İki yol da gerçek <c>TileInteraction</c> üzerinden
    /// ölçülüyor.
    /// </summary>
    [UnityTest]
    public IEnumerator BedavaJoker_BozmazUcretliJoker_Bozar()
    {
        HedefiUzaklastir();
        Reflect.Set(gm, "currentScore", 100000);
        Reflect.Set(gm, "isJokerModeActive", true);

        SetBoard(new[]
        {
            "H", "O", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Reflect.Call("LevelRun", "Reset");

        // --- 1) BEDAVA: katalizör hakkı var
        Field(grid, "freeJokerCharges").SetValue(grid, 1);
        MonoBehaviour tile = cells[0].GetChild(0).GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "TileInteraction");
        Assert.IsNotNull(tile, "TileInteraction yok");

        Invoke(tile, "OnPointerDown", (UnityEngine.EventSystems.PointerEventData)null);
        yield return null;

        Debug.Log($"{Tag}_JOKER_BEDAVA: ucretliJoker={UcretliJoker()}, " +
                  $"kalanBedava={Get(grid, "freeJokerCharges")}");
        Assert.IsFalse(UcretliJoker(), "bedava Joker ücretli sayıldı");

        // --- 2) ÜCRETLİ: katalizör hakkı yok
        Reflect.Set(gm, "isJokerModeActive", true);
        Field(grid, "freeJokerCharges").SetValue(grid, 0);
        MonoBehaviour tile2 = cells[1].GetChild(0).GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "TileInteraction");
        Assert.IsNotNull(tile2, "ikinci TileInteraction yok");

        Invoke(tile2, "OnPointerDown", (UnityEngine.EventSystems.PointerEventData)null);
        yield return null;

        Debug.Log($"{Tag}_JOKER_UCRETLI: ucretliJoker={UcretliJoker()}");
        Assert.IsTrue(UcretliJoker(), "ücretli Joker bayrağı yazılmadı");
    }

    // ==================================================== kazanma ekranı

    [UnityTest]
    public IEnumerator KazanmaEkrani_YildiziDegerlendirirKaydederVeKartaYazar()
    {
        Reflect.Call("LevelRun", "Reset");
        Reflect.Call("LevelRun", "AddScore", 300);

        // Sınırın ALTINDA, yardımsız: üç yıldız
        int limit = StarRules.MoveLimit(0);
        for (int i = 0; i < limit - 1; i++) Reflect.Call("LevelRun", "CountMove");

        int oncekiXp = ProgressionService.Xp;
        Invoke(ui, "ShowWinScreen");
        yield return null;

        MonoBehaviour card = Find("ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");
        bool acik = Reflect.Get<bool>(card, "starsEnabled");

        Debug.Log($"{Tag}_KAZANMA: hamle {Hamle()}/{limit}, yildiz {Yildizlar()}, " +
                  $"kayit {StarService.StarsFor(0)}, XP {oncekiXp} -> {ProgressionService.Xp}, " +
                  $"kart bayragi {acik}");

        Assert.IsTrue(acik, "sonuç kartında yıldız bayrağı kapalı");
        Assert.AreEqual(StarRules.All, Yildizlar(), "üç yıldız beklenirdi");
        Assert.AreEqual(StarRules.All, StarService.StarsFor(0), "yıldızlar kaydedilmedi");
        Assert.AreEqual(Hamle(), StarService.BestMoves(0), "en iyi hamle kaydedilmedi");
        Assert.Greater(ProgressionService.Xp, oncekiXp, "yıldız XP'si verilmedi");
    }

    [UnityTest]
    public IEnumerator SonucKarti_KacanKosuluYazar()
    {
        Reflect.Call("LevelRun", "Reset");
        Reflect.Call("LevelRun", "AddScore", 300);

        int limit = StarRules.MoveLimit(0);
        for (int i = 0; i < limit + 6; i++) Reflect.Call("LevelRun", "CountMove");

        Invoke(ui, "ShowWinScreen");
        yield return null;

        MonoBehaviour card = Find("ResultCard");
        TMP_Text hint = (TMP_Text)Reflect.Get(card, "starHintText");
        TMP_Text moves = (TMP_Text)Reflect.Get(card, "movesValueText");
        Assert.IsNotNull(hint, "yıldız yönlendirme satırı bağlı değil");

        string beklenen = Loc.Format(CodeStrings.ResultStarNeedMoves, limit, Hamle());

        Debug.Log($"{Tag}_KACAN: hamle kutusu \"{moves.text}\", satir \"{hint.text}\"");

        Assert.IsFalse(StarRules.Has(Yildizlar(), StarRules.UnderMoveLimit));
        Assert.AreEqual(beklenen, hint.text, "kaçan koşul yazılmadı");
        Assert.AreEqual(Loc.Format(CodeStrings.ResultMovesWithLimit, Hamle(), limit), moves.text,
            "hamle kutusu sınırı göstermiyor");
    }

    [UnityTest]
    public IEnumerator SonucKarti_UcYildizdaKusursuzYazar()
    {
        Reflect.Call("LevelRun", "Reset");
        Reflect.Call("LevelRun", "AddScore", 300);
        Reflect.Call("LevelRun", "CountMove");

        Invoke(ui, "ShowWinScreen");
        yield return null;

        MonoBehaviour card = Find("ResultCard");
        TMP_Text hint = (TMP_Text)Reflect.Get(card, "starHintText");

        Debug.Log($"{Tag}_KUSURSUZ: \"{hint.text}\"");
        Assert.AreEqual(Loc.Get(CodeStrings.ResultStarPerfect), hint.text);
    }

    /// <summary>
    /// SERBEST MODDA kazanma ekranı HİÇ GELMEZ: hedef sayacı işlemiyor,
    /// <c>CheckWinCondition</c> hemen dönüyor. Yıldız bu yüzden
    /// imkânsız — kural ayrıca <c>starsAllowed</c> ile de kapalı.
    /// </summary>
    [UnityTest]
    public IEnumerator SerbestModda_KazanmaEkraniGelmez()
    {
        Field(grid, "currentGameMode").SetValue(grid, 2);
        Assert.IsTrue(Reflect.Get<bool>(grid, "IsFreeMode"), "serbest mod kurulmadı");

        GameObject winPanel = (GameObject)Get(ui, "winPanel");
        winPanel.SetActive(false);
        Reflect.Call("LevelRun", "Reset");

        // Hedefi TEK bir H2'ye indir ve birleşmeyi yap: normal modda
        // kazanma ekranı gelirdi.
        MonoBehaviour lm = Find("LevelManager");
        System.Collections.IList levels = (System.Collections.IList)Get(lm, "levels");
        System.Collections.IList goals =
            (System.Collections.IList)Get(levels[(int)Get(lm, "currentLevelIndex")], "levelGoals");
        foreach (object g in goals)
        {
            g.GetType().GetField("targetAmount").SetValue(g, 1);
            g.GetType().GetField("currentAmount").SetValue(g, 0);
        }

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Invoke(grid, "Shift", Vector2.left);
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"{Tag}_SERBEST: kazanma paneli acik mi={winPanel.activeInHierarchy}, " +
                  $"yildiz kaydi={StarService.StarsFor(0)}");

        Assert.IsFalse(winPanel.activeInHierarchy, "serbest modda kazanma ekranı açıldı");
        Assert.AreEqual(0, StarService.StarsFor(0), "serbest modda yıldız kaydedildi");
    }

    // ==================================================== hub ve harita

    [UnityTest]
    public IEnumerator BolumHaritasi_DugumlerdeYildizVar()
    {
        SaveService.Data.levelStars = new List<int> { StarRules.All, StarRules.Complete };
        SaveService.Data.unlockedLevel = 3;
        SaveService.Data.maxLevelUnlocked = 4;
        SaveService.MarkDirty();

        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(0.8f);

        MonoBehaviour[] dugumler = Object.FindObjectsOfType<MonoBehaviour>(true)
            .Where(m => m != null && m.GetType().Name == "LevelNodeStars").ToArray();

        Debug.Log($"{Tag}_HARITA: {dugumler.Length} dugum bulundu");
        Assert.AreEqual(7, dugumler.Length, "her bölüme yıldız satırı kurulmalı");

        MonoBehaviour ilk = dugumler.First(d => Reflect.Get<int>(d, "levelIndex") == 0);
        Reflect.Call(ilk, "Apply");
        yield return null;

        Image[] yildizlar = Reflect.Get<Image[]>(ilk, "stars");
        Sprite dolu = Reflect.Get<Sprite>(ilk, "fullSprite");
        Assert.IsNotNull(yildizlar, "yıldız görselleri bağlı değil");
        Assert.AreEqual(3, yildizlar.Length);

        int doluSayi = yildizlar.Count(y => y != null && y.sprite == dolu);
        Debug.Log($"{Tag}_HARITA: L1 dolu yildiz {doluSayi}/3 (kayitta uc yildiz var)");
        Assert.AreEqual(3, doluSayi, "L1'in üç yıldızı dolu görünmeli");
    }

    [UnityTest]
    public IEnumerator HubKahramanKart_SiradakiBolumunYildizlari()
    {
        SaveService.Data.levelStars = new List<int>
        {
            StarRules.All, StarRules.All, StarRules.Complete | StarRules.NoHelp
        };
        SaveService.Data.unlockedLevel = 2;        // sıradaki bölüm L3 (indeks 2)
        SaveService.Data.maxLevelUnlocked = 3;
        SaveService.MarkDirty();

        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(0.8f);

        MonoBehaviour hub = Find("MenuHub");
        Assert.IsNotNull(hub, "MenuHub yok");
        Reflect.Call(hub, "Apply");
        yield return null;

        GameObject heroStars = Reflect.Get<GameObject>(hub, "heroStars");
        Image[] gorseller = Reflect.Get<Image[]>(hub, "heroStarImages");
        Sprite dolu = Reflect.Get<Sprite>(hub, "starFullSprite");

        Assert.IsNotNull(heroStars, "kahraman kart yıldızları bağlı değil");
        Assert.IsTrue(heroStars.activeInHierarchy, "yıldız satırı gizli (HubFeatures.Stars?)");
        Assert.AreEqual(3, gorseller.Length);

        bool[] beklenen = { true, false, true };   // 1. ve 3. yıldız
        for (int i = 0; i < 3; i++)
        {
            bool suAn = gorseller[i].sprite == dolu;
            Debug.Log($"{Tag}_HUB: yuva {i} dolu={suAn} (beklenen {beklenen[i]})");
            Assert.AreEqual(beklenen[i], suAn, $"yuva {i} yanlış");
        }
    }
}
