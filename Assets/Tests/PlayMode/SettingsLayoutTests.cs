using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Ayarlar panelinin CAM KART üzerine oturup oturmadığını çalışma zamanında ölçer.
//
// Cam kart (GlassBackground) sprite'ı PreserveAspect ile çizildiği için görünen
// kartın dikdörtgeni RectTransform'dan KÜÇÜKTÜR ve ekran oranına göre değişir.
// Test o dikdörtgeni sprite oranından hesaplar, çerçeve kalınlığını düşerek
// "iç içerik alanını" bulur ve her öğenin o alanda kaldığını doğrular.
// Ayrıca her TMP metninin kutusu TMP'nin istediği boyutu karşılamalı (kırpılma yok).
//
// EKRAN BOYUTU: batch mode her zaman 640x480 yüzeyle açılır, -screen-width
// işe yaramaz. Bu yüzden SafeAreaRoot geçici olarak hedef REFERANS boyutuna
// sabitlenir; layout ve AspectRatioFitter gerçek ekranda olduğu gibi çalışır.
public class SettingsLayoutTests
{
    private const string Tag = "NEXUM_SETTINGS";

    // SettingsPanel_.png: 796x1280. Çerçevenin iç kenarları (pikselden orana)
    private const float SpriteW = 796f;
    private const float SpriteH = 1280f;
    private const float InnerLeft = 100f / SpriteW;
    private const float InnerRight = 690f / SpriteW;
    private const float InnerTop = 80f / SpriteH;      // üstten
    private const float InnerBottom = 1190f / SpriteH; // üstten

    private static readonly Vector2[] Sizes =
    {
        new Vector2(1080f, 1920f),
        new Vector2(1080f, 2114f),
        new Vector2(1080f, 2300f),
        new Vector2(1440f, 1920f),
    };

    private static readonly string[] Content =
    {
        "Image", "Text (TMP)", "GameModeText", "ModeContainer",
        "AudioTitle", "MusicRow", "SfxRow",
        "LearningTitle", "FlashcardsRow", "HintsRow", "ResetProgressButton",
    };

    private RectTransform safeRoot;
    private RectTransform card;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");
        menu.GetType().GetMethod("OpenSettingsPanel", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(menu, null);
        yield return new WaitForSeconds(1f);

        safeRoot = Find("SafeAreaRoot") as RectTransform;
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        // Güvenli alan scripti kendi boyutunu dayatmasın
        MonoBehaviour safeArea = safeRoot.GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "SafeArea");
        if (safeArea != null) safeArea.enabled = false;

        card = Find("SafeAreaRoot/SettingsPanel/GlassBackground") as RectTransform;
        Assert.IsNotNull(card, "GlassBackground yok");
    }

    private Transform Find(string path)
    {
        Transform root = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        return root == null ? null : root.Find(path);
    }

    private void SetCanvasSize(Vector2 size)
    {
        safeRoot.anchorMin = new Vector2(0.5f, 0.5f);
        safeRoot.anchorMax = new Vector2(0.5f, 0.5f);
        safeRoot.pivot = new Vector2(0.5f, 0.5f);
        safeRoot.sizeDelta = size;
        safeRoot.anchoredPosition = Vector2.zero;
    }

    /// <summary>SafeAreaRoot yerel birimlerinde dikdörtgen (x0, y0, x1, y1).</summary>
    private Vector4 LocalRect(RectTransform rect)
    {
        Vector3[] c = new Vector3[4];
        rect.GetWorldCorners(c);
        Vector3 a = safeRoot.InverseTransformPoint(c[0]);
        Vector3 b = safeRoot.InverseTransformPoint(c[2]);
        return new Vector4(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                           Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    private Vector4 CardInnerRect()
    {
        Vector4 r = LocalRect(card);
        float w = r.z - r.x;
        float h = r.w - r.y;
        float scale = Mathf.Min(w / SpriteW, h / SpriteH);
        float dw = SpriteW * scale;
        float dh = SpriteH * scale;
        float cx = (r.x + r.z) * 0.5f;
        float cy = (r.y + r.w) * 0.5f;
        float x0 = cx - dw * 0.5f;
        float y1 = cy + dh * 0.5f;
        return new Vector4(x0 + dw * InnerLeft, y1 - dh * InnerBottom,
                           x0 + dw * InnerRight, y1 - dh * InnerTop);
    }

    [UnityTest]
    public IEnumerator AyarlarKartinIcindeKaliyor()
    {
        List<string> hatalar = new List<string>();

        foreach (Vector2 size in Sizes)
        {
            SetCanvasSize(size);
            yield return null;
            yield return null;

            string ekran = $"{size.x:F0}x{size.y:F0}";
            Vector4 inner = CardInnerRect();
            Debug.Log($"{Tag}_KART: {ekran} | ic alan x {inner.x:F0}..{inner.z:F0} y {inner.y:F0}..{inner.w:F0}");

            foreach (string name in Content)
            {
                Transform t = FindContent(name);
                if (t == null) { Debug.Log($"{Tag}_SATIR: {ekran} | {name,-20} YOK"); continue; }

                RectTransform rt = t as RectTransform;
                Vector4 r = LocalRect(rt);

                float tasma = Mathf.Max(
                    Mathf.Max(inner.x - r.x, r.z - inner.z),
                    Mathf.Max(r.w - inner.w, inner.y - r.y));

                string tmpBilgi = "";
                bool kirpik = false;
                TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.ForceMeshUpdate();
                    // preferredHeight satir yuksekligine dayanir; Turkce İ/Ğ gibi
                    // harfler cizimde onu asabildigi icin GERCEK cizim siniri olculur.
                    Vector2 cizim = tmp.textBounds.size;
                    float gy = Mathf.Max(tmp.preferredHeight, cizim.y);
                    float gx = Mathf.Max(tmp.preferredWidth, cizim.x);
                    float ky = rt.rect.height, kx = rt.rect.width;
                    kirpik = gy > ky + 0.5f || gx > kx + 0.5f;
                    tmpBilgi = $"| TMP {gx:F0}x{gy:F0} kutu {kx:F0}x{ky:F0}{(kirpik ? " KIRPIK" : "")}";
                }

                Debug.Log($"{Tag}_SATIR: {ekran} | {name,-20} x {r.x,7:F0}..{r.z,7:F0} " +
                          $"y {r.y,7:F0}..{r.w,7:F0} | tasma {tasma,6:F0} {tmpBilgi}");

                if (tasma > 1f) hatalar.Add($"{ekran} {name} kart disina {tasma:F0} birim");
                if (kirpik) hatalar.Add($"{ekran} {name} metin kirpiliyor");
            }
        }

        Debug.Log($"{Tag}_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    private Transform FindContent(string name)
    {
        Transform kart = Find("SafeAreaRoot/SettingsPanel/GlassBackground");
        if (kart == null) return null;

        Transform icerik = kart.Find("CardContent");
        if (icerik != null)
        {
            Transform t = icerik.Find(name);
            if (t != null) return t;
        }
        return kart.Find(name);
    }
}
