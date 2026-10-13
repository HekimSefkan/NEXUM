using UnityEditor;
using UnityEngine;

/// <summary>
/// Kurucuların kullandığı DEĞERLER. Renk, punto, boşluk ve süre
/// <see cref="NexumTheme"/> asset'inden okunur; kurucuların içine sabit sayı
/// yazılmaz. Bir değeri değiştirmek için asset düzenlenir, kod değişmez.
///
/// Yalnızca GEOMETRİ sabitleri burada duruyor: bunlar görsel dil değil, belirli
/// bir sprite'ın (cam kart) ölçüleri — temaya ait değiller.
/// </summary>
public static class UITokens
{
    private static NexumTheme cached;

    /// <summary>Tema asset'i. Yoksa alan varsayılanlarıyla geçici bir örnek döner.</summary>
    public static NexumTheme Theme
    {
        get
        {
            if (cached != null) return cached;

            cached = AssetDatabase.LoadAssetAtPath<NexumTheme>(NexumTheme.AssetPath);
            if (cached == null)
            {
                Debug.LogWarning($"UITokens: tema asset'i yok ({NexumTheme.AssetPath}); " +
                                 "varsayılanlar kullanılıyor (NEXUM → Tema Asset'ini Kur)");
                cached = ScriptableObject.CreateInstance<NexumTheme>();
            }
            return cached;
        }
    }

    public static void Reload()
    {
        cached = null;
    }

    // --- Renk rolleri (temadan) -------------------------------------------
    public static Color Primary { get { return Theme.palette.primary; } }
    public static Color Accent { get { return Theme.palette.primaryBright; } }
    public static Color Secondary { get { return Theme.palette.secondary; } }
    public static Color Danger { get { return Theme.palette.danger; } }
    public static Color Success { get { return Theme.palette.success; } }
    public static Color Warning { get { return Theme.palette.warning; } }
    public static Color OnCard { get { return Theme.palette.textPrimary; } }
    public static Color TextSecondary { get { return Theme.palette.textSecondary; } }

    // --- Punto rolleri (temadan) ------------------------------------------
    public static float FontSectionTitle { get { return Theme.typography.title.size; } }
    public static float FontRowLabel { get { return Theme.typography.body.size; } }
    public static float FontButton { get { return Theme.typography.label.size; } }
    public static float FontMeterLabel { get { return Theme.typography.label.size; } }

    // --- Boşluk (temadan) -------------------------------------------------
    public static float SpaceXs { get { return Theme.spacing.xs; } }
    public static float SpaceSm { get { return Theme.spacing.sm; } }
    public static float SpaceMd { get { return Theme.spacing.md; } }
    public static float SpaceLg { get { return Theme.spacing.lg; } }
    public static float SpaceXl { get { return Theme.spacing.xl; } }

    // --- Süre (temadan) ---------------------------------------------------
    public static float DurationShort { get { return Theme.motion.durationShort; } }
    public static float DurationMedium { get { return Theme.motion.durationMedium; } }
    public static float DurationLong { get { return Theme.motion.durationLong; } }

    // ======================================================================
    // GEOMETRİ — temaya ait değil: cam kart sprite'ının (SettingsPanel_.png)
    // kendi ölçüleri. Sprite değişirse burası değişir, tema değişmez.
    // ======================================================================
    public const float CardSpriteWidth = 796f;
    public const float CardSpriteHeight = 1280f;
    public const float CardAspect = CardSpriteWidth / CardSpriteHeight;
    public const float CardInsetLeft = 100f / CardSpriteWidth;    // 0.1256
    public const float CardInsetRight = 690f / CardSpriteWidth;   // 0.8668

    // Kartın parlak ayraçları: üç bölümün sınırları (oransal, alttan)
    public const float CardDividerUpper = 0.575f;
    public const float CardDividerLower = 0.3172f;

    // --- Dikey ritim (oransal, kart yüksekliğine göre) ---------------------
    public const float RhythmTitleHeight = 0.043f;
    public const float RhythmRowHeight = 0.058f;
    public const float RhythmGapAfterTitle = 0.015f;
    public const float RhythmGapBetweenRows = 0.012f;

    // --- Ortak anchor'lar -------------------------------------------------
    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    public static readonly Vector2 Zero = Vector2.zero;
    public static readonly Vector2 One = Vector2.one;
}
