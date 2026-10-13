using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ayarlar panelinin cam kart içeriğini kurar.
///
/// Bu kurucu aynı zamanda UIBuilder altyapısının KANITI: içerik daha önce elle
/// (NexumSettingsLayout ile) kurulmuştu. Kurucu çalıştırıldığında sahne diff'i
/// BOŞ kalıyor — yani kurucu, elde var olanı birebir üretebiliyor.
///
/// Menüden: NEXUM → Ayarlar Panelini Yerleştir
/// Batch mode: -executeMethod SettingsCardBuilder.Run   (rapor için -dryRun ekleyin)
/// </summary>
public static class SettingsCardBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GlassPath = "Canvas/SafeAreaRoot/SettingsPanel/GlassBackground";
    private const string ContentName = "CardContent";

    private struct Band
    {
        public string name;
        public float x0, x1, y0, y1;
        public Vector2 fixedSize;     // (0,0) değilse nokta anchor + sabit boyut

        public Band(string n, float ax0, float ax1, float ay0, float ay1)
        { name = n; x0 = ax0; x1 = ax1; y0 = ay0; y1 = ay1; fixedSize = Vector2.zero; }

        public Band(string n, float ax0, float ax1, float ay0, float ay1, Vector2 size)
        { name = n; x0 = ax0; x1 = ax1; y0 = ay0; y1 = ay1; fixedSize = size; }
    }

    // Kartın üç bölümü UITokens.CardDividerUpper / CardDividerLower ile ayrılır.
    // Dikey ritim her bölümde aynı (UITokens.Rhythm*).
    private static readonly Band[] Layout =
    {
        // 1. bölüm: başlık + oyun modu. Logo ve "AYARLARI" sabit boyutlu:
        // oransal olsalardı kart büyüyünce logo dokusunun ekranda kullanım
        // boyutu da büyüyüp çözünürlük uyarısı doğuruyordu.
        new Band("Image",               0.175f, 0.520f, 0.8815f, 0.9315f, new Vector2(372f, 87f)),
        new Band("Text (TMP)",          0.530f, 0.865f, 0.8815f, 0.9315f, new Vector2(367f, 87f)),
        new Band("GameModeText",        UITokens.CardInsetLeft, UITokens.CardInsetRight, 0.8235f, 0.8665f),
        new Band("ModeContainer",       UITokens.CardInsetLeft, UITokens.CardInsetRight, 0.6105f, 0.8085f),

        // 2. bölüm: ses
        new Band("AudioTitle",          UITokens.CardInsetLeft, UITokens.CardInsetRight, 0.5220f, 0.5650f),
        new Band("MusicRow",            0.220f, 0.780f, 0.4490f, 0.5070f),
        new Band("SfxRow",              0.220f, 0.780f, 0.3790f, 0.4370f),

        // 3. bölüm: öğrenme
        new Band("LearningTitle",       UITokens.CardInsetLeft, UITokens.CardInsetRight, 0.2642f, 0.3072f),
        new Band("FlashcardsRow",       0.220f, 0.780f, 0.1912f, 0.2492f),
        new Band("HintsRow",            0.220f, 0.780f, 0.1212f, 0.1792f),
        new Band("ResetProgressButton", 0.320f, 0.680f, 0.0762f, 0.1062f),
    };

    [MenuItem("NEXUM/Ayarlar Panelini Yerleştir")]
    public static void RunFromMenu()
    {
        Build(false);
    }

    public static void Run()
    {
        Build(UIBuilder.DryRunRequested);
    }

    public static void RunDryRun()
    {
        Build(true);
    }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("SettingsCard", ScenePath, dryRun);

        GameObject glass = b.FindObject(GlassPath);
        if (glass == null)
        {
            b.Error($"bulunamadı: {GlassPath}");
            b.End();
            return;
        }

        // CardContent: GÖRÜNEN kartla birebir aynı dikdörtgen. GlassBackground
        // tam ekran stretch ama sprite'ı PreserveAspect ile çiziliyor; içerik
        // doğrudan oraya konursa kart büyüyüp küçüldükçe kayıyor.
        // adopt: true -> bu obje daha önce elle kurulmuştu, kurucu devralıyor.
        GameObject content = b.Ensure(GlassPath, ContentName, adopt: true);
        if (content == null)
        {
            b.End();
            return;
        }

        b.SetStretch(content, UITokens.Zero, UITokens.One);
        b.EnsureComponent<AspectRatioFitter>(content, fitter =>
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = UITokens.CardAspect;
            fitter.enabled = true;
        });

        foreach (Band band in Layout)
        {
            GameObject child = b.FindObject(GlassPath + "/" + band.name)
                               ?? b.FindObject(GlassPath + "/" + ContentName + "/" + band.name);
            if (child == null)
            {
                b.Error($"içerik bulunamadı: {band.name}");
                continue;
            }

            b.Reparent(child, content);

            if (band.fixedSize != Vector2.zero)
            {
                Vector2 point = new Vector2((band.x0 + band.x1) * 0.5f, (band.y0 + band.y1) * 0.5f);
                b.SetFixed(child, point, band.fixedSize);
            }
            else
            {
                b.SetBand(child, band.x0, band.x1, band.y0, band.y1);
            }
        }

        // Logo gerilmesin
        GameObject logo = b.FindObject(GlassPath + "/" + ContentName + "/Image");
        if (logo != null)
        {
            Image image = logo.GetComponent<Image>();
            if (image != null) b.Apply(logo, image, i => i.preserveAspect = true);
        }

        // "AYARLARI" logonun hemen sağında başlasın; bölüm başlıkları ortalı
        SetAlignment(b, content, "Text (TMP)", TextAlignmentOptions.Left);
        foreach (string title in new[] { "GameModeText", "AudioTitle", "LearningTitle" })
        {
            SetAlignment(b, content, title, TextAlignmentOptions.Center);
        }

        b.End();
    }

    private static void SetAlignment(UIBuilder b, GameObject content, string name, TextAlignmentOptions align)
    {
        Transform t = content.transform.Find(name);
        if (t == null) return;

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;

        b.Apply(t.gameObject, tmp, x => x.alignment = align);
    }
}
