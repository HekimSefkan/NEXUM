using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Kaydırma davranışının parmak izini çıkarır: sabit bir tahta ve sabit bir hamle
// dizisiyle oynanır, sonuç tek satırlık imzaya çevrilir. Refactor öncesi ve
// sonrası bu imza birebir aynı kalmalıdır.
//
// Rastgelelik Random.InitState ile sabitlenir (spawn ve seviye havuzu).
public class ShiftBehaviorTests
{
    private const string Tag = "NEXUM_SHIFT";
    private const int Seed = 20260929;

    private MonoBehaviour grid;
    private System.Collections.Generic.List<Transform> cells;

    private static readonly string[] Sequence =
    {
        "left", "up", "right", "down", "left", "left", "up", "right",
        "down", "down", "left", "up", "up", "right", "left", "down"
    };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        FieldInfo cellsField = grid.GetType().GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(cellsField, "cells alanı yok");
        cells = (System.Collections.Generic.List<Transform>)cellsField.GetValue(grid);
        Assert.AreEqual(16, cells.Count, "grid 16 hücre olmalı");
    }

    private GameObject BasicElement(string name)
    {
        GameObject[] basics = (GameObject[])grid.GetType()
            .GetField("basicElements", BindingFlags.Public | BindingFlags.Instance).GetValue(grid);
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

    private string Signature()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            if (i > 0) sb.Append(i % 4 == 0 ? " / " : ",");
            sb.Append(cells[i].childCount > 0
                ? cells[i].GetChild(0).name.Replace("(Clone)", "").Replace("Tile_", "")
                : ".");
        }
        return sb.ToString();
    }

    private void Shift(string direction)
    {
        Vector2 v = direction == "up" ? Vector2.up
                  : direction == "down" ? Vector2.down
                  : direction == "left" ? Vector2.left : Vector2.right;
        grid.GetType().GetMethod("Shift", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(grid, new object[] { v });
    }

    [UnityTest]
    public IEnumerator KaydirmaParmakIzi()
    {
        // Sabit başlangıç tahtası (satır satır)
        string[] layout =
        {
            "H", "",  "",  "H",
            "",  "O", "",  "O",
            "C", "",  "H", "",
            "",  "N", "",  "H"
        };

        Random.InitState(Seed);
        SetBoard(layout);
        Debug.Log($"{Tag}_BASLANGIC: {Signature()}");

        foreach (string dir in Sequence)
        {
            Shift(dir);
            yield return null;
            Debug.Log($"{Tag}_ADIM {dir,-5}: {Signature()}");
        }

        Debug.Log($"{Tag}_IMZA: {Signature()}");
        Assert.Pass();
    }
}
