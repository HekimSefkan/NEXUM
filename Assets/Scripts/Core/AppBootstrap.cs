using UnityEngine;
using UnityEngine.SceneManagement;

// Uygulama açılırken, ilk sahne yüklenmeden önce bir kez çalışır. Sahneye eklenmesi gerekmez.
public static class AppBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Mobilde varsayılan 30 FPS yerine 60 FPS hedefle; vSync kapalıyken targetFrameRate geçerli olur
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Pause açıkken sahne değiştirilirse oyun donmasın: her sahne yüklemesinde zaman ölçeğini sıfırla
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Uygulama arka plana alınınca veya kapanınca PlayerPrefs'i diske yazan gizli, kalıcı obje
        var lifecycle = new GameObject("AppLifecycle");
        lifecycle.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(lifecycle);
        lifecycle.AddComponent<AppLifecycle>();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
    }

    private sealed class AppLifecycle : MonoBehaviour
    {
        private void OnApplicationPause(bool paused)
        {
            if (paused) PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            // HideAndDontSave objeleri Editor'de Play modundan çıkınca kendiliğinden silinmez
            Destroy(gameObject);
#endif
        }
    }
}
