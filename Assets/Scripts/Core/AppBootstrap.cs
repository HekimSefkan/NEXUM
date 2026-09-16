using UnityEngine;

// Uygulama açılırken, ilk sahne yüklenmeden önce bir kez çalışır. Sahneye eklenmesi gerekmez.
public static class AppBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Mobilde varsayılan 30 FPS yerine 60 FPS hedefle; vSync kapalıyken targetFrameRate geçerli olur
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
