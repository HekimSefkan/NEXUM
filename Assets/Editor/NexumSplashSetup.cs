using System.Linq;
using UnityEditor;
using UnityEngine;

// Açılış ekranına NEXUM logosunu ekler. Unity Personal lisansında Unity logosu
// kaldırılamaz; bu script yalnızca yanına kendi logomuzu ekler, süre ve stil
// varsayılan kalır.
//
// Menüden:  NEXUM / Açılış Logosunu Uygula
// Batch:    Unity.exe -batchmode -nographics -quit -projectPath <proje>
//                     -executeMethod NexumSplashSetup.Apply -logFile <log>
public static class NexumSplashSetup
{
    private const string Tag = "NEXUM_SPLASH";
    private const string LogoPath = "Assets/Art/Icons/icon_master_1024.png";

    [MenuItem("NEXUM/Açılış Logosunu Uygula")]
    public static void Apply()
    {
        Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        if (logo == null)
        {
            // İkon dokuları varsayılan olarak Sprite değil; Sprite'a çevrilmemişse atla
            Debug.Log($"{Tag}: {LogoPath} Sprite olarak yüklenemedi (doku tipi Sprite değil), adım atlandı");
            return;
        }

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = PlayerSettings.SplashScreen.showUnityLogo;   // lisans neyse o kalsın

        PlayerSettings.SplashScreenLogo[] existing = PlayerSettings.SplashScreen.logos ?? new PlayerSettings.SplashScreenLogo[0];
        bool alreadyThere = existing.Any(l => l.logo == logo);
        if (alreadyThere)
        {
            Debug.Log($"{Tag}: logo zaten ekli, değişiklik yok");
            Debug.Log(Tag + ": OK");
            return;
        }

        PlayerSettings.SplashScreenLogo nexum = PlayerSettings.SplashScreenLogo.Create(2f, logo);
        PlayerSettings.SplashScreen.logos = existing.Concat(new[] { nexum }).ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: logo eklendi (toplam {PlayerSettings.SplashScreen.logos.Length}), Unity logosu={PlayerSettings.SplashScreen.showUnityLogo}");
        Debug.Log(Tag + ": OK");
    }
}
