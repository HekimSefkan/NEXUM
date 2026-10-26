using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Taş seviyelerinin AYIRT EDİLEBİLİRLİĞİ (2d-3a düzeltmesi, madde 2.4).
///
/// Kullanıcı hedefleri:
///   komşu seviyeler >= 1,6:1 · L1 &lt;-&gt; L4 >= 3:1 · boş hücre &lt;-&gt; dolu taş >= 2,5:1
/// Ek kısıt (madde 2.2): formül BEYAZ, yani iç alanda beyaz >= 4,5:1.
///
/// <b>Bu dördü İÇ ALANDA aynı anda sağlanamaz.</b> Beyaz metin iç alanı
/// L &lt;= 0,183 ile sınırlar; siyahtan (L+0,05 = 0,05) o tavana (0,233) kadar
/// olan dinamik aralık 4,67x'tir, oysa 2,5 x 1,6^3 = 10,24 gerekir.
/// Bu yüzden merdiven METNİN OLMADIĞI ÇERÇEVEDE taşınıyor; iç alan dört
/// seviyede de tek ve koyu kalıyor — madde 2.1 ile aynı yöne bakar
/// ("süsleme iç alanın dışında yaşayacak").
///
/// Ölçüm GERÇEK RENDER'DAN (madde 2.4: "gerçek render'dan, piksel okuyarak
/// ölç"): tahta dolu olarak çizilir, formüller kapatılır, her hücrenin dış
/// %10'luk şeridi ve merkezi örneklenir.
/// </summary>
public class TileContrastTests
{
    private const string Tag = "NEXUM_TASKONTRAST";
    private const float BandRatio = 0.10f;
    private static readonly Vector2Int Boyut = new Vector2Int(1080, 1920);

    private const float NeighbourMin = 1.6f;
    private const float EndsMin = 3.0f;
    private const float EmptyMin = 2.5f;
    private const float TextMin = 4.5f;
    private const int GreyStepMin = 20;

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator SeviyelerAyirtEdilebiliyor()
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
        Assert.IsNotNull(cells, "cells yok");
        Assert.GreaterOrEqual(cells.Count, 16, "16 hücre bekleniyordu");

        var basics = Reflect.Get<GameObject[]>(grid, "basicElements");
        var recipes = (System.Collections.IList)Reflect.Get(grid, "recipes");

        // Her seviyeden bir prefab seç
        List<GameObject> prefabs = basics.Where(b => b != null).ToList();
        foreach (object r in recipes)
        {
            GameObject p = Reflect.Get<GameObject>(r, "resultPrefab");
            if (p != null && !prefabs.Contains(p)) prefabs.Add(p);
        }

        Dictionary<int, GameObject> seviyeTas = new Dictionary<int, GameObject>();
        foreach (GameObject p in prefabs)
        {
            Component tv = p.GetComponent("TileVisual");
            if (tv == null) continue;
            int lv = Reflect.Get<int>(tv, "level");
            if (lv >= 1 && lv <= 4 && !seviyeTas.ContainsKey(lv)) seviyeTas[lv] = p;
        }
        Assert.AreEqual(4, seviyeTas.Count, "dört seviyeden taş bulunamadı");

        // Tahtayı kur: 0..3 dört seviye, 4 BOŞ
        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform c = cell.GetChild(i);
                c.SetParent(null);
                Object.DestroyImmediate(c.gameObject);
            }
        }
        for (int lv = 1; lv <= 4; lv++)
        {
            GameObject tas = Object.Instantiate(seviyeTas[lv], cells[lv - 1]);
            tas.transform.localScale = Vector3.one;
            tas.transform.localPosition = Vector3.zero;
        }

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        if (safeRoot != null) LayoutCheck.SetCanvasSize(safeRoot, LayoutCheck.CanvasSizes[0]);
        yield return null;
        yield return null;

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        // Formüller kapalı: ölçülen şey YÜZEY
        PixelProbe.Session oturum = PixelProbe.Begin(canvas.gameObject, Boyut, true);
        Assert.IsNotNull(oturum, "render oturumu açılamadı");
        yield return null;                       // canvas yeniden kurulumu kare sonunda
        yield return null;   // WaitForEndOfFrame batch mode'da güvenilir değil
        PixelProbe.Render(oturum);
        Assert.IsFalse(PixelProbe.IsBlank(oturum),
            "render boş geldi: süit -nographics ile koşulmuş olabilir");
        Assert.IsNotNull(oturum.frame, "render alınamadı");

        Dictionary<int, Color> serit = new Dictionary<int, Color>();
        Dictionary<int, Color> ic = new Dictionary<int, Color>();
        for (int lv = 1; lv <= 4; lv++)
        {
            RectInt? r = PixelProbe.Bounds(oturum, (RectTransform)cells[lv - 1]);
            Assert.IsNotNull(r, $"L{lv} hücresi karede bulunamadı");
            serit[lv] = PixelProbe.BandMean(oturum.frame, r.Value, BandRatio);
            ic[lv] = PixelProbe.CenterMode(oturum.frame, r.Value);
        }

        RectInt? bosR = PixelProbe.Bounds(oturum, (RectTransform)cells[4]);
        Assert.IsNotNull(bosR, "boş hücre karede bulunamadı");
        Color bos = PixelProbe.CenterMode(oturum.frame, bosR.Value);

        PixelProbe.End(oturum);

        for (int s = 1; s <= 4; s++)
        {
            Debug.Log($"{Tag}_SEVIYE: L{s} şerit {PixelProbe.Hex(serit[s])} " +
                      $"L={PixelProbe.Luminance(serit[s]):F4} gri={Gri(serit[s])}  " +
                      $"iç {PixelProbe.Hex(ic[s])} L={PixelProbe.Luminance(ic[s]):F4}");
        }
        Debug.Log($"{Tag}_SEVIYE: BOŞ {PixelProbe.Hex(bos)} L={PixelProbe.Luminance(bos):F4} " +
                  $"gri={Gri(bos)}");

        List<string> hatalar = new List<string>();

        // --- iç alan dört seviyede de AYNI (madde 2.1)
        for (int s = 2; s <= 4; s++)
        {
            float fark = PixelProbe.Contrast(ic[1], ic[s]);
            if (fark > 1.05f)
            {
                hatalar.Add($"L1 ile L{s} iç alanı farklı ({PixelProbe.Hex(ic[1])} / " +
                            $"{PixelProbe.Hex(ic[s])}, {fark:F2}:1) — iç alan tek olmalı");
            }
        }

        for (int s = 1; s <= 3; s++)
        {
            Kontrol(hatalar, $"L{s}<->L{s + 1} şerit",
                    PixelProbe.Contrast(serit[s], serit[s + 1]), NeighbourMin);
        }
        Kontrol(hatalar, "L1<->L4 şerit", PixelProbe.Contrast(serit[1], serit[4]), EndsMin);
        Kontrol(hatalar, "boş <-> dolu iç alan", PixelProbe.Contrast(bos, ic[1]), EmptyMin);
        Kontrol(hatalar, "beyaz metin / iç alan",
                PixelProbe.Contrast(Color.white, ic[1]), TextMin);

        // --- gri tonlama: renk tek başına sinyal değil
        int[] gri = { Gri(bos), Gri(serit[1]), Gri(serit[2]), Gri(serit[3]), Gri(serit[4]) };
        string[] ad = { "BOŞ", "L1", "L2", "L3", "L4" };
        for (int i = 0; i < gri.Length - 1; i++)
        {
            int d = Mathf.Abs(gri[i + 1] - gri[i]);
            Debug.Log($"{Tag}_GRI: {ad[i]} -> {ad[i + 1]}  {d}");
            if (d < GreyStepMin)
            {
                hatalar.Add($"gri tonlamada {ad[i]} -> {ad[i + 1]} farkı {d} (alt sınır {GreyStepMin})");
            }
        }

        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    private static void Kontrol(List<string> hatalar, string ad, float deger, float sinir)
    {
        Debug.Log($"{Tag}_OLCUM: {ad,-24} {deger:F2}:1  hedef >= {sinir:F1}");
        if (deger < sinir) hatalar.Add($"{ad} {deger:F2}:1 (hedef >= {sinir:F1})");
    }

    private static int Gri(Color c)
    {
        return Mathf.RoundToInt(Mathf.Pow(Mathf.Max(0f, PixelProbe.Luminance(c)), 1f / 2.2f) * 255f);
    }
}
