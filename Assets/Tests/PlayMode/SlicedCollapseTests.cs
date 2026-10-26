using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// ÇÖKME DEDEKTÖRÜ — eşiksiz.
///
/// 2d-3a faciasının tam imzası: <c>Image.type = Sliced</c> olan bir öğenin
/// rect'i sprite'ın KENARLIK TOPLAMINA eşitlenir. Ayarlar panelindeki üç mod
/// butonu 56×56'ya düştü, çünkü <c>btn_secondary_normal</c>'ın kenarlığı
/// 28+28 = 56 ve <c>VerticalLayoutGroup</c> bu değeri <c>preferredWidth</c>
/// olarak okudu.
///
/// Diğer iki kural bunu TEK BAŞINA yakalayamaz:
///   - 144 kuralı artık saydam <c>TouchArea</c>'yı ölçüyor; çökmüş butonun
///     dokunma alanı 144 olduğu için geçer.
///   - Kardeş kuralı medyana bağlı; ÜÇÜ BİRDEN çökerse medyan da çöker ve
///     hiçbiri eşiğin altında kalmaz.
///
/// Bu test eşik kullanmaz: 9-slice bir görselin ORTA ALANI sıfırlanmışsa
/// (genişlik veya yükseklik, kenarlık toplamını aşmıyorsa) çizim zaten
/// bozuktur — köşeler üst üste biner, dolgu kaybolur.
/// </summary>
public class SlicedCollapseTests
{
    private const string Tag = "NEXUM_COKME";

    /// <summary>Orta alan bu kadardan küçükse çökmüş sayılır (yuvarlama payı).</summary>
    private const float Pay = 2f;

    [UnityTest]
    public IEnumerator MenuSliced9DilimCokmemis()
    {
        yield return Olc("MainMenu");
    }

    [UnityTest]
    public IEnumerator OyunSliced9DilimCokmemis()
    {
        yield return Olc("Game");
    }

    /// <summary>
    /// Kaydırma çubuğu ve tutamacı muaf: 20 birimlik bir çubukta Unity'nin
    /// kendi sprite'ının 10+10 kenarlığı orta alanı zaten sıfırlar. Bu bir
    /// çökme değil, çubuğun tasarımı (bkz. docs/DESIGN_TOKENS.md dokunma
    /// hedefi istisnası).
    /// </summary>
    private static bool KaydirmaCubugu(Transform t)
    {
        while (t != null)
        {
            if (t.GetComponent<Scrollbar>() != null) return true;
            t = t.parent;
        }
        return false;
    }

    private IEnumerator Olc(string sahne)
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene(sahne);
        yield return null;
        yield return new WaitForSeconds(1f);

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        // Kapalı panellerin içindeki 9-slice'lar da ölçülmeli
        foreach (Transform p in PixelProbe.Panels(canvas))
        {
            p.gameObject.SetActive(true);
            p.localScale = Vector3.one;
        }

        List<string> hatalar = new List<string>();
        int olculen = 0;
        float enDar = float.MaxValue;
        string enDarAd = "-";

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";

            foreach (Image img in canvas.GetComponentsInChildren<Image>(true))
            {
                if (img == null || img.type != Image.Type.Sliced) continue;
                if (img.sprite == null) continue;
                if (KaydirmaCubugu(img.transform)) continue;

                Vector4 b = img.sprite.border;
                if (b == Vector4.zero) continue;

                RectTransform rt = img.rectTransform;
                float ortaW = rt.rect.width - (b.x + b.z);
                float ortaH = rt.rect.height - (b.y + b.w);
                olculen++;

                float dar = Mathf.Min(ortaW, ortaH);
                if (dar < enDar) { enDar = dar; enDarAd = $"{ekran} {PixelProbe.Path(rt)}"; }

                if (ortaW < Pay || ortaH < Pay)
                {
                    hatalar.Add($"{ekran}: {PixelProbe.Path(rt)} " +
                                $"rect {rt.rect.width:F0}x{rt.rect.height:F0}, " +
                                $"{img.sprite.name} kenarlık ({b.x:F0},{b.y:F0},{b.z:F0},{b.w:F0}) " +
                                $"-> orta alan {ortaW:F0}x{ortaH:F0} ÇÖKMÜŞ");
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: {sahne} ölçülen {olculen} Sliced görsel, " +
                  $"en dar orta alan {enDar:F0} ({enDarAd}), sorun {hatalar.Count}");
        foreach (string h in hatalar.Distinct().Take(20)) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar.Distinct().ToList(),
            string.Join(" | ", hatalar.Distinct().Take(6)));
    }
}
