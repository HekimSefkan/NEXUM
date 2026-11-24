using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sonuç kartının PAYLAŞIM GÖRSELİ (1.6b).
///
/// <b>Yöntem.</b> Oyun canvas'ı Screen Space Overlay; overlay canvas'ı kamera
/// göremez, yani doğrudan RenderTexture'a çizilemez. <c>NexumDeviceShot</c>
/// edit mode'da bu sorunu canvas'ı BİR KARELİĞİNE Screen Space Camera'ya
/// alıp kendi ortografik kamerasıyla çizerek çözüyor; aynı yöntem burada
/// çalışma zamanında kullanılıyor. Çizim ve geri alma aynı çağrı içinde
/// bittiği için ekrana ara kare düşmez.
///
/// <b>Çözünürlük sabittir: 1080x1920.</b> Cihazın ekranı ne olursa olsun
/// paylaşılan görsel aynı — tasarım alanı da 1080x1920 (CanvasScaler
/// Expand), yani kart ölçeklenmeden oturur.
///
/// <b>KİŞİSEL VERİ YOK.</b> Görselde yalnızca oyun ekranı var: oyuncu adı ve
/// e-posta oyun sahnesinde hiçbir yerde yazmıyor. Bunu
/// <c>ResultShareTests</c> ölçüyor — kayda sentinel bir ad/e-posta yazıp
/// canvas altındaki TÜM metinleri tarıyor.
///
/// <b>AĞ YOK.</b> Görsel yalnızca cihazda üretilir ve cihazın kendi paylaşım
/// menüsüne verilir; hiçbir yere yüklenmez.
/// </summary>
public static class ResultShare
{
    private const string Tag = "NEXUM_PAYLASIM";

    public const int Width = 1080;
    public const int Height = 1920;

    /// <summary>Üretilen son görselin dosya yolu (test ve hata ayıklama).</summary>
    public static string LastPath { get; private set; }

    /// <summary>Paylaşım dosyasının adı. Üzerine yazılır, birikmez.</summary>
    public const string FileName = "nexum-sonuc.png";

    /// <summary>
    /// Kartı 1080x1920 PNG'ye çizer. Çağıran <c>Object.Destroy</c> etmeli.
    /// Canvas bulunamazsa null döner.
    /// </summary>
    public static Texture2D Render()
    {
        Canvas canvas = Object.FindObjectsOfType<Canvas>(true)
            .FirstOrDefault(c => c != null && c.isRootCanvas && c.name == "Canvas");
        if (canvas == null)
        {
            Debug.LogError($"{Tag}: kök Canvas bulunamadı, görsel üretilemedi");
            return null;
        }

        RenderMode eskiMod = canvas.renderMode;
        Camera eskiKamera = canvas.worldCamera;
        float eskiMesafe = canvas.planeDistance;

        GameObject kameraGo = new GameObject("NexumShareCamera");
        Texture2D png = null;
        RenderTexture rt = null;

        try
        {
            Camera kamera = kameraGo.AddComponent<Camera>();
            kamera.orthographic = true;
            kamera.clearFlags = CameraClearFlags.SolidColor;
            kamera.backgroundColor = new Color(0.02f, 0.05f, 0.08f, 1f);
            kamera.nearClipPlane = 0.1f;
            kamera.farClipPlane = 1000f;
            kamera.transform.position = new Vector3(0f, 0f, -500f);

            rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1;
            kamera.targetTexture = rt;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = kamera;
            canvas.planeDistance = 100f;

            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text t in canvas.GetComponentsInChildren<TMP_Text>(true)) t.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            kamera.Render();

            RenderTexture onceki = RenderTexture.active;
            RenderTexture.active = rt;
            png = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            png.Apply();
            RenderTexture.active = onceki;
        }
        finally
        {
            canvas.renderMode = eskiMod;
            canvas.worldCamera = eskiKamera;
            canvas.planeDistance = eskiMesafe;

            // SIRA ÖNEMLİ: kameraya bağlıyken serbest bırakmak Unity'de
            // hata logluyor ("Releasing render texture that is set as
            // Camera.targetTexture"). Önce kamera gider, sonra doku.
            Object.DestroyImmediate(kameraGo);

            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        return png;
    }

    /// <summary>
    /// Görseli cihazın geçici klasörüne yazar ve yolu döndürür.
    /// Kalıcı depolama KULLANILMAZ: paylaşılan dosya oyuncunun galerisini
    /// kirletmez, işletim sistemi önbelleği temizleyebilir.
    /// </summary>
    public static string Save(Texture2D png)
    {
        if (png == null) return null;

        string yol = Path.Combine(Application.temporaryCachePath, FileName);
        try
        {
            File.WriteAllBytes(yol, png.EncodeToPNG());
            LastPath = yol;
            return yol;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{Tag}: görsel yazılamadı ({yol}): {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Görseli üretir, diske yazar ve cihazın paylaşım menüsünü açar.
    /// Dönen değer dosya yolu; üretilemezse null.
    /// </summary>
    public static string Share(ResultCard card)
    {
        Texture2D png = Render();
        if (png == null) return null;

        string yol = Save(png);
        Object.Destroy(png);

        if (string.IsNullOrEmpty(yol)) return null;

        Paylas(yol);
        return yol;
    }

    /// <summary>
    /// Cihazın paylaşım menüsü. Editörde menü yok; yalnızca yol loglanır.
    /// </summary>
    private static void Paylas(string yol)
    {
#if UNITY_EDITOR
        Debug.Log($"{Tag}: editörde paylaşım menüsü yok; görsel üretildi -> {yol}");
#else
        new NativeShare()
            .AddFile(yol)
            .SetSubject(Loc.Get(CodeStrings.ShareSubject))
            .SetText(Loc.Get(CodeStrings.ShareText))
            .Share();
#endif
    }
}
