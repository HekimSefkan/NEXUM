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
}
