using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Game view'ı mağaza için tam çözünürlükte (1080x1920) PNG'ye kaydeder.
// Menü: NEXUM / Ekran Görüntüsü Al (1080x1920)   —  kısayol: Ctrl+Shift+S
//
// Çıktı: Tools/store/screenshots/ekran_yyyyMMdd_HHmmss.png (git'te yok sayılır)
//
// Not: ScreenCapture.CaptureScreenshot yalnızca Play modunda anlamlı çalışır ve
// görüntüyü kare sonunda yazar; bu yüzden dosya birkaç kare sonra oluşur.
public static class NexumScreenshot
{
    private const string Tag = "NEXUM_SCREENSHOT";
    private const string OutputDir = "Tools/store/screenshots";
    private const int TargetWidth = 1080;
    private const int TargetHeight = 1920;

    [MenuItem("NEXUM/Ekran Görüntüsü Al (1080x1920) %#s")]
    public static void Capture()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("NEXUM",
                "Ekran görüntüsü için önce Play moduna geç, istediğin ekranı aç, sonra bu komutu kullan.",
                "Tamam");
            return;
        }

        if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);

        string path = Path.Combine(OutputDir,
            "ekran_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png").Replace('\\', '/');

        // Game view farklı bir çözünürlükteyse superSize ile büyütmek bulanıklaştırır;
        // bunun yerine Game view'ın 1080x1920'ye ayarlanması istenir.
        int width = Screen.width;
        int height = Screen.height;
        if (width != TargetWidth || height != TargetHeight)
        {
            Debug.LogWarning($"{Tag}: Game view {width}x{height}; mağaza görüntüleri için " +
                             $"Game sekmesindeki çözünürlük listesine {TargetWidth}x{TargetHeight} ekleyip seçmen önerilir. " +
                             "Görüntü yine de mevcut çözünürlükte kaydedilecek.");
        }

        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"{Tag}: {path} ({width}x{height}) - dosya birkaç kare içinde yazılır");
    }
}
