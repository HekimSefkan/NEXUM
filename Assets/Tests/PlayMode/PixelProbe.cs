using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GERÇEK PİKSELDEN ölçüm. Kontrast iddiaları artık token tablosundan değil,
/// Unity'nin çizdiği kareden hesaplanıyor.
///
/// NEDEN: bir metnin okunup okunmadığı rengiyle değil ARKASINDAKİ YÜZEYLE
/// olan farkıyla belirlenir. 2d-3a'da Metal kiti geçişi bir sürü yüzeyi
/// açıktan koyuya çevirdi; metin renkleri olduğu yerde kaldı ve TARİF DEFTERİ
/// butonunun etiketi 1,12:1'e düştü. O yüzeyi hiçbir test okumuyordu.
///
/// Yöntem: Canvas geçici olarak ortografik bir kameraya bağlanır ve kamera
/// hedef boyutta bir RenderTexture'a çizdirilir. Önce bütün TMP bileşenleri
/// kapatılıp bir kare alınır — bu, metinlerin ARKASINDAKİ yüzeydir.
/// (Editor'deki <c>NexumContrastSweep</c> ile aynı tanım; biri düzeltir,
/// bu doğrular.)
/// </summary>
public static class PixelProbe
{
    public const float MinContrast = 4.5f;

    public static float Luminance(Color c)
    {
        return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
    }

    private static float Channel(float v)
    {
        return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    public static float Contrast(Color a, Color b)
    {
        float la = Luminance(a), lb = Luminance(b);
        if (la < lb) { float t = la; la = lb; lb = t; }
        return (la + 0.05f) / (lb + 0.05f);
    }

    public static string Hex(Color c)
    {
        Color32 x = c;
        return $"#{x.r:X2}{x.g:X2}{x.b:X2}";
    }

    /// <summary>
    /// Yakalama oturumu. Canvas kameraya BAĞLI kalır; ölçüm bittikten sonra
    /// <see cref="End"/> ile geri alınır.
    ///
    /// NEDEN İKİ AŞAMA: PlayMode'da canvas yeniden kurulumu KARE SONUNDA
    /// oluyor. Tek aşamalı sürüm <c>ForceUpdateCanvases</c> + <c>Render</c>
    /// diyordu ve BOŞ kare alıyordu (her örnek #CDCDCD çıkıyordu). Ayrıca
    /// dikdörtgen -> piksel eşlemesi canvas'ın O ANKİ rect'ine bağlı; eski
    /// sürüm render'dan SONRA ölçüyordu ve canvas çoktan 640x480'e dönmüş
    /// oluyordu.
    /// </summary>
    public class Session
    {
        public Texture2D frame;
        public GameObject canvasGo;
        public Vector2Int size;
        public Vector2 canvasSize;      // render anındaki canvas rect'i

        internal Canvas canvas;
        internal RenderMode oldMode;
        internal Camera oldCam;
        internal float oldPlane;
        internal GameObject camGo;
        internal Camera cam;
        internal RenderTexture rt;
        internal List<TMP_Text> texts;
        internal List<bool> wasEnabled;
    }

    /// <summary>Canvas'ı kameraya bağlar. Çağıran bir kare beklemeli.</summary>
    public static Session Begin(GameObject canvasGo, Vector2Int size, bool hideTexts)
    {
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null) return null;

        Session s = new Session { canvasGo = canvasGo, size = size, canvas = canvas };

        s.texts = canvasGo.GetComponentsInChildren<TMP_Text>(true).ToList();
        s.wasEnabled = s.texts.Select(t => t.enabled).ToList();
        if (hideTexts)
        {
            for (int i = 0; i < s.texts.Count; i++) s.texts[i].enabled = false;
        }
        else
        {
            s.texts = null;
        }

        s.oldMode = canvas.renderMode;
        s.oldCam = canvas.worldCamera;
        s.oldPlane = canvas.planeDistance;

        s.camGo = new GameObject("PixelProbeCamera");
        s.cam = s.camGo.AddComponent<Camera>();
        s.cam.orthographic = true;
        s.cam.clearFlags = CameraClearFlags.SolidColor;
        s.cam.backgroundColor = new Color(0.02f, 0.05f, 0.08f, 1f);
        s.cam.nearClipPlane = 0.1f;
        s.cam.farClipPlane = 1000f;
        s.cam.transform.position = new Vector3(0f, 0f, -500f);

        s.rt = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
        s.rt.antiAliasing = 1;
        s.cam.targetTexture = s.rt;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = s.cam;
        canvas.planeDistance = 100f;
        Canvas.ForceUpdateCanvases();
        return s;
    }

    /// <summary>Kareyi çizer ve okur. <see cref="Begin"/>'den sonra bir kare beklenmeli.</summary>
    public static void Render(Session s)
    {
        if (s == null) return;

        Canvas.ForceUpdateCanvases();
        s.cam.Render();

        RectTransform cr = s.canvasGo.GetComponent<RectTransform>();
        s.canvasSize = new Vector2(cr.rect.width, cr.rect.height);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = s.rt;
        s.frame = new Texture2D(s.size.x, s.size.y, TextureFormat.RGB24, false);
        s.frame.ReadPixels(new Rect(0, 0, s.size.x, s.size.y), 0, 0);
        s.frame.Apply();
        RenderTexture.active = prev;
    }

    /// <summary>
    /// Kare tek renk mi? Süit <c>-nographics</c> ile koşarsa kamera hiçbir
    /// şey çizemez ve her örnek aynı griye düşer; ölçüm sessizce anlamsız
    /// olur. Bunu test SESSİZCE GEÇMEMELİ.
    /// </summary>
    public static bool IsBlank(Session s)
    {
        if (s == null || s.frame == null) return true;
        Texture2D f = s.frame;
        Color a = f.GetPixel(4, 4);
        Color b = f.GetPixel(f.width - 5, 4);
        Color c = f.GetPixel(4, f.height - 5);
        Color d = f.GetPixel(f.width / 2, f.height / 2);
        return Contrast(a, b) < 1.01f && Contrast(a, c) < 1.01f && Contrast(a, d) < 1.01f;
    }

    /// <summary>Canvas'ı eski haline döndürür ve kareyi serbest bırakır.</summary>
    public static void End(Session s)
    {
        if (s == null) return;

        s.cam.targetTexture = null;
        Object.DestroyImmediate(s.rt);
        Object.DestroyImmediate(s.camGo);

        s.canvas.renderMode = s.oldMode;
        s.canvas.worldCamera = s.oldCam;
        s.canvas.planeDistance = s.oldPlane;

        if (s.texts != null)
        {
            for (int i = 0; i < s.texts.Count; i++) s.texts[i].enabled = s.wasEnabled[i];
        }
        Canvas.ForceUpdateCanvases();

        if (s.frame != null) Object.DestroyImmediate(s.frame);
        s.frame = null;
    }

    /// <summary>
    /// Bir RectTransform'un karedeki MEDYAN rengi. Ortalama almak iki farklı
    /// yüzeyin sınırında var olmayan bir ara renk uydururdu.
    /// </summary>
    public static Color? Sample(Session s, RectTransform rect, float inset = 0f)
    {
        if (s == null || s.frame == null) return null;
        Texture2D frame = s.frame;
        Vector2Int size = s.size;
        RectTransform canvasRect = s.canvasGo.GetComponent<RectTransform>();
        float cw = s.canvasSize.x, ch = s.canvasSize.y;
        if (cw <= 0f || ch <= 0f) return null;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Vector3 k in corners)
        {
            Vector3 local = canvasRect.InverseTransformPoint(k);
            x0 = Mathf.Min(x0, local.x); x1 = Mathf.Max(x1, local.x);
            y0 = Mathf.Min(y0, local.y); y1 = Mathf.Max(y1, local.y);
        }

        float dx = (x1 - x0) * inset, dy = (y1 - y0) * inset;
        x0 += dx; x1 -= dx; y0 += dy; y1 -= dy;

        int px0 = Mathf.RoundToInt((x0 / cw + 0.5f) * size.x);
        int px1 = Mathf.RoundToInt((x1 / cw + 0.5f) * size.x);
        int py0 = Mathf.RoundToInt((y0 / ch + 0.5f) * size.y);
        int py1 = Mathf.RoundToInt((y1 / ch + 0.5f) * size.y);

        // Kare dışına taşan nesne ÖLÇÜLMEZ. Kırpmak onu kenar pikseline
        // eşitler ve boş kare rengini yüzey sanmaya yol açar.
        if (px0 < 0 || py0 < 0 || px1 >= size.x || py1 >= size.y) return null;
        if (px1 - px0 < 2 || py1 - py0 < 2) return null;

        List<Color> colors = new List<Color>();
        int stepX = Mathf.Max(1, (px1 - px0) / 24);
        int stepY = Mathf.Max(1, (py1 - py0) / 24);
        for (int y = py0; y <= py1; y += stepY)
        {
            for (int x = px0; x <= px1; x += stepX) colors.Add(frame.GetPixel(x, y));
        }
        if (colors.Count == 0) return null;

        return colors.OrderBy(Luminance).ElementAt(colors.Count / 2);
    }

    /// <summary>RectTransform'un karedeki piksel sınırları.</summary>
    public static RectInt? Bounds(Session s, RectTransform rect)
    {
        if (s == null) return null;
        Vector2Int size = s.size;
        RectTransform canvasRect = s.canvasGo.GetComponent<RectTransform>();
        float cw = s.canvasSize.x, ch = s.canvasSize.y;
        if (cw <= 0f || ch <= 0f) return null;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Vector3 k in corners)
        {
            Vector3 local = canvasRect.InverseTransformPoint(k);
            x0 = Mathf.Min(x0, local.x); x1 = Mathf.Max(x1, local.x);
            y0 = Mathf.Min(y0, local.y); y1 = Mathf.Max(y1, local.y);
        }

        int px0 = Mathf.Clamp(Mathf.RoundToInt((x0 / cw + 0.5f) * size.x), 0, size.x - 1);
        int px1 = Mathf.Clamp(Mathf.RoundToInt((x1 / cw + 0.5f) * size.x), 0, size.x - 1);
        int py0 = Mathf.Clamp(Mathf.RoundToInt((y0 / ch + 0.5f) * size.y), 0, size.y - 1);
        int py1 = Mathf.Clamp(Mathf.RoundToInt((y1 / ch + 0.5f) * size.y), 0, size.y - 1);
        if (px1 - px0 < 4 || py1 - py0 < 4) return null;

        return new RectInt(px0, py0, px1 - px0, py1 - py0);
    }

    /// <summary>
    /// Dikdörtgenin dış <paramref name="band"/> oranındaki şeridinin ORTALAMA
    /// rengi (merkez hariç). Taş seviyesi bu şeritte taşınıyor.
    /// </summary>
    public static Color BandMean(Texture2D frame, RectInt r, float band)
    {
        int ix = Mathf.RoundToInt(r.width * band);
        int iy = Mathf.RoundToInt(r.height * band);
        double sr = 0, sg = 0, sb = 0;
        int n = 0;
        for (int y = r.yMin; y < r.yMax; y++)
        {
            for (int x = r.xMin; x < r.xMax; x++)
            {
                if (x >= r.xMin + ix && x < r.xMax - ix &&
                    y >= r.yMin + iy && y < r.yMax - iy) continue;
                Color c = frame.GetPixel(x, y);
                sr += c.r; sg += c.g; sb += c.b; n++;
            }
        }
        return n == 0 ? Color.black : new Color((float)(sr / n), (float)(sg / n), (float)(sb / n));
    }

    /// <summary>Dikdörtgenin merkezindeki en sık görülen renk (metin pikseli elenir).</summary>
    public static Color CenterMode(Texture2D frame, RectInt r, float inset = 0.35f)
    {
        int ix = Mathf.RoundToInt(r.width * inset);
        int iy = Mathf.RoundToInt(r.height * inset);
        Dictionary<Color32, int> say = new Dictionary<Color32, int>(new Rgb());
        for (int y = r.yMin + iy; y < r.yMax - iy; y++)
        {
            for (int x = r.xMin + ix; x < r.xMax - ix; x++)
            {
                Color32 c = frame.GetPixel(x, y);
                say[c] = say.TryGetValue(c, out int n) ? n + 1 : 1;
            }
        }
        if (say.Count == 0) return Color.black;
        Color32 enSik = say.OrderByDescending(kv => kv.Value).First().Key;
        return enSik;
    }

    private class Rgb : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b) { return a.r == b.r && a.g == b.g && a.b == b.b; }
        public int GetHashCode(Color32 c) { return (c.r << 16) | (c.g << 8) | c.b; }
    }

    /// <summary>Panel-benzeri objeler: adı "Panel" ile biten, kök altındaki objeler.</summary>
    public static List<Transform> Panels(Transform canvas)
    {
        List<Transform> list = new List<Transform>();
        foreach (string rootName in new[] { "", "SafeAreaRoot" })
        {
            Transform root = string.IsNullOrEmpty(rootName) ? canvas : canvas.Find(rootName);
            if (root == null) continue;
            foreach (Transform t in root)
            {
                if (t.name.EndsWith("Panel")) list.Add(t);
            }
        }
        return list;
    }

    public static string Path(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s.Replace("Canvas/SafeAreaRoot/", "").Replace("Canvas/", "");
    }
}
