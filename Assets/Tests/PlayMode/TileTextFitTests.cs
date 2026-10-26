using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

        // HER TAŞ KENDİ PUNTOSUYLA ölçülür. Punto artık formül başına
        // pişiriliyor (CaCO₃ 44 ... "H" 72); tek örnek taşın metnini
        // değiştirerek ölçmek, bütün sembolleri O taşın puntosunda ölçerdi.
        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        List<TMP_Text> kutular = new List<TMP_Text>();
        List<string> semboller = new List<string>();
        // Hepsi AYNI hücrenin altına konur (26 sembol, 16 hücre): taş
        // hücreye gerildiği için her örnek aynı 200x200 kutuyu alır.
        for (int i = 0; i < prefabs.Count; i++)
        {
            string sym = Reflect.Call<string>("ReactionText", "Symbol", prefabs[i]);
            if (string.IsNullOrEmpty(sym) || semboller.Contains(sym)) continue;

            GameObject tas = Object.Instantiate(prefabs[i], cells[0]);
            tas.transform.localScale = Vector3.one;
            tas.transform.localPosition = Vector3.zero;

            TMP_Text t = tas.GetComponentInChildren<TMP_Text>(true);
            if (t == null) { Object.DestroyImmediate(tas); continue; }

            semboller.Add(sym);
            kutular.Add(t);
        }
        Assert.Greater(semboller.Count, 10, "ölçülecek sembol bulunamadı");
        Debug.Log($"{Tag}: {semboller.Count} sembol ölçülecek (her biri kendi puntosunda)");

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

            for (int si = 0; si < semboller.Count; si++)
            {
                string sym = semboller[si];
                TMP_Text kutu = kutular[si];
                if (sym.Any(ch => ch >= '₀' && ch <= '₉')) literalAltSimge++;

                kutu.text = sym;
                kutu.ForceMeshUpdate();

                float doluluk = LayoutCheck.FillRatio(kutu, sym);
                float dolulukW = LayoutCheck.FillRatioWidth(kutu, sym);
                int satir = LayoutCheck.LineCount(kutu, sym);
                string detay;
                bool kirpik = LayoutCheck.IsClipped(kutu, out detay);

                float enKotuYerel = Mathf.Max(doluluk, dolulukW);
                if (enKotuYerel > enKotu) { enKotu = enKotuYerel; enKotuAd = $"{ekran} {sym}"; }

                if (kirpik) hatalar.Add($"{ekran}: \"{sym}\" kırpık ({detay})");

                // GENİŞLİK: satır kırma KAPALI ölçülür. FillRatio yalnızca
                // yüksekliğe bakar ve IsClipped kırma açıkken genişlik
                // sorununu bildiremez (çizilen genişlik tanımı gereği kutuyu
                // aşamaz). CaCO₃ 2d-3a'da tam buradan kaçtı.
                if (dolulukW > LayoutCheck.MaxFillRatio)
                {
                    hatalar.Add($"{ekran}: \"{sym}\" GENİŞLİK doluluğu " +
                                $"%{dolulukW * 100f:F0} (sınır %{LayoutCheck.MaxFillRatio * 100f:F0})");
                }

                // Kimyasal formül tek satırdır: kırılma kusurdur.
                if (satir > 1)
                {
                    hatalar.Add($"{ekran}: \"{sym}\" {satir} satıra kırılıyor (tek satır olmalı)");
                }

                if (doluluk > LayoutCheck.MaxFillRatio)
                {
                    hatalar.Add($"{ekran}: \"{sym}\" YÜKSEKLİK doluluğu %{doluluk * 100f:F0} " +
                                $"(sınır %{LayoutCheck.MaxFillRatio * 100f:F0})");
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: en kotu doluluk %{enKotu * 100f:F0} ({enKotuAd}), " +
                  $"sinir %{LayoutCheck.MaxFillRatio * 100f:F0}, sorun {hatalar.Count}");
        Debug.Log($"{Tag}_ALTSIMGE: literal alt simge kullanan sembol = {literalAltSimge} " +
                  "(0 olmali: TMP <sub> etiketi kullaniliyor)");

        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");

        // --- Kural bozuksa: her sembol için GEREKEN puntoyu raporla
        if (hatalar.Count > 0)
        {
            LayoutCheck.SetCanvasSize(safeRoot, LayoutCheck.CanvasSizes[0]);
            yield return null;

            for (int si = 0; si < semboller.Count; si++)
            {
                TMP_Text kutu = kutular[si];
                string sym = semboller[si];
                float mevcut = kutu.fontSize;
                float gereken = mevcut;

                for (float punto = Reflect.TileMaxFont(); punto >= Reflect.TileMinFont(); punto -= 1f)
                {
                    kutu.fontSize = punto;
                    kutu.ForceMeshUpdate();
                    string d2;
                    if (LayoutCheck.FillRatio(kutu, sym) <= LayoutCheck.MaxFillRatio
                        && LayoutCheck.FillRatioWidth(kutu, sym) <= LayoutCheck.MaxFillRatio
                        && LayoutCheck.LineCount(kutu, sym) == 1
                        && !LayoutCheck.IsClipped(kutu, out d2))
                    {
                        gereken = punto;
                        break;
                    }
                }
                kutu.fontSize = mevcut;

                if (!Mathf.Approximately(gereken, mevcut))
                {
                    Debug.Log($"{Tag}_PUNTO: \"{sym}\" mevcut {mevcut:F0}, gereken {gereken:F0}");
                }
            }
        }

        // Kirpilma ve literal alt simge SERT kural: her zaman duser.
        Assert.AreEqual(0, literalAltSimge,
            "literal alt simge karakteri kullanılmamalı (fontlarda glif yok); <sub> kullan");
        Assert.IsEmpty(hatalar.Where(h => h.Contains("kırpık")).ToList(),
            "taş formülü kırpılıyor");

        // %85 kuralı artık SERT: taş prefab'larında punto 80 -> 74 uygulandı
        // (2d-3a). Atlanan test kabul edilmiyor.
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    /// <summary>
    /// PİŞMİŞ PUNTO BAYATLAMASI.
    ///
    /// Puntolar <c>NexumTileVisuals</c> tarafından hücre ölçüsünden
    /// hesaplanıp prefab'a DONDURULUYOR. Tahta yerleşimi değişirse (3b'de
    /// olabilir) donmuş değerler sessizce yanlış kalır — belge↔tablo
    /// testiyle aynı sınıf bir boşluk.
    ///
    /// Bu test aramayı BAĞIMSIZ olarak yeniden yapar (kurucunun kodunu
    /// paylaşmaz: paylaşsaydı ortak bir hata ikisini birden geçerdi) ve
    /// pişmiş değerle karşılaştırır.
    /// </summary>
    [UnityTest]
    public IEnumerator PismisPuntolarGuncel()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        GridLayoutGroup board = Object.FindObjectsOfType<GridLayoutGroup>(true)
            .FirstOrDefault(g => g.name == "GridBoard");
        Assert.IsNotNull(board, "GridBoard yok");

        float icOran = Reflect.TileContentRatio();
        float kutu = board.cellSize.x * icOran;
        Debug.Log($"{Tag}_KUTU: cellSize {board.cellSize.x:F0} x içOran {icOran:F2} = {kutu:F0} birim");

        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        var cells = Reflect.Get<List<Transform>>(grid, "cells");
        var basics = Reflect.Get<GameObject[]>(grid, "basicElements");
        var recipes = (System.Collections.IList)Reflect.Get(grid, "recipes");

        List<GameObject> prefabs = basics.Where(b => b != null).ToList();
        foreach (object r in recipes)
        {
            GameObject p = Reflect.Get<GameObject>(r, "resultPrefab");
            if (p != null && !prefabs.Contains(p)) prefabs.Add(p);
        }

        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform c = cell.GetChild(i);
                c.SetParent(null);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        List<string> hatalar = new List<string>();
        float minPunto = float.MaxValue, maxPunto = 0f;

        foreach (GameObject prefab in prefabs)
        {
            GameObject ornek = Object.Instantiate(prefab, cells[0]);
            ornek.transform.localScale = Vector3.one;
            ornek.transform.localPosition = Vector3.zero;

            TMP_Text tmp = ornek.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) { Object.DestroyImmediate(ornek); continue; }

            float pismis = tmp.fontSize;
            minPunto = Mathf.Min(minPunto, pismis);
            maxPunto = Mathf.Max(maxPunto, pismis);

            float beklenen = Reflect.TileMinFont();
            float tavan = Reflect.TileMaxFont();
            for (float f = tavan; f >= Reflect.TileMinFont(); f -= 1f)
            {
                tmp.fontSize = f;
                tmp.ForceMeshUpdate();
                Vector2 ister = tmp.GetPreferredValues(tmp.text, 0f, 0f);
                if (ister.x <= kutu * LayoutCheck.MaxFillRatio &&
                    ister.y <= kutu * LayoutCheck.MaxFillRatio)
                {
                    beklenen = f;
                    break;
                }
            }
            tmp.fontSize = pismis;

            if (!Mathf.Approximately(pismis, beklenen))
            {
                hatalar.Add($"{prefab.name}: pişmiş {pismis:F0}, " +
                            $"bugünkü hücreden hesaplanan {beklenen:F0}");
            }

            Object.DestroyImmediate(ornek);
            yield return null;
        }

        Debug.Log($"{Tag}_PISMIS: {prefabs.Count} taş, punto {minPunto:F0}..{maxPunto:F0}, " +
                  $"bayat {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_BAYAT: {h}");

        Assert.IsEmpty(hatalar,
            "pişmiş puntolar bugünkü hücre ölçüsüyle uyuşmuyor; " +
            "NEXUM -> Taş Görsellerini Bağla komutunu çalıştırın | " +
            string.Join(" | ", hatalar.Take(6)));
    }
}
