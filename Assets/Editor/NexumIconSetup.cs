using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Android ikon slotlarını (Adaptive / Round / Legacy) Assets/Art/Icons altındaki
// görsellerle doldurur. ProjectSettings.asset elle düzenlenmez, PlayerSettings API'si kullanılır.
//
// Menüden:  NEXUM / Android İkonlarını Uygula
// Batch:    Unity.exe -batchmode -nographics -quit -projectPath <proje>
//                     -executeMethod NexumIconSetup.Apply -logFile <log>
//
// Görseller Tools/generate_icons.py ile üretilir.
public static class NexumIconSetup
{
    private const string Tag = "NEXUM_ICON";

    private const string Foreground = "Assets/Art/Icons/icon_adaptive_foreground_432.png";
    private const string Background = "Assets/Art/Icons/icon_adaptive_background_432.png";
    private const string Round = "Assets/Art/Icons/icon_round_512.png";
    private const string Legacy = "Assets/Art/Icons/icon_legacy_512.png";

    [MenuItem("NEXUM/Android İkonlarını Uygula")]
    public static void Apply()
    {
        Texture2D foreground = Load(Foreground);
        Texture2D background = Load(Background);
        Texture2D round = Load(Round);
        Texture2D legacy = Load(Legacy);
        if (foreground == null || background == null || round == null || legacy == null)
        {
            Debug.Log(Tag + ": FAIL (ikon görselleri eksik)");
            EditorApplication.Exit(1);
            return;
        }

        foreach (PlatformIconKind supported in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        {
            Debug.Log($"{Tag}_KIND: \"{supported}\" (tip {supported.GetType().Name})");
        }

        // Adaptive: katman 0 arka plan, katman 1 ön plan
        SetKind("Adaptive", new[] { background, foreground });
        SetKind("Round", new[] { round });
        SetKind("Legacy", new[] { legacy });

        AssetDatabase.SaveAssets();
        Report();
        Debug.Log(Tag + ": OK");
    }

    private static Texture2D Load(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) Debug.Log($"{Tag}: bulunamadı {path}");
        return texture;
    }

    // AndroidPlatformIconKind, UnityEditor.Android.Extensions.dll içinde olduğu ve
    // Assembly-CSharp-Editor onu referans almadığı için tür adıyla aranır.
    private static PlatformIconKind FindKind(string kindName)
    {
        // Unity 2022.3'te adlar API sürümünü de taşır: "Adaptive (API 26)", "Round (API 25)", "Legacy"
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        {
            if (kind.ToString().StartsWith(kindName, System.StringComparison.OrdinalIgnoreCase)) return kind;
        }
        return null;
    }

    private static void SetKind(string kindName, Texture2D[] layers)
    {
        PlatformIconKind kind = FindKind(kindName);
        if (kind == null)
        {
            Debug.Log($"{Tag}: {kindName} türü bu Unity sürümünde yok, atlandı");
            return;
        }

        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        foreach (PlatformIcon icon in icons)
        {
            // Bazı türlerde tek katman beklenir; fazlasını göndermemek için kırpılır
            Texture2D[] used = layers.Take(Mathf.Max(1, icon.maxLayerCount)).ToArray();
            icon.SetTextures(used);
        }

        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        Debug.Log($"{Tag}: {kindName} -> {icons.Length} slot, katman {layers.Length}");
    }

    // Uygulandıktan sonra hangi slotun hangi görsele bağlandığını yazar
    private static void Report()
    {
        foreach (string kindName in new[] { "Adaptive", "Round", "Legacy" })
        {
            PlatformIconKind kind = FindKind(kindName);
            if (kind == null) continue;
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            int filled = 0;
            var sizes = new List<string>();
            foreach (PlatformIcon icon in icons)
            {
                Texture2D[] textures = icon.GetTextures();
                bool any = textures != null && textures.Any(t => t != null);
                if (any) filled++;
                sizes.Add($"{icon.width}x{icon.height}{(any ? "+" : "-")}");
            }
            Debug.Log($"{Tag}_SLOT: {kindName}: {filled}/{icons.Length} dolu [{string.Join(" ", sizes)}]");
        }
    }
}
