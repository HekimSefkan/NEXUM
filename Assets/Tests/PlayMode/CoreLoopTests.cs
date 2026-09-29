using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Çekirdek döngü düzeltmelerinin regresyon testleri.
// Test derlemesi Assembly-CSharp'ı referans alamadığı için her şeye yansıma
// (reflection) ile erişilir.
public class CoreLoopTests
{
    // Doğuş animasyon penceresi + pay (GridManager.SpawnDelay = 0,3 sn)
    private const float SpawnBekleme = 0.4f;

    private MonoBehaviour grid;
    private MonoBehaviour levelManager;
    private List<Transform> cells;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        PlayerPrefs.SetInt("SelectedLevel", 0);
        PlayerPrefs.SetInt("TutorialRead_Level_0", 1);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "GridManager");
        levelManager = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "LevelManager");
        Assert.IsNotNull(grid, "GridManager yok");
        Assert.IsNotNull(levelManager, "LevelManager yok");

        cells = (List<Transform>)Field(grid, "cells").GetValue(grid);
        Assert.AreEqual(16, cells.Count, "grid 16 hücre olmalı");
    }

    // ------------------------------------------------------------- yardımcılar

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo f = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, "alan yok: " + name);
        return f;
    }

    private static object Get(object target, string name)
    {
        return Field(target, name).GetValue(target);
    }

    private static object GetProp(object target, string name)
    {
        PropertyInfo p = target.GetType().GetProperty(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(p, "özellik yok: " + name);
        return p.GetValue(target);
    }

    private object Invoke2(string method, params object[] args)
    {
        MethodInfo m = grid.GetType().GetMethod(method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "metot yok: " + method);
        return m.Invoke(grid, args);
    }

    private void Invoke(string method, params object[] args)
    {
        MethodInfo m = grid.GetType().GetMethod(method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "metot yok: " + method);
        m.Invoke(grid, args);
    }

    /// <summary>Bulunulan bölümün hedef listesi (LevelGoal nesneleri).</summary>
    private IList Goals()
    {
        var levels = (IList)Get(levelManager, "levels");
        int index = (int)Get(levelManager, "currentLevelIndex");
        return (IList)Get(levels[index], "levelGoals");
    }

    /// <summary>Bir bileşiğin taş prefab'ını tarif tablosundan bulur.</summary>
    private GameObject ResultPrefab(string tileName)
    {
        var recipes = (IList)Get(grid, "recipes");
        foreach (object r in recipes)
        {
            var prefab = (GameObject)r.GetType().GetField("resultPrefab").GetValue(r);
            if (prefab != null && prefab.name == tileName) return prefab;
        }
        Assert.Fail("tarif sonucu bulunamadı: " + tileName);
        return null;
    }

    /// <summary>İki taşın tarifindeki puan ödülü.</summary>
    private int RecipeScore(string a, string b)
    {
        var recipes = (IList)Get(grid, "recipes");
        foreach (object r in recipes)
        {
            var e1 = (GameObject)r.GetType().GetField("element1").GetValue(r);
            var e2 = (GameObject)r.GetType().GetField("element2").GetValue(r);
            if (e1 == null || e2 == null) continue;
            bool duz = e1.name == a && e2.name == b;
            bool ters = e1.name == b && e2.name == a;
            if (duz || ters) return (int)r.GetType().GetField("scoreReward").GetValue(r);
        }
        Assert.Fail($"tarif yok: {a} + {b}");
        return 0;
    }

    private GameObject BasicElement(string name)
    {
        var basics = (GameObject[])Get(grid, "basicElements");
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

    private string TileName(int index)
    {
        return cells[index].childCount > 0
            ? cells[index].GetChild(0).name.Replace("(Clone)", "")
            : null;
    }

    private int TileCount()
    {
        int n = 0;
        foreach (Transform cell in cells) if (cell.childCount > 0) n++;
        return n;
    }

    /// <summary>Birleşmesiz hamle için hazırlık: sayaçları sıfırla, iki taş koy.</summary>
    private void BirlesmesizTahta(int gameMode)
    {
        Field(grid, "currentGameMode").SetValue(grid, gameMode);
        Field(grid, "catalystCharge").SetValue(grid, 0);
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        SetBoard(new[]
        {
            "",   "",   "", "",
            "",   "",   "", "",
            "",   "",   "", "",
            "H",  "Fe", "", ""
        });
    }

    private void Shift(string direction)
    {
        Vector2 v = direction == "up" ? Vector2.up
                  : direction == "down" ? Vector2.down
                  : direction == "left" ? Vector2.left : Vector2.right;
        Invoke("Shift", v);
    }

    /// <summary>İlk hedefi verilen bileşiğe çevirir; diğer hedefleri karşılanmış sayar.</summary>
    private void SetSingleGoal(string tileName, int amount)
    {
        IList goals = Goals();
        Assert.Greater(goals.Count, 0, "bölümün hedefi yok");
        for (int i = 0; i < goals.Count; i++)
        {
            object g = goals[i];
            if (i == 0)
            {
                g.GetType().GetField("targetPrefab").SetValue(g, ResultPrefab(tileName));
                g.GetType().GetField("targetAmount").SetValue(g, amount);
                g.GetType().GetField("currentAmount").SetValue(g, 0);
            }
            else
            {
                // Diğer hedefler zaten karşılanmış olsun (targetAmount 0 bölme hatası yapar)
                g.GetType().GetField("targetAmount").SetValue(g, 1);
                g.GetType().GetField("currentAmount").SetValue(g, 1);
            }
        }
    }

    private int GoalAmount(int index)
    {
        object g = Goals()[index];
        return (int)g.GetType().GetField("currentAmount").GetValue(g);
    }

    // ------------------------------------------------------------------ testler

    // Tek bir kaydırmada iki H2 üretilir; hedef 1 tanedir. Kazanma ekranı yalnızca
    // bir kez tetiklenmeli, hedef sayacı hedefi aşmamalı.
    [UnityTest]
    public IEnumerator KazanmaBirKezTetiklenir()
    {
        SetSingleGoal("Tile_H2", 1);

        string[] layout =
        {
            "H", "H", "", "",
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        };
        SetBoard(layout);

        Shift("left");

        bool hasWon = (bool)Get(grid, "hasWon");
        int amount = GoalAmount(0);
        Debug.Log($"NEXUM_WIN_TEST: hasWon={hasWon} hedef={amount}/1");

        Assert.IsTrue(hasWon, "kazanma bayrağı kurulmalı");
        Assert.AreEqual(1, amount, "aynı kaydırmadaki ikinci birleşme hedefi bir daha artırmamalı");
        yield return null;
    }

    // Geri alma, taşların yanında skoru, hedef sayacını, katalizör şarjını ve
    // toplam sentezi de hamle öncesine döndürmeli.
    [UnityTest]
    public IEnumerator GeriAlmaTumSayaclariGeriAlir()
    {
        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GameManager");
        Assert.IsNotNull(gm, "GameManager yok");

        SetSingleGoal("Tile_H2", 5);                       // kazanma tetiklenmesin
        Field(grid, "currentUndoLimit").SetValue(grid, 5);
        Field(grid, "usedUndos").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 3);   // şarj da geri alınmalı
        Field(gm, "currentScore").SetValue(gm, 300);
        PlayerPrefs.SetInt("TotalSynthesis", 40);

        int oncekiSkor = (int)Get(gm, "currentScore");
        int oncekiHedef = GoalAmount(0);
        int oncekiSarj = (int)Get(grid, "catalystCharge");
        int oncekiSentez = PlayerPrefs.GetInt("TotalSynthesis", 0);
        int undoCost = (int)Get(grid, "undoCost");

        string[] layout =
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        };
        SetBoard(layout);

        Shift("left");                                     // H + H -> H2 (birleşme)
        yield return null;

        int sonraSkor = (int)Get(gm, "currentScore");
        Assert.AreNotEqual(oncekiSkor, sonraSkor, "birleşme skoru artırmalıydı");
        Assert.AreEqual(oncekiHedef + 1, GoalAmount(0), "birleşme hedefi artırmalıydı");

        Invoke("ExecuteUndo");
        yield return null;

        int skor = (int)Get(gm, "currentScore");
        int hedef = GoalAmount(0);
        int sarj = (int)Get(grid, "catalystCharge");
        int sentez = PlayerPrefs.GetInt("TotalSynthesis", 0);
        Debug.Log($"NEXUM_UNDO_TEST: skor={skor} (beklenen {oncekiSkor - undoCost}) " +
                  $"hedef={hedef}/{oncekiHedef} sarj={sarj}/{oncekiSarj} sentez={sentez}/{oncekiSentez}");

        Assert.AreEqual(oncekiSkor - undoCost, skor, "skor hamle öncesine dönüp bedel düşülmeli");
        Assert.AreEqual(oncekiHedef, hedef, "hedef sayacı geri alınmalı");
        Assert.AreEqual(oncekiSarj, sarj, "katalizör şarjı geri alınmalı");
        Assert.AreEqual(oncekiSentez, sentez, "toplam sentez geri alınmalı");
    }

    // Bölüm kazanılınca o bölümün skoru toplam skora bir kez eklenmeli.
    [UnityTest]
    public IEnumerator KazaninceToplamSkorBirikir()
    {
        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GameManager");
        Assert.IsNotNull(gm, "GameManager yok");

        PlayerPrefs.SetInt("TotalScore", 100);
        Field(grid, "currentGameMode").SetValue(grid, 0);   // Normal mod: puan x1
        Field(gm, "currentScore").SetValue(gm, 0);
        SetSingleGoal("Tile_H2", 1);

        int odul = RecipeScore("Tile_H", "Tile_H");

        // İki H2 üretilir ama kazanma (ve toplama ekleme) yalnızca ilkinde olur
        SetBoard(new[]
        {
            "H", "H", "", "",
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Shift("left");
        yield return null;

        int toplam = PlayerPrefs.GetInt("TotalScore", 0);
        Debug.Log($"NEXUM_TOTALSCORE_TEST: toplam={toplam} (beklenen {100 + odul})");
        Assert.AreEqual(100 + odul, toplam, "kazanma anindaki skor toplama bir kez eklenmeli");
    }

    // Taşlar bir hücre değil, duvara ya da önlerindeki engele kadar kaymalı.
    [UnityTest]
    public IEnumerator KaymaEngeleKadarGider()
    {
        Field(grid, "catalystCharge").SetValue(grid, 0);

        // 1) Boş satırda taş duvara kadar gider (3 hücre)
        SetBoard(new[]
        {
            "", "", "", "H",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        Shift("left");
        yield return null;
        Debug.Log($"NEXUM_SLIDE_TEST duvar: [0]={TileName(0)} [3]={TileName(3)}");
        Assert.AreEqual("Tile_H", TileName(0), "taş duvara kadar kaymalı");

        // 2) Birleşemeyen bir taşın önünde durur (Fe + H tarifi yok)
        Field(grid, "catalystCharge").SetValue(grid, 0);
        SetBoard(new[]
        {
            "Fe", "", "", "H",
            "",   "", "", "",
            "",   "", "", "",
            "",   "", "", ""
        });
        Shift("left");
        yield return null;
        Debug.Log($"NEXUM_SLIDE_TEST engel: [0]={TileName(0)} [1]={TileName(1)}");
        Assert.AreEqual("Tile_Fe", TileName(0), "engel yerinde kalmalı");
        Assert.AreEqual("Tile_H", TileName(1), "taş engelin önünde durmalı");
    }

    // Aynı hamlede üretilen bileşik ikinci kez birleşmemeli (2048 kuralı).
    [UnityTest]
    public IEnumerator UretilenBilesikAyniHamledeTekrarBirlesmez()
    {
        Field(grid, "catalystCharge").SetValue(grid, 0);
        SetSingleGoal("Tile_H2O", 5);

        // O, H, H -> H+H birleşip H2 olur; H2 + O aynı hamlede birleşmemeli
        SetBoard(new[]
        {
            "O", "H", "H", "",
            "",  "",  "",  "",
            "",  "",  "",  "",
            "",  "",  "",  ""
        });
        Shift("left");
        yield return null;
        Debug.Log($"NEXUM_SLIDE_TEST kilit: [0]={TileName(0)} [1]={TileName(1)}");

        Assert.AreEqual("Tile_O", TileName(0), "O yerinde kalmalı");
        Assert.AreEqual("Tile_H2", TileName(1), "H2 aynı hamlede O ile birleşmemeli");
    }

    // Melez spawn: birleşme olmayan her 2 hamlede bir yeni taş gelir.
    [UnityTest]
    public IEnumerator MelezSpawnIkiBosHamledeBirTasEkler()
    {
        BirlesmesizTahta(gameMode: 0);
        int baslangic = TileCount();

        // Yeni taş animasyonlar bittikten sonra doğduğu için her hamleden
        // sonra doğuş penceresi kadar beklenir.
        Shift("up");                                       // 1. birleşmesiz hamle
        yield return new WaitForSeconds(SpawnBekleme);
        int birHamle = TileCount();

        Shift("down");                                     // 2. birleşmesiz hamle
        yield return new WaitForSeconds(SpawnBekleme);
        int ikiHamle = TileCount();

        Debug.Log($"NEXUM_SPAWN_TEST melez: baslangic={baslangic} 1.hamle={birHamle} 2.hamle={ikiHamle}");
        Assert.AreEqual(baslangic, birHamle, "ilk birleşmesiz hamlede taş gelmemeli");
        Assert.AreEqual(baslangic + 1, ikiHamle, "ikinci birleşmesiz hamlede taş gelmeli");
    }

    // Katalizör şarjı sentezle dolar, birleşmesiz hamle onu sıfırlamaz;
    // dolunca bedava Joker hakkı verir ve şarj başa döner.
    [UnityTest]
    public IEnumerator KatalizorSarjiSentezleDolar()
    {
        Field(grid, "currentGameMode").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 0);
        Field(grid, "freeJokerCharges").SetValue(grid, 0);
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        SetSingleGoal("Tile_H2", 99);                      // kazanma tetiklenmesin

        int limit = (int)grid.GetType().GetField("CatalystChargeLimit",
            BindingFlags.Public | BindingFlags.Static).GetValue(null);

        // Tek kaydırmada 2 sentez -> şarj 2
        SetBoard(new[]
        {
            "H", "H", "", "",
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Shift("left");
        yield return new WaitForSeconds(SpawnBekleme);
        int ikiSentez = (int)Get(grid, "catalystCharge");

        // Birleşmesiz hamle şarjı sıfırlamamalı
        Shift("left");
        yield return new WaitForSeconds(SpawnBekleme);
        int bosHamleSonrasi = (int)Get(grid, "catalystCharge");

        // Şarjı sınırın bir altına kur, tek sentez daha yap -> ödül
        Field(grid, "catalystCharge").SetValue(grid, limit - 1);
        Field(grid, "freeJokerCharges").SetValue(grid, 0);
        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });
        Shift("left");
        yield return new WaitForSeconds(SpawnBekleme);

        int sarj = (int)Get(grid, "catalystCharge");
        int bedava = (int)Get(grid, "freeJokerCharges");
        Debug.Log($"NEXUM_KATALIZOR: 2 sentez -> {ikiSentez}, bos hamle sonrasi {bosHamleSonrasi}, " +
                  $"sinir {limit} asilinca sarj={sarj} bedavaJoker={bedava}");

        Assert.AreEqual(2, ikiSentez, "tek kaydırmadaki iki sentez şarjı 2 artırmalı");
        Assert.AreEqual(2, bosHamleSonrasi, "birleşmesiz hamle şarjı sıfırlamamalı");
        Assert.AreEqual(1, bedava, "şarj dolunca bedava Joker hakkı verilmeli");
        Assert.AreEqual(0, sarj, "ödülden sonra şarj başa dönmeli");
    }

    // Bedava Joker hakkı bir kez harcanır.
    [UnityTest]
    public IEnumerator BedavaJokerBirKezHarcanir()
    {
        Field(grid, "freeJokerCharges").SetValue(grid, 2);

        bool ilk = (bool)Invoke2("TryConsumeFreeJoker");
        bool ikinci = (bool)Invoke2("TryConsumeFreeJoker");
        bool ucuncu = (bool)Invoke2("TryConsumeFreeJoker");
        int kalan = (int)Get(grid, "freeJokerCharges");

        Debug.Log($"NEXUM_KATALIZOR_JOKER: {ilk}/{ikinci}/{ucuncu} kalan={kalan}");
        Assert.IsTrue(ilk, "ilk kullanım bedava olmalı");
        Assert.IsTrue(ikinci, "ikinci kullanım bedava olmalı");
        Assert.IsFalse(ucuncu, "hak bitince bedava olmamalı");
        Assert.AreEqual(0, kalan);
        yield return null;
    }

    // Yeni taş, kayma ve birleşme animasyonları bitmeden doğmamalı.
    [UnityTest]
    public IEnumerator YeniTasAnimasyonlardanSonraDogar()
    {
        Field(grid, "currentGameMode").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 0);
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        SetSingleGoal("Tile_H2", 5);

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        float t0 = Time.time;
        Shift("left");                                     // H + H -> H2, doğuş istendi
        int hemen = TileCount();
        bool bekleyen = (bool)GetProp(grid, "HasPendingSpawn");

        yield return null;
        int birKare = TileCount();

        yield return new WaitForSeconds(0.35f);
        int sonra = TileCount();
        float gecen = Time.time - t0;

        Debug.Log($"NEXUM_SPAWN_ZAMAN: hemen={hemen} birKare={birKare} " +
                  $"{gecen:F2}sn sonra={sonra} (bekleyen={bekleyen})");

        Assert.AreEqual(1, hemen, "kaydırma biter bitmez yeni taş olmamalı");
        Assert.IsTrue(bekleyen, "doğuş bekliyor olmalı");
        Assert.AreEqual(1, birKare, "bir kare sonra da yeni taş olmamalı");
        Assert.AreEqual(2, sonra, "animasyonlar bitince yeni taş doğmalı");
    }

    // Yeni taş, kaydırma yönünün tersindeki kenarda doğmalı.
    [UnityTest]
    public IEnumerator YeniTasKarsiKenardaDogar()
    {
        Field(grid, "currentGameMode").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 0);
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        SetSingleGoal("Tile_H2", 5);

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        Shift("left");                                     // sola kaydırıldı
        yield return new WaitForSeconds(0.4f);

        int yeni = -1;
        for (int i = 0; i < 16; i++)
        {
            if (i == 0) continue;                          // birleşme ürünü
            if (cells[i].childCount > 0) { yeni = i; break; }
        }

        Debug.Log($"NEXUM_SPAWN_KONUM: sola kaydırıldı, yeni taş hücre {yeni} (sütun {yeni % 4})");
        Assert.AreNotEqual(-1, yeni, "yeni taş doğmalı");
        Assert.AreEqual(3, yeni % 4, "sola kaydırınca yeni taş en sağ sütunda doğmalı");
    }

    // Oyuncu animasyonu beklemeden hamle yaparsa bekleyen taş önce doğar.
    [UnityTest]
    public IEnumerator BekleyenTasSonrakiHamleden0nceDogar()
    {
        Field(grid, "currentGameMode").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 0);
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        SetSingleGoal("Tile_H2", 5);

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        Shift("left");
        Assert.IsTrue((bool)GetProp(grid, "HasPendingSpawn"), "doğuş bekliyor olmalı");
        Assert.AreEqual(1, TileCount());

        Shift("up");                                       // animasyon beklenmeden ikinci hamle
        int sonra = TileCount();
        Debug.Log($"NEXUM_SPAWN_ARA: ikinci hamleden hemen sonra tas={sonra}");

        Assert.GreaterOrEqual(sonra, 2, "bekleyen taş ikinci hamleden önce doğmalı");
        yield return null;
    }

    // Hiçbir şeyi değiştirmeyen kaydırma geri alma yığınını şişirmemeli.
    [UnityTest]
    public IEnumerator GecersizHamleSnapshotBirakmaz()
    {
        string[] layout =
        {
            "H", "",  "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        };
        SetBoard(layout);

        var history = (System.Collections.ICollection)Get(grid, "historyStack");
        Shift("left");                                     // sola dayalı tek taş: değişiklik yok
        yield return null;
        int sonra = history.Count;

        Shift("left");
        yield return null;
        Debug.Log($"NEXUM_UNDO_SNAPSHOT: yigin={history.Count} (ilk gecersiz hamleden sonra {sonra})");

        Assert.AreEqual(sonra, history.Count, "geçersiz hamle yığına snapshot eklememeli");
    }
}
