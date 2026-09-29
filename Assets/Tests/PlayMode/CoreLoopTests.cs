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
