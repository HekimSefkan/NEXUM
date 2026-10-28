using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Taş durum sprite'ları ÇALIŞMA ZAMANINDA bağlı mı (3b/B6).
///
/// <c>TileVisual</c>'da beş sprite alanı vardı ama yalnızca <c>newborn</c>
/// tetikleniyordu; <c>selected</c>, <c>merging</c> ve <c>discovered</c> hiç
/// çağrılmıyordu. Bağlanmazsa "yeni doğan taş normalden daha parlak"
/// kararı hiç çalışmaz — sprite üretilmiş ama kullanılmamış olur.
///
/// Ölçüm GERÇEK RENDER'DAN: dört seviyeden taş tahtaya konur, bir karede
/// normal hâlleri, ikinci karede <c>newborn</c> hâlleri ölçülür.
/// Kabul: <b>her seviyede newborn parlaklığı normalden yüksek.</b>
/// </summary>
public class TileStateTests
{
    private const string Tag = "NEXUM_TASDURUM";
    private static readonly Vector2Int Boyut = new Vector2Int(1080, 1920);

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator YeniDoganTasNormaldenParlak()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        var cells = Reflect.Get<List<Transform>>(grid, "cells");
        var basics = Reflect.Get<GameObject[]>(grid, "basicElements");
        var recipes = (System.Collections.IList)Reflect.Get(grid, "recipes");

        List<GameObject> prefabs = basics.Where(x => x != null).ToList();
        foreach (object r in recipes)
        {
            GameObject p = Reflect.Get<GameObject>(r, "resultPrefab");
            if (p != null && !prefabs.Contains(p)) prefabs.Add(p);
        }

        // Her seviyeden bir taş
        Dictionary<int, GameObject> seviye = new Dictionary<int, GameObject>();
        foreach (GameObject p in prefabs)
        {
            Component tv = p.GetComponent("TileVisual");
            if (tv == null) continue;
            int lv = Reflect.Get<int>(tv, "level");
            if (lv >= 1 && lv <= 4 && !seviye.ContainsKey(lv)) seviye[lv] = p;
        }
        Assert.AreEqual(4, seviye.Count, "dört seviyeden taş bulunamadı");

        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform c = cell.GetChild(i);
                c.SetParent(null);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        List<Component> gorseller = new List<Component>();
        for (int lv = 1; lv <= 4; lv++)
        {
            GameObject tas = Object.Instantiate(seviye[lv], cells[lv - 1]);
            tas.transform.localScale = Vector3.one;
            tas.transform.localPosition = Vector3.zero;
            gorseller.Add(tas.GetComponent("TileVisual"));
        }

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        if (safeRoot != null) LayoutCheck.SetCanvasSize(safeRoot, LayoutCheck.CanvasSizes[0]);
        yield return null;

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        // --- 1) normal
        float[] normalL = new float[4];
        yield return Olc(canvas, cells, normalL);

        // --- 2) newborn (uzun süre: ölçüm bitene kadar normale dönmesin)
        foreach (Component v in gorseller)
        {
            Assert.IsNotNull(v, "TileVisual yok");
            Reflect.Call(v, "PlayNewborn", 30f);
        }
        yield return null;

        float[] newbornL = new float[4];
        yield return Olc(canvas, cells, newbornL);

        List<string> hatalar = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"{Tag}_SEVIYE: L{i + 1} normal L={normalL[i]:F4}  " +
                      $"newborn L={newbornL[i]:F4}  fark {newbornL[i] - normalL[i]:+0.0000;-0.0000}");
            if (newbornL[i] <= normalL[i])
            {
                hatalar.Add($"L{i + 1}: newborn {newbornL[i]:F4} <= normal {normalL[i]:F4}");
            }
        }

        // Durum alanlarının hepsi bağlı mı?
        foreach (Component v in gorseller)
        {
            foreach (string alan in new[] { "normal", "selected", "merging", "newborn", "discovered" })
            {
                if (Reflect.Get<Sprite>(v, alan) == null)
                {
                    hatalar.Add($"{v.gameObject.name}: {alan} sprite'ı bağlı değil");
                }
            }
        }

        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    private static IEnumerator Olc(Transform canvas, List<Transform> cells, float[] cikti)
    {
        PixelProbe.Session oturum = PixelProbe.Begin(canvas.gameObject, Boyut, true);
        Assert.IsNotNull(oturum, "render oturumu açılamadı");
        yield return null;
        yield return null;
        PixelProbe.Render(oturum);
        Assert.IsFalse(PixelProbe.IsBlank(oturum),
            "render boş geldi: süit -nographics ile koşulmuş olabilir");

        for (int lv = 1; lv <= 4; lv++)
        {
            RectInt? r = PixelProbe.Bounds(oturum, (RectTransform)cells[lv - 1]);
            Assert.IsNotNull(r, $"L{lv} hücresi karede bulunamadı");
            cikti[lv - 1] = PixelProbe.Luminance(PixelProbe.BandMean(oturum.frame, r.Value, 0.10f));
        }

        PixelProbe.End(oturum);
    }
}
