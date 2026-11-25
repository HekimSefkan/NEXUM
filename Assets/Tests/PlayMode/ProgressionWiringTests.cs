using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// XP'NİN OYUNA BAĞLANDIĞI NOKTALARIN ÖLÇÜMÜ (Faz 2 / 2.1).
///
/// <c>ProgressionTests</c> kuralı sahnesiz ölçüyor; burada ölçülen, o
/// kuralın GERÇEK oyunda doğru yerden çağrılması:
///  - birleştir -> geri al döngüsü XP bırakmıyor,
///  - kazanma ekranı XP ödüyor ve sonuç kartı ödeneni yazıyor,
///  - kutlama paneli modal listede ve geri tuşu zincirinde,
///  - kutlama sonuç kartından SONRA açılıyor.
/// </summary>
public class ProgressionWiringTests
{
    private const string Tag = "NEXUM_XP_BAGLANTI";

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

    private void Shift(Vector2 yon) { Invoke(grid, "Shift", yon); }

    // LevelRun Assembly-CSharp'ta; test derlemesi onu referans ALAMAZ
    // (CLAUDE.md "Test derlemesi ve yansıma"). Erişim Reflect üzerinden.
    private static int Kazanilan() { return Reflect.Prop<int>("LevelRun", "EarnedScore"); }

    private static int VerilenXp() { return Reflect.Prop<int>("LevelRun", "XpAwarded"); }

    private static bool KazaOldu() { return Reflect.Prop<bool>("LevelRun", "AccidentHappened"); }

    private static int YildizMaskesi() { return Reflect.Prop<int>("LevelRun", "Stars"); }

    /// <summary>Hedefi ulaşılamaz yapar: kazanma ekranı araya girmesin.</summary>
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

    // ==================================================== geri alma ve XP

    /// <summary>
    /// ÖMÜR BOYU SAYAÇ KURALI: birleştir -> geri al döngüsü XP bırakmaz.
    ///
    /// İlk birleşme YENİ bir tarif keşfedebilir ve keşif XP'si geri
    /// ALINMAZ (keşfedilen bilgi geri alınamaz, <c>discoveredRecipes</c>
    /// ile aynı istisna). Bu yüzden ölçüm iki parçalı: ilk turdan sonraki
    /// 10 tur XP'yi HİÇ değiştirmemeli.
    /// </summary>
    [UnityTest]
    public IEnumerator BirlestirGeriAl_OnKez_XpDegismiyor()
    {
        HedefiUzaklastir();
        Field(grid, "currentUndoLimit").SetValue(grid, 99);
        Field(grid, "usedUndos").SetValue(grid, 0);
        Field(gm, "currentScore").SetValue(gm, 100000);

        string[] layout =
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        };

        // Tur 0: keşif burada olabilir, ölçümün dışında tutulur.
        SetBoard(layout);
        Shift(Vector2.left);
        yield return null;
        Invoke(grid, "ExecuteUndo");
        yield return null;

        int baslangic = ProgressionService.Xp;
        int kazanilanBaslangic = Kazanilan();

        for (int i = 0; i < 10; i++)
        {
            SetBoard(layout);
            Shift(Vector2.left);
            yield return null;
            Invoke(grid, "ExecuteUndo");
            yield return null;
        }

        Debug.Log($"{Tag}_UNDO: 10 tur sonunda XP {baslangic} -> {ProgressionService.Xp}, " +
                  $"kazanilan puan {kazanilanBaslangic} -> {Kazanilan()}, " +
                  $"defter {SaveService.Data.discoveredRecipes.Count}");

        Assert.AreEqual(baslangic, ProgressionService.Xp,
            "birlestir -> geri al dongusu XP uretti");
    }

    /// <summary>
    /// Geri alma KAZANILAN puanı da geri alıyor, yani kazanma anında
    /// ödenen XP kesinleşmiş durumu anlatıyor.
    /// </summary>
    [UnityTest]
    public IEnumerator GeriAlma_KazanilanPuaniGeriAliyor()
    {
        HedefiUzaklastir();
        Field(grid, "currentUndoLimit").SetValue(grid, 99);
        Field(grid, "usedUndos").SetValue(grid, 0);
        Field(gm, "currentScore").SetValue(gm, 100000);

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        int once = Kazanilan();
        Shift(Vector2.left);
        yield return null;
        int birlesmeSonrasi = Kazanilan();

        Invoke(grid, "ExecuteUndo");
        yield return null;
        int geriAlmaSonrasi = Kazanilan();

        Debug.Log($"{Tag}_KAZANILAN: {once} -> {birlesmeSonrasi} -> {geriAlmaSonrasi}");

        Assert.Greater(birlesmeSonrasi, once, "ölçüm anlamsız: birleşme puan vermedi");
        Assert.AreEqual(once, geriAlmaSonrasi, "kazanılan puan geri alınmadı");
    }

    // ==================================================== kazanma ekranı

    [UnityTest]
    public IEnumerator KazanmaEkrani_XpOduyorVeKartaYaziyor()
    {
        // Kazanma ekranının okuduğu değeri doğrudan kuruyoruz: ölçülen şey
        // ShowWinScreen'in XP'yi ödeyip karta yazması.
        Reflect.Call("LevelRun", "Reset");
        Reflect.Call("LevelRun", "AddScore", 500);

        int oncekiXp = ProgressionService.Xp;
        Invoke(ui, "ShowWinScreen");
        yield return null;

        int verilen = VerilenXp();

        // 2.2: kazanma ekranı bölüm XP'sinin YANINA yıldız XP'sini de
        // ödüyor. Beklenti sabit yazılmaz, kazanılan yıldızdan hesaplanır.
        int yildizXp = StarRules.CountOf(YildizMaskesi()) * ProgressionTable.StarXp;
        int beklenen = ProgressionTable.LevelClearXp
                       + 500 / ProgressionTable.ScoreXpDivisor
                       + yildizXp;

        MonoBehaviour card = Find("ResultCard");
        Assert.IsNotNull(card, "ResultCard yok");
        TextMeshProUGUI xpText = (TextMeshProUGUI)card.GetType()
            .GetField("xpValueText").GetValue(card);
        Assert.IsNotNull(xpText, "kartta XP alanı bağlı değil");

        Debug.Log($"{Tag}_KAZANMA: XP {oncekiXp} -> {ProgressionService.Xp} " +
                  $"(verilen {verilen}, beklenen {beklenen}, yildiz payi {yildizXp}), " +
                  $"kartta \"{xpText.text}\"");

        Assert.Greater(yildizXp, 0, "ölçüm anlamsız: hiç yıldız kazanılmadı");

        Assert.AreEqual(beklenen, verilen);
        Assert.AreEqual(oncekiXp + beklenen, ProgressionService.Xp);
        Assert.AreEqual(Loc.Format(CodeStrings.ResultXpValue, verilen), xpText.text,
            "kart ödenen XP'yi yazmıyor");
    }

    /// <summary>
    /// Aynı bölümü ikinci kez AYNI sonuçla bitirmek kartta "+0 XP" yazar;
    /// satır GİZLENMEZ ("XP sistemi bozuldu" izlenimi vermesin).
    /// </summary>
    [UnityTest]
    public IEnumerator AyniBolumIkinciKez_KartSifirYaziyor()
    {
        Reflect.Call("LevelRun", "Reset");
        Reflect.Call("LevelRun", "AddScore", 500);
        Invoke(ui, "ShowWinScreen");
        yield return null;

        Reflect.Call("LevelRun", "Reset");
        Reflect.Call("LevelRun", "AddScore", 500);
        Invoke(ui, "ShowWinScreen");
        yield return null;

        MonoBehaviour card = Find("ResultCard");
        TextMeshProUGUI xpText = (TextMeshProUGUI)card.GetType()
            .GetField("xpValueText").GetValue(card);

        Debug.Log($"{Tag}_TEKRAR: ikinci bitiriste kartta \"{xpText.text}\"");

        Assert.AreEqual(0, VerilenXp());
        Assert.AreEqual(Loc.Format(CodeStrings.ResultXpValue, 0), xpText.text);
        Assert.IsTrue(xpText.gameObject.activeInHierarchy, "satır gizlendi");
    }

    // ==================================================== kutlama paneli

    [UnityTest]
    public IEnumerator KutlamaPaneli_SahnedeVeKapali()
    {
        MonoBehaviour panel = Find("RankUpPanel");
        Assert.IsNotNull(panel, "RankUpPanel Game sahnesinde yok");
        Assert.IsFalse(panel.gameObject.activeSelf, "kutlama paneli açık kaydedilmiş");

        GameObject bagli = (GameObject)Get(ui, "rankUpPanel");
        Assert.AreSame(panel.gameObject, bagli, "UIManager paneli tanımıyor");
        yield return null;
    }

    [UnityTest]
    public IEnumerator KutlamaAcikken_GirdiKilitli()
    {
        SaveService.Data.xp = ProgressionTable.Ranks[2].threshold;
        SaveService.Data.rankCelebrated = 0;

        MonoBehaviour panel = Find("RankUpPanel");
        bool acildi = (bool)panel.GetType().GetMethod("ShowIfPending").Invoke(panel, null);
        yield return null;

        bool modal = (bool)ui.GetType().GetProperty("IsModalPanelOpen").GetValue(ui);
        Debug.Log($"{Tag}_KILIT: panel acildi={acildi}, IsModalPanelOpen={modal}");

        Assert.IsTrue(acildi, "bekleyen kutlama açılmadı");
        Assert.IsTrue(modal, "kutlama açıkken girdi kilidi yok");

        panel.GetType().GetMethod("Close").Invoke(panel, null);
        yield return null;

        Assert.IsFalse((bool)ui.GetType().GetProperty("IsModalPanelOpen").GetValue(ui));
        Assert.IsFalse(ProgressionService.CelebrationPending, "kapanış kutlamayı işaretlemedi");
    }

    [UnityTest]
    public IEnumerator GeriTusu_OnceKutlamayiKapatir()
    {
        SaveService.Data.xp = ProgressionTable.Ranks[2].threshold;
        SaveService.Data.rankCelebrated = 0;

        MonoBehaviour panel = Find("RankUpPanel");
        panel.GetType().GetMethod("ShowIfPending").Invoke(panel, null);
        yield return null;

        GameObject pause = (GameObject)Get(ui, "pausePanel");
        bool duraklatOnce = pause != null && pause.activeInHierarchy;

        bool cikis = Reflect.Call<bool>("Nexum.Core.BackButtonHandler", "HandleBackPress");
        yield return null;

        bool duraklatSonra = pause != null && pause.activeInHierarchy;
        Debug.Log($"{Tag}_GERI: cikis={cikis}, panel acik={panel.gameObject.activeInHierarchy}, " +
                  $"duraklat {duraklatOnce} -> {duraklatSonra}");

        Assert.IsFalse(cikis, "geri tuşu uygulamadan çıkmak istedi");
        Assert.IsFalse(panel.gameObject.activeInHierarchy, "kutlama kapanmadı");
        Assert.AreEqual(duraklatOnce, duraklatSonra, "geri tuşu duraklatı da açtı");
        Assert.IsFalse(ProgressionService.CelebrationPending,
            "geri tuşuyla kapatmak kutlamayı işaretlemedi");
    }

    /// <summary>
    /// Kutlama bölüm ORTASINDA açılmaz: kazanma ekranı gelmeden
    /// <c>ShowRankUpIfPending</c> çağrılmıyor, yani panel kapalı kalıyor.
    /// </summary>
    [UnityTest]
    public IEnumerator OynanisSirasinda_KutlamaAcilmiyor()
    {
        HedefiUzaklastir();
        SaveService.Data.xp = 0;
        SaveService.Data.rankCelebrated = 0;

        MonoBehaviour panel = Find("RankUpPanel");

        // Rütbe atlatacak kadar tarif keşfi: oynanış sırasında XP akıyor.
        for (int i = 0; i < 20; i++) ProgressionService.AwardRecipeDiscovery("t" + i);

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Shift(Vector2.left);
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"{Tag}_ORTA: XP {ProgressionService.Xp}, rutbe " +
                  $"{ProgressionService.RankIndex}, bekliyor " +
                  $"{ProgressionService.CelebrationPending}, panel " +
                  $"{panel.gameObject.activeInHierarchy}");

        Assert.IsTrue(ProgressionService.CelebrationPending, "ölçüm anlamsız: rütbe atlamadı");
        Assert.IsFalse(panel.gameObject.activeInHierarchy,
            "kutlama oyunun ortasında açıldı");
    }

    /// <summary>
    /// "Kazasız Bölüm": oyun sonu ekranı görülen oynanışta verilmez,
    /// görülmeyen oynanışta verilir.
    /// </summary>
    [UnityTest]
    public IEnumerator KazasizBolum_KazadanSonraVerilmiyor()
    {
        Reflect.Call("LevelRun", "Reset");
        Invoke(ui, "ShowGameOver");
        yield return null;

        Assert.IsTrue(KazaOldu(), "oyun sonu ekranı kaza saymadı");

        Invoke(ui, "ShowWinScreen");
        yield return null;

        Debug.Log($"{Tag}_KAZASIZ: kaza={KazaOldu()}, " +
                  $"rozet={Badges.IsEarned(Badges.FlawlessLevel)}");
        Assert.IsFalse(Badges.IsEarned(Badges.FlawlessLevel),
            "kaza olan bölümde Kazasız Bölüm rozeti verildi");

        // Kazasız oynanış: aynı sahnede sayaçlar sıfırlanır
        Reflect.Call("LevelRun", "Reset");
        Invoke(ui, "ShowWinScreen");
        yield return null;

        Assert.IsTrue(Badges.IsEarned(Badges.FlawlessLevel),
            "kazasız bitişte rozet verilmedi");
    }
}
