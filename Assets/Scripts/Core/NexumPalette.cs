using UnityEngine;

/// <summary>
/// Çalışma zamanı renk paleti — <c>NexumTheme.asset</c>'in koddan erişilebilen
/// karşılığı.
///
/// NEDEN AYRI BİR SINIF: tema asset'i <c>Assets/Data/Theme</c> altında ve
/// Resources'ta değil, yani çalışma zamanında yüklenemiyor. Kurucular
/// (Editor) temayı doğrudan okur; oyun kodu da aynı değerleri kullanmalı ki
/// ekranda tek bir palet olsun.
///
/// KURAL: kodda <c>new Color(...)</c> ile renk uydurulmaz, buradan alınır.
/// Değerler <c>NexumTheme.asset</c> ile BİREBİR aynıdır ve
/// <c>Assets/Tests/PlayMode/PaletteTests.cs</c> ikisinin ayrışmadığını her
/// koşuda doğrular — tema değişip burası unutulursa test düşer.
/// </summary>
public static class NexumPalette
{
    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f,
                         ((rgb >> 8) & 0xFF) / 255f,
                         (rgb & 0xFF) / 255f, 1f);
    }

    public static readonly Color Primary = Hex(0x00B4D8);
    public static readonly Color PrimaryBright = Hex(0x8FD8FF);
    public static readonly Color Secondary = Hex(0x90E0EF);
    public static readonly Color Danger = Hex(0xE63946);
    public static readonly Color Success = Hex(0x2ECC71);
    public static readonly Color Warning = Hex(0xFFD700);
    public static readonly Color TextPrimary = Hex(0xFFFFFF);
    public static readonly Color TextSecondary = Hex(0xADB5BD);
    public static readonly Color TextMuted = Hex(0x515961);
    public static readonly Color TextOnLight = Hex(0x323232);
    public static readonly Color PanelBackground = Hex(0x2E3240);
    public static readonly Color CardBackground = Hex(0x0B2A3D);

    /// <summary>Test için: rol adı → renk.</summary>
    public static Color ByRole(string role)
    {
        switch (role)
        {
            case "primary": return Primary;
            case "primaryBright": return PrimaryBright;
            case "secondary": return Secondary;
            case "danger": return Danger;
            case "success": return Success;
            case "warning": return Warning;
            case "textPrimary": return TextPrimary;
            case "textSecondary": return TextSecondary;
            case "textMuted": return TextMuted;
            case "textOnLight": return TextOnLight;
            case "panelBackground": return PanelBackground;
            case "cardBackground": return CardBackground;
            default: return Color.magenta;
        }
    }

    /// <summary>Test için: tüm rol adları.</summary>
    public static readonly string[] Roles =
    {
        "primary", "primaryBright", "secondary", "danger", "success", "warning",
        "textPrimary", "textSecondary", "textMuted", "textOnLight",
        "panelBackground", "cardBackground",
    };
}
