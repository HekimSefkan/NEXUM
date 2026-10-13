using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tema asset'ini üretir / günceller. Değerler <see cref="NexumTheme"/> alan
/// varsayılanlarından gelir (sahnelerden sayılarak çıkarılmış gerçek değerler);
/// bu script yalnızca asset'i oluşturur ve font referanslarını bağlar.
///
/// IDEMPOTENT: asset varsa yeniden oluşturmaz, yalnızca eksik font bağlarını
/// tamamlar.
///
/// Menüden: NEXUM → Tema Asset'ini Kur
/// Batch mode: -executeMethod NexumThemeBuilder.Run
/// </summary>
public static class NexumThemeBuilder
{
    private const string Tag = "NEXUM_THEME";

    // Hangi rol hangi font ailesini kullanıyor (sahnedeki kullanımdan çıkarıldı)
    private const string FontDisplay = "Orbitron-Bold SDF";
    private const string FontTitle = "Orbitron-SemiBold SDF";
    private const string FontSubtitle = "Oxanium-SemiBold SDF";
    private const string FontBody = "Oxanium-Regular SDF";
    private const string FontLabel = "Oxanium-Medium SDF";
    private const string FontValue = "Orbitron-Bold SDF";

    [MenuItem("NEXUM/Tema Asset'ini Kur")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(NexumTheme.AssetPath));

        NexumTheme theme = AssetDatabase.LoadAssetAtPath<NexumTheme>(NexumTheme.AssetPath);
        bool created = false;

        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<NexumTheme>();
            AssetDatabase.CreateAsset(theme, NexumTheme.AssetPath);
            created = true;
        }

        int bound = 0;
        bound += Bind(theme.typography.display, FontDisplay);
        bound += Bind(theme.typography.title, FontTitle);
        bound += Bind(theme.typography.subtitle, FontSubtitle);
        bound += Bind(theme.typography.body, FontBody);
        bound += Bind(theme.typography.label, FontLabel);
        bound += Bind(theme.typography.value, FontValue);

        if (created || bound > 0)
        {
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"{Tag}: asset {(created ? "oluşturuldu" : "zaten vardı")}, font bağı +{bound}");
        Debug.Log($"{Tag}: renk {theme.AllColors.Length}, punto {theme.typography.All.Length}, " +
                  $"boşluk {theme.spacing.All.Length}");
        Debug.Log($"{Tag}: OK");
    }

    private static int Bind(NexumTheme.TypeStyle style, string fontName)
    {
        if (style.font != null) return 0;

        string guid = AssetDatabase.FindAssets($"t:TMP_FontAsset {fontName}").FirstOrDefault();
        if (guid == null)
        {
            Debug.LogWarning($"{Tag}: font bulunamadı -> {fontName}");
            return 0;
        }

        style.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        return style.font != null ? 1 : 0;
    }
}
