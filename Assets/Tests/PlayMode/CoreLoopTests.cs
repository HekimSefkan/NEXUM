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
    private void BirlesmesizTahta(int gameMode, int entropi)
    {
        Field(grid, "currentGameMode").SetValue(grid, gameMode);
        Field(grid, "emptyShiftCount").SetValue(grid, entropi);
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

    // Geri alma, taşların yanında skoru, hedef sayacını, entropiyi ve toplam
    // sentezi de hamle öncesine döndürmeli.
    [UnityTest]
    public IEnumerator GeriAlmaTumSayaclariGeriAlir()
    {
        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GameManager");
        Assert.IsNotNull(gm, "GameManager yok");

        SetSingleGoal("Tile_H2", 5);                       // kazanma tetiklenmesin
        Field(grid, "currentUndoLimit").SetValue(grid, 5);
        Field(grid, "usedUndos").SetValue(grid, 0);
        Field(grid, "emptyShiftCount").SetValue(grid, 3);  // entropi de geri alınmalı
        Field(gm, "currentScore").SetValue(gm, 300);
        PlayerPrefs.SetInt("TotalSynthesis", 40);

        int oncekiSkor = (int)Get(gm, "currentScore");
        int oncekiHedef = GoalAmount(0);
        int oncekiEntropi = (int)Get(grid, "emptyShiftCount");
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
        int entropi = (int)Get(grid, "emptyShiftCount");
        int sentez = PlayerPrefs.GetInt("TotalSynthesis", 0);
        Debug.Log($"NEXUM_UNDO_TEST: skor={skor} (beklenen {oncekiSkor - undoCost}) " +
                  $"hedef={hedef}/{oncekiHedef} entropi={entropi}/{oncekiEntropi} sentez={sentez}/{oncekiSentez}");

        Assert.AreEqual(oncekiSkor - undoCost, skor, "skor hamle öncesine dönüp bedel düşülmeli");
        Assert.AreEqual(oncekiHedef, hedef, "hedef sayacı geri alınmalı");
        Assert.AreEqual(oncekiEntropi, entropi, "entropi sayacı geri alınmalı");
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
        Field(grid, "emptyShiftCount").SetValue(grid, 0);

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
        Field(grid, "emptyShiftCount").SetValue(grid, 0);
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
        Field(grid, "emptyShiftCount").SetValue(grid, 0);
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
        BirlesmesizTahta(gameMode: 0, entropi: 0);
        int baslangic = TileCount();

        Shift("up");                                       // 1. birleşmesiz hamle
        yield return null;
        int birHamle = TileCount();

        Shift("down");                                     // 2. birleşmesiz hamle
        yield return null;
        int ikiHamle = TileCount();

        Debug.Log($"NEXUM_SPAWN_TEST melez: baslangic={baslangic} 1.hamle={birHamle} 2.hamle={ikiHamle}");
        Assert.AreEqual(baslangic, birHamle, "ilk birleşmesiz hamlede taş gelmemeli");
        Assert.AreEqual(baslangic + 1, ikiHamle, "ikinci birleşmesiz hamlede taş gelmeli");
    }

    // Serbest modda (mod 2) spawn kuralı aynıdır ama entropi ceza taşı gelmez.
    [UnityTest]
    public IEnumerator SerbestModdaEntropiCezasiYok()
    {
        // Normal mod: entropi 4 iken bir birleşmesiz hamle ceza taşını getirir
        BirlesmesizTahta(gameMode: 0, entropi: 4);
        int oncekiNormal = TileCount();
        Shift("up");
        yield return null;
        int normalSonra = TileCount();
        int normalEntropi = (int)Get(grid, "emptyShiftCount");

        // Serbest mod: aynı durumda ceza taşı gelmez, entropi de artmaz
        BirlesmesizTahta(gameMode: 2, entropi: 4);
        int oncekiSerbest = TileCount();
        Shift("up");
        yield return null;
        int serbestSonra = TileCount();
        int serbestEntropi = (int)Get(grid, "emptyShiftCount");

        Debug.Log($"NEXUM_SPAWN_TEST ceza: normal {oncekiNormal}->{normalSonra} (entropi {normalEntropi}) | " +
                  $"serbest {oncekiSerbest}->{serbestSonra} (entropi {serbestEntropi})");

        Assert.AreEqual(oncekiNormal + 1, normalSonra, "normal modda entropi cezası taş eklemeli");
        Assert.AreEqual(0, normalEntropi, "ceza sonrası entropi sıfırlanmalı");
        Assert.AreEqual(oncekiSerbest, serbestSonra, "serbest modda ceza taşı gelmemeli");
        Assert.AreEqual(4, serbestEntropi, "serbest modda entropi sayacı işlemez");
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
