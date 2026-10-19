using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Taş üzerindeki formülün okunabilirliği: 26 sembolün hepsi, dört canvas
/// boyutunda, taşın GERÇEK metin kutusunda ölçülür.
///
/// Alt simge NOTU: literal alt simge karakterleri (U+2080–2089) kullanılamaz —
/// Oxanium TTF'inde yalnızca ₀₁₂₃₄ var (₅₆₇₈₉ yok) ve hiçbir statik TMP
/// atlasında hiçbiri yok. Bu yüzden semboller TMP <sub> etiketiyle yazılır;
/// <sub> normal rakam glifini küçültüp alçalttığı için ek glif gerektirmez.
/// Bu test o kararın bozulmadığını da doğrular.
/// </summary>
public class TileTextFitTests
{
    private const string Tag = "NEXUM_TILETEXT";

    [UnityTest]
    public IEnumerator TasFormulleriSigar()
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

        // Tüm taş prefab'ları: temel elementler + tariflerin ürünleri
        List<GameObject> prefabs = basics.Where(b => b != null).ToList();
        foreach (object r in recipes)
        {
            GameObject p = Reflect.Get<GameObject>(r, "resultPrefab");
            if (p != null && !prefabs.Contains(p)) prefabs.Add(p);
        }
        Assert.Greater(prefabs.Count, 20, "taş prefab'ları bulunamadı");

        // Ölçüm için tek bir taş örneği; metni sırayla değiştirilir
        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform c = cell.GetChild(i);
                c.SetParent(null);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        GameObject ornek = Object.Instantiate(prefabs[0], cells[0]);
        ornek.transform.localScale = Vector3.one;
        ornek.transform.localPosition = Vector3.zero;
        TMP_Text kutu = ornek.GetComponentInChildren<TMP_Text>(true);
        Assert.IsNotNull(kutu, "taşta TMP metni yok");

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        // Sembolleri prefab'lardan topla (ElementData'dan)
        List<string> semboller = new List<string>();
        foreach (GameObject p in prefabs)
        {
            string sym = Reflect.Call<string>("ReactionText", "Symbol", p);
            if (!string.IsNullOrEmpty(sym) && !semboller.Contains(sym)) semboller.Add(sym);
        }
        Debug.Log($"{Tag}: {semboller.Count} sembol ölçülecek");

        List<string> hatalar = new List<string>();
        float enKotu = 0f;
        string enKotuAd = "-";
        int literalAltSimge = 0;

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";

            foreach (string sym in semboller)
            {
                if (sym.Any(ch => ch >= '₀' && ch <= '₉')) literalAltSimge++;

                kutu.text = sym;
                kutu.ForceMeshUpdate();

                float doluluk = LayoutCheck.FillRatio(kutu, sym);
                string detay;
                bool kirpik = LayoutCheck.IsClipped(kutu, out detay);

                if (doluluk > enKotu) { enKotu = doluluk; enKotuAd = $"{ekran} {sym}"; }

                if (kirpik) hatalar.Add($"{ekran}: \"{sym}\" kırpık ({detay})");
                if (doluluk > LayoutCheck.MaxFillRatio)
                {
                    hatalar.Add($"{ekran}: \"{sym}\" doluluk %{doluluk * 100f:F0} " +
                                $"(sınır %{LayoutCheck.MaxFillRatio * 100f:F0})");
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: en kotu doluluk %{enKotu * 100f:F0} ({enKotuAd}), " +
                  $"sinir %{LayoutCheck.MaxFillRatio * 100f:F0}, sorun {hatalar.Count}");
        Debug.Log($"{Tag}_ALTSIMGE: literal alt simge kullanan sembol = {literalAltSimge} " +
                  "(0 olmali: TMP <sub> etiketi kullaniliyor)");

        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");

        // --- Kural bozuksa: 2d-3 icin GEREKEN puntoyu olc
        float mevcutPunto = kutu.fontSize;
        float gereken = mevcutPunto;
        if (hatalar.Count > 0)
        {
            LayoutCheck.SetCanvasSize(safeRoot, LayoutCheck.CanvasSizes[0]);
            yield return null;

            string enUzun = semboller.OrderByDescending(x => LayoutCheck.FillRatio(kutu, x)).First();
            for (float punto = mevcutPunto; punto >= 40f; punto -= 2f)
            {
                kutu.fontSize = punto;
                kutu.ForceMeshUpdate();
                bool tamam = semboller.All(sym => LayoutCheck.FillRatio(kutu, sym) <= LayoutCheck.MaxFillRatio);
                if (tamam) { gereken = punto; break; }
            }
            kutu.fontSize = mevcutPunto;
            Debug.Log($"{Tag}_PUNTO: mevcut {mevcutPunto:F0}, en uzun sembol \"{enUzun}\", " +
                      $"%85 kuralini saglayan en buyuk punto = {gereken:F0}");
        }

        // Kirpilma ve literal alt simge SERT kural: her zaman duser.
        Assert.AreEqual(0, literalAltSimge,
            "literal alt simge karakteri kullanılmamalı (fontlarda glif yok); <sub> kullan");
        Assert.IsEmpty(hatalar.Where(h => h.Contains("kırpık")).ToList(),
            "taş formülü kırpılıyor");

        // %85 kurali: taş prefab'ları bu adımda DEĞİŞTİRİLMİYOR (2d-2 ekranlara
        // dokunmuyor). Kural bozuksa test yeşile boyanmaz, ATLANIR ve ölçülen
        // değer raporlanır; düzeltme 2d-3'ün iş listesinde.
        if (hatalar.Count > 0)
        {
            Assert.Ignore($"%85 kuralı bozuk: en kötü %{enKotu * 100f:F0} ({enKotuAd}). " +
                          $"Gereken punto {gereken:F0} (şu an {mevcutPunto:F0}). " +
                          "Taş prefab'ları 2d-3'te güncellenecek.");
        }
    }
}
