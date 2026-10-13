using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Görsel dilin tek kaynağı. Değerler UYDURULMADI: mevcut sahnelerden sayılarak
/// çıkarıldı (bkz. docs/DESIGN_TOKENS.md envanter tablosu).
///
/// Kurucular (<c>UITokens</c>) bu asset'ten besleniyor. Bir değeri değiştirmek
/// için asset düzenlenir, kod değişmez.
///
/// Asset: Assets/Data/Theme/NexumTheme.asset (NexumThemeBuilder üretir/günceller).
/// </summary>
[CreateAssetMenu(fileName = "NexumTheme", menuName = "NEXUM/Tema")]
public class NexumTheme : ScriptableObject
{
    public const string AssetPath = "Assets/Data/Theme/NexumTheme.asset";

    // ===================================================== RENK PALETİ

    [Serializable]
    public class Palette
    {
        [Tooltip("Marka camgöbeği. Sahnede en çok kullanılan vurgu rengi (8 bileşen).")]
        public Color primary = Hex("#00B4D8");

        [Tooltip("Açık camgöbeği: HUD vurgusu ve yeni UI (profil başlığı, katalizör etiketi).")]
        public Color primaryBright = Hex("#8FD8FF");

        [Tooltip("İkincil camgöbeği: ikinci düzey vurgular.")]
        public Color secondary = Hex("#90E0EF");

        [Tooltip("Tehlike / hata.")]
        public Color danger = Hex("#E63946");

        [Tooltip("Başarı / olumlu geri bildirim.")]
        public Color success = Hex("#2ECC71");

        [Tooltip("Uyarı / bedel / sayaç.")]
        public Color warning = Hex("#FFD700");

        [Tooltip("Koyu zeminde birincil metin.")]
        public Color textPrimary = Hex("#FFFFFF");

        [Tooltip("Koyu zeminde ikincil metin (açıklama, ipucu).")]
        public Color textSecondary = Hex("#ADB5BD");

        [Tooltip("Açık zeminde metin (beyaz kartların üzeri).")]
        public Color textOnLight = Hex("#323232");

        [Tooltip("Panel arka planı.")]
        public Color panelBackground = Hex("#2E3240");

        [Tooltip("Kart arka planı.")]
        public Color cardBackground = Hex("#0B2A3D");
    }

    // ===================================================== TİPOGRAFİ

    [Serializable]
    public class TypeStyle
    {
        public string role;
        public TMP_FontAsset font;
        public float size;

        public TypeStyle(string role, float size)
        {
            this.role = role;
            this.size = size;
        }
    }

    [Serializable]
    public class Typography
    {
        [Tooltip("Taş ve bileşik etiketi (en sık kullanılan punto: 28 bileşen).")]
        public TypeStyle display = new TypeStyle("display", 80f);

        [Tooltip("Bölüm başlığı (16 bileşen).")]
        public TypeStyle title = new TypeStyle("title", 50f);

        [Tooltip("Alt başlık / giriş alanı (19 bileşen).")]
        public TypeStyle subtitle = new TypeStyle("subtitle", 40f);

        [Tooltip("Gövde metni (22 bileşen — en yaygın metin boyutu).")]
        public TypeStyle body = new TypeStyle("body", 36f);

        [Tooltip("Etiket / küçük metin (8 bileşen).")]
        public TypeStyle label = new TypeStyle("label", 30f);

        [Tooltip("Sayısal değer (10 bileşen).")]
        public TypeStyle value = new TypeStyle("value", 60f);

        public TypeStyle[] All
        {
            get { return new[] { display, title, subtitle, body, label, value }; }
        }
    }

    // ===================================================== BOŞLUK VE ÇERÇEVE

    [Serializable]
    public class Spacing
    {
        public float xs = 8f;
        public float sm = 16f;
        public float md = 24f;
        public float lg = 40f;
        public float xl = 64f;

        public float[] All { get { return new[] { xs, sm, md, lg, xl }; } }
    }

    [Serializable]
    public class Shape
    {
        [Tooltip("Köşe yarıçapları. Not: köşeler şu an sprite'ların içinde çizili; " +
                 "bu değerler yeni çizilecek yüzeyler için referanstır.")]
        public float radiusSmall = 12f;
        public float radiusMedium = 24f;
        public float radiusLarge = 40f;

        public float borderThin = 2f;
        public float borderThick = 4f;
    }

    // ===================================================== HAREKET

    [Serializable]
    public class Motion
    {
        [Tooltip("Kısa: dokunma geri bildirimi, punch (DOPunchScale 0.15).")]
        public float durationShort = 0.15f;

        [Tooltip("Orta: panel açılışı, ölçek tween'i (DOScale 0.3).")]
        public float durationMedium = 0.3f;

        [Tooltip("Uzun: kazanma ekranı, vurgu döngüsü (0.5).")]
        public float durationLong = 0.5f;

        [Tooltip("Sönme: beliriş parlaması, matris yeniden düzenleme, denklem (0.35).")]
        public float durationFade = 0.35f;

        [Tooltip("Belirme / açılma: taşmalı giriş.")]
        public Ease easeAppear = Ease.OutBack;

        [Tooltip("Hareket / kayma.")]
        public Ease easeMove = Ease.OutQuad;

        [Tooltip("Kaybolma / kapanma.")]
        public Ease easeDisappear = Ease.InBack;

        [Tooltip("Doluluk göstergesi.")]
        public Ease easeFill = Ease.OutBounce;
    }

    /// <summary>DOTween'e bağımlılık yaratmamak için eğriler ad olarak tutulur.</summary>
    public enum Ease
    {
        Linear,
        OutQuad,
        InQuad,
        OutBack,
        InBack,
        OutBounce,
        InOutSine,
        InExpo,
    }

    // ===================================================== ALANLAR

    public Palette palette = new Palette();
    public Typography typography = new Typography();
    public Spacing spacing = new Spacing();
    public Shape shape = new Shape();
    public Motion motion = new Motion();

    // ===================================================== YARDIMCI

    public static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }

    /// <summary>Verilen renk temada var mı? (Validator kullanıyor.)</summary>
    public bool IsThemeColor(Color c, float tolerance = 0.004f)
    {
        foreach (Color t in AllColors)
        {
            if (Mathf.Abs(t.r - c.r) <= tolerance &&
                Mathf.Abs(t.g - c.g) <= tolerance &&
                Mathf.Abs(t.b - c.b) <= tolerance) return true;
        }
        return false;
    }

    public Color[] AllColors
    {
        get
        {
            return new[]
            {
                palette.primary, palette.primaryBright, palette.secondary,
                palette.danger, palette.success, palette.warning,
                palette.textPrimary, palette.textSecondary, palette.textOnLight,
                palette.panelBackground, palette.cardBackground,
            };
        }
    }

    /// <summary>Verilen punto tipografi ölçeğinde var mı?</summary>
    public bool IsThemeFontSize(float size)
    {
        foreach (TypeStyle s in typography.All)
        {
            if (Mathf.Abs(s.size - size) < 0.01f) return true;
        }
        return false;
    }
}
