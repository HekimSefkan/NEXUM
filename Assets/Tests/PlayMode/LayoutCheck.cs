using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Yerleşim ölçümünün TEK kaynağı: 4 canvas boyutunda taşma, çakışma ve metin
/// kırpılması kontrolü. Her yerleşim testi buradan geçer, ölçüm kodu kopyalanmaz.
///
/// Neden Editor'de değil PlayMode'da? Sahnede kayıtlı rect değerleri TMP ve
/// layout grupları yüzünden yanıltıyor; gerçek dikdörtgenler ancak çalışma
/// zamanında oluşuyor. Bu yüzden kurucular yapıyı kurar, doğrulamayı buradaki
/// ortak yardımcı yapar.
///
/// Ekran boyutu: batch mode her zaman 640x480 yüzeyle açılır, -screen-width
/// işlemez. Bu yüzden hedef REFERANS boyutu SafeAreaRoot'a dayatılır; layout ve
/// AspectRatioFitter gerçek cihazdaki gibi çalışır.
/// </summary>
public static class LayoutCheck
{
    /// <summary>CLAUDE.md'deki dört canvas boyutu.</summary>
    public static readonly Vector2[] CanvasSizes =
    {
        new Vector2(1080f, 1920f),
        new Vector2(1080f, 2114f),
        new Vector2(1080f, 2300f),
        new Vector2(1440f, 1920f),
    };

    // ------------------------------------------------------------ sahne erişimi

    public static Transform Find(string path)
    {
        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        return canvas == null ? null : canvas.Find(path);
    }

    /// <summary>SafeArea scriptini kapatır (kendi boyutunu dayatmasın).</summary>
    public static RectTransform PrepareSafeRoot()
    {
        RectTransform safeRoot = Find("SafeAreaRoot") as RectTransform;
        if (safeRoot == null) return null;

        MonoBehaviour safeArea = safeRoot.GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea");
        if (safeArea != null) safeArea.enabled = false;

        return safeRoot;
    }

    /// <summary>Canvas'ı hedef referans boyutuna sabitler.</summary>
    public static void SetCanvasSize(RectTransform safeRoot, Vector2 size)
    {
        safeRoot.anchorMin = new Vector2(0.5f, 0.5f);
        safeRoot.anchorMax = new Vector2(0.5f, 0.5f);
        safeRoot.pivot = new Vector2(0.5f, 0.5f);
        safeRoot.sizeDelta = size;
        safeRoot.anchoredPosition = Vector2.zero;
    }

    // --------------------------------------------------------------- ölçümler

    /// <summary>SafeAreaRoot yerel birimlerinde dikdörtgen (x0, y0, x1, y1).</summary>
    public static Vector4 LocalRect(RectTransform safeRoot, RectTransform rect)
    {
        Vector3[] c = new Vector3[4];
        rect.GetWorldCorners(c);
        Vector3 a = safeRoot.InverseTransformPoint(c[0]);
        Vector3 b = safeRoot.InverseTransformPoint(c[2]);
        return new Vector4(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                           Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    /// <summary>
    /// PreserveAspect / AspectRatioFitter ile çizilen bir sprite'ın GERÇEK
    /// dikdörtgeni; ardından çerçeve kalınlığı düşülerek iç içerik alanı.
    /// </summary>
    public static Vector4 AspectFittedInner(Vector4 outer, float spriteW, float spriteH,
                                            float insetLeft, float insetRight,
                                            float insetTop, float insetBottom)
    {
        float w = outer.z - outer.x;
        float h = outer.w - outer.y;
        float scale = Mathf.Min(w / spriteW, h / spriteH);
        float dw = spriteW * scale;
        float dh = spriteH * scale;
        float cx = (outer.x + outer.z) * 0.5f;
        float cy = (outer.y + outer.w) * 0.5f;
        float x0 = cx - dw * 0.5f;
        float y1 = cy + dh * 0.5f;

        return new Vector4(x0 + dw * insetLeft, y1 - dh * insetBottom,
                           x0 + dw * insetRight, y1 - dh * insetTop);
    }

    /// <summary>Dikdörtgenin sınırdan ne kadar taştığı (negatifse içeride).</summary>
    public static float Overflow(Vector4 rect, Vector4 bounds)
    {
        return Mathf.Max(
            Mathf.Max(bounds.x - rect.x, rect.z - bounds.z),
            Mathf.Max(rect.w - bounds.w, bounds.y - rect.y));
    }

    /// <summary>İki dikdörtgenin örtüşme alanı (0 ise çakışma yok).</summary>
    public static float OverlapArea(Vector4 a, Vector4 b)
    {
        float w = Mathf.Min(a.z, b.z) - Mathf.Max(a.x, b.x);
        float h = Mathf.Min(a.w, b.w) - Mathf.Max(a.y, b.y);
        return (w > 0f && h > 0f) ? w * h : 0f;
    }

    /// <summary>
    /// TMP metni kutusuna sığıyor mu? preferredHeight satır yüksekliğine dayanır;
    /// Türkçe İ/Ğ gibi harfler çizimde onu aşabildiği için GERÇEK çizim sınırı
    /// (textBounds) da hesaba katılır.
    ///
    /// SATIR KAYDIRMA: <c>preferredWidth</c> metnin TEK SATIRA sığdırılmış
    /// genişliğini verir. Kaydırmalı bir kutuda (ör. duraklat kartındaki bilgi
    /// metni) bu değer kutu genişliğinin kat kat üstünde çıkar ve rahatça sığan
    /// metin "kırpık" sayılırdı. Kaydırma açıkken genişlik GERÇEK çizim
    /// sınırından, yükseklik kutu genişliğiyle hesaplatılan tercih değerinden
    /// okunur.
    /// </summary>
    public static bool IsClipped(TMP_Text tmp, out string detail)
    {
        tmp.ForceMeshUpdate();

        float boxW = tmp.rectTransform.rect.width;
        float boxH = tmp.rectTransform.rect.height;
        Vector2 drawn = tmp.textBounds.size;

        float needW;
        float needH;

        if (tmp.enableWordWrapping)
        {
            needW = drawn.x;
            needH = Mathf.Max(tmp.GetPreferredValues(tmp.text, boxW, 0f).y, drawn.y);
        }
        else
        {
            needW = Mathf.Max(tmp.preferredWidth, drawn.x);
            needH = Mathf.Max(tmp.preferredHeight, drawn.y);
        }

        bool clipped = needH > boxH + 0.5f || needW > boxW + 0.5f;
        detail = $"TMP {needW:F0}x{needH:F0} kutu {boxW:F0}x{boxH:F0}{(clipped ? " KIRPIK" : "")}";
        return clipped;
    }

    /// <summary>Metnin kutusunu ne kadar doldurduğu (%). Sığma kuralı %85.</summary>
    public const float MaxFillRatio = 0.85f;

    /// <summary>
    /// YÜKSEKLİK doluluğu. Kutu genişliği yalnızca SATIR KIRMA KISITI olarak
    /// geçilir; dönen değer yüksekliğin oranıdır.
    /// </summary>
    public static float FillRatio(TMP_Text tmp, string content)
    {
        float boxH = tmp.rectTransform.rect.height;
        float boxW = tmp.rectTransform.rect.width;
        if (boxH <= 0f) return 0f;
        return tmp.GetPreferredValues(content, boxW, 0f).y / boxH;
    }

    /// <summary>
    /// GENİŞLİK doluluğu — satır kırma KAPALI ölçülür.
    ///
    /// NEDEN AYRI BİR ÖLÇÜ: <see cref="FillRatio"/> yalnızca yüksekliğe bakar
    /// ve <see cref="IsClipped"/> satır kırma açıkken genişlik sorununu
    /// BİLDİREMEZ (çizilen genişlik tanımı gereği kutuyu aşamaz, çünkü TMP
    /// sığdırmak için kırmıştır). 2d-3a'da CaCO₃ taşta "CaCO" / "₃" diye iki
    /// satıra kırılıyordu, Fe₂O₃ kutunun %90'ını kaplıyordu — iki test de
    /// yeşildi ve "en kötü doluluk %79" diyordu.
    /// </summary>
    public static float FillRatioWidth(TMP_Text tmp, string content)
    {
        float boxW = tmp.rectTransform.rect.width;
        if (boxW <= 0f) return 0f;

        // Kırma kısıtı yok: gerçekte İSTENEN genişlik ölçülür.
        return tmp.GetPreferredValues(content, 0f, 0f).x / boxW;
    }

    /// <summary>
    /// Metin kaç satıra düşüyor? Tek satırlık alanda kırılma kusurdur.
    /// </summary>
    public static int LineCount(TMP_Text tmp, string content)
    {
        string eski = tmp.text;
        tmp.text = content;
        tmp.ForceMeshUpdate();
        int n = tmp.textInfo != null ? tmp.textInfo.lineCount : 1;
        tmp.text = eski;
        tmp.ForceMeshUpdate();
        return n;
    }

    // ------------------------------------------------------------------ rapor

    public static string Describe(string label, Vector4 rect)
    {
        return $"{label,-20} x {rect.x,7:F0}..{rect.z,7:F0} y {rect.y,7:F0}..{rect.w,7:F0}";
    }

    /// <summary>Aynı ebeveyn altındaki kardeşlerde çakışma arar.</summary>
    public static List<string> FindOverlaps(RectTransform safeRoot, IEnumerable<RectTransform> rects)
    {
        List<RectTransform> list = rects.Where(r => r != null).ToList();
        List<string> found = new List<string>();

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                float area = OverlapArea(LocalRect(safeRoot, list[i]), LocalRect(safeRoot, list[j]));
                if (area > 1f) found.Add($"{list[i].name} x {list[j].name} = {area:F0} birim²");
            }
        }
        return found;
    }
}
