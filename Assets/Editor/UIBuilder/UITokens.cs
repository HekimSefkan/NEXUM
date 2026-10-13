using UnityEngine;

/// <summary>
/// Kurucuların kullandığı DEĞERLER. Renk, punto, boşluk ve oran gibi her sayı
/// buradan gelir; kurucuların içine sabit yazılmaz.
///
/// Şimdilik düz sabitler. Bir sonraki adımda tema tokenlarına geçilecek:
/// o zaman bu sınıfın gövdesi bir tema asset'inden okumaya döner, kurucular
/// hiç değişmez. Bu yüzden isimler renk KODU değil renk ROLÜ anlatıyor
/// (Accent, OnCard…), ölçüler de piksel değil rol adı taşıyor.
/// </summary>
public static class UITokens
{
    // --- Renk rolleri -----------------------------------------------------
    public static readonly Color Accent = new Color32(0x8F, 0xD8, 0xFF, 0xFF);   // camgöbeği vurgu
    public static readonly Color Success = new Color32(0x2E, 0xCC, 0x71, 0xFF);
    public static readonly Color Warning = new Color32(0xF1, 0xC4, 0x0F, 0xFF);
    public static readonly Color OnCard = Color.white;

    // --- Punto rolleri ----------------------------------------------------
    public const float FontSectionTitle = 50f;
    public const float FontRowLabel = 36f;
    public const float FontButton = 28f;
    public const float FontMeterLabel = 28f;

    // --- Cam kart (SettingsPanel_.png) ------------------------------------
    // Sprite 796x1280; çerçevenin iç kenarları piksel ölçümünden orana çevrildi.
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
