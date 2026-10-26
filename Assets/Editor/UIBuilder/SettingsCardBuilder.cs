using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ayarlar panelinin kartını ve içeriğini kurar.
///
/// 2d-3a DÜZELTMESİNDE YENİDEN YAZILDI. Üç kusur aynı kökten geliyordu:
///
/// 1) <b>Çöken mod butonları.</b> <c>ModeContainer</c> bir
///    <c>VerticalLayoutGroup</c> idi ve <c>childControlWidth</c> açıktı; yani
///    çocuğun genişliğini <c>Image.preferredWidth</c> belirliyordu. Sprite
///    9-slice olunca (<c>Image.Type.Sliced</c>) bu değer dokunun genişliği
///    olmaktan çıkıp KENARLIK TOPLAMI oldu (1365 → 56) ve üç buton 56×56'ya
///    çöktü. <b>Seçim: layout grubu yerine anchor.</b> Satırda sabit üç buton
///    var; grup hiçbir şey kazandırmıyor ama sprite ölçüsüne gizli bir
///    bağımlılık getiriyor. Grup devre dışı, butonlar açık dikdörtgenle
///    konuyor — ölçü artık sprite'tan bağımsız.
///
/// 2) <b>Ekranı kaplayan levha.</b> <c>GlassBackground</c> tam ekran
///    stretch'ti ve eski <c>SettingsPanel_.png</c> boyalı bir karttı: kenar
///    payı PİKSELİN İÇİNDE pişmişti. 9-slice karşılığının iç payı yok,
///    dikdörtgeni kenardan kenara doldurur. Artık kart bir KART: içerik
///    kutusu + açık iç pay (kenarlık + <see cref="UITokens.PanelPadding"/>).
///
/// 3) <b>Dokunma hedefi.</b> Mod butonları 117,8, anahtarlar 60,
///    "İLERLEMEYİ SIFIRLA" 52 birimdi — hepsi
///    <see cref="UITokens.TouchMin"/> (144) altında. <b>GRAFİKLERİ
///    BÜYÜTÜLMEDİ:</b> dokunma alanı saydam bir <c>TouchArea</c> çocuğuyla
///    tamamlanıyor (<c>NexumTouchTargets</c>). Mod butonları dikey üçlü
///    düzeninde ve 480×118 ölçüsünde KALIYOR; anahtar 130×60, sıfırlama
///    butonu 560×52.
///
/// Yerleşim artık ORANLA değil BİRİMLE tanımlı (<see cref="Layout"/>): oran
/// tablosu kartın hangi ölçüde olduğunu gizliyordu, birim tablosu dokunma
/// hedefi gibi mutlak kuralları doğrudan ifade ediyor.
///
/// Menüden: NEXUM → Ayarlar Panelini Yerleştir
/// Batch mode: -executeMethod SettingsCardBuilder.Run   (rapor için -dryRun ekleyin)
/// </summary>
public static class SettingsCardBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PanelPath = "Canvas/SafeAreaRoot/SettingsPanel";
    private const string GlassPath = PanelPath + "/GlassBackground";
    private const string ContentName = "CardContent";

    /// <summary>Kart içerik kutusu (birim). Kart bunun etrafına iç payla kurulur.</summary>
    private const float ContentW = 800f;
    // 1505 -> 1560: anahtarların 144'lük dokunma alanları 117 birimlik satır
    // hatvesinde 27 birim kesişiyordu. Hatve 146'ya çıkınca içerik sığmadı;
    // grafikleri küçültmek yerine kart büyütüldü (1664 < 1920, her ekranda sığar).
    private const float ContentH = 1560f;

    private const float TitleH = 75f;        // bölüm başlığı
    private const float RowW = 604f;         // anahtar satırı genişliği
    private const float RowH = 101f;         // anahtar satırı yüksekliği
    private const float ModeW = 480f;        // mod butonu (GRAFİK ölçüsü, değişmedi)
    private const float ModeH = 118f;
    // 10 -> 26: 118 birimlik grafiklerin 144'lük dokunma alanları 128
    // hatvede 16 birim kesişiyordu. Hatve = 118 + 26 = 144.
    private const float ModeGap = 26f;

    private const string ToggleOffPath = "Assets/Art/UI/toggle_off_normal.png";
    private const float ModeBlock = 3f * ModeH + 2f * ModeGap;   // 374

    private struct Band
    {
        public string name;
        public float top;        // içerik kutusunun ÜSTÜNDEN birim
        public float height;
        public float width;      // 0 = tam genişlik
        public float centerX;    // width > 0 iken merkez (NaN = ortala)

        public Band(string n, float t, float h, float w = 0f, float cx = float.NaN)
        { name = n; top = t; height = h; width = w; centerX = cx; }
    }

    // Dikey ritim: başlık 75 · satır 144 (dokunma hedefi) · bölüm arası 56 ·
    // başlık altı 24 · satır arası 16. Toplam 1498 <= 1505.
    // Dikey ritim (birim, içerik kutusunun üstünden). Grafik ölçüleri
    // DEĞİŞMEDİ: mod butonu 480×118, anahtar satırı 604×101, sıfırlama
    // butonu 560×52. Dokunma alanı ayrı bir katman.
    // Dikey ritim (birim, içerik kutusunun üstünden). Grafik ölçüleri
    // DEĞİŞMEDİ: mod butonu 480×118, anahtar satırı 604×101, sıfırlama
    // butonu 560×52. Dokunma alanı ayrı bir katman; aralıklar o katmanın
    // KESİŞMEMESİ için açıldı (anahtar hatvesi 146, mod hatvesi 144).
    private static readonly Band[] Layout =
    {
        new Band("Image",                  0f,    87f,  372f, 239.5f),   // logo
        new Band("Text (TMP)",             0f,    87f,  367f, 617.2f),   // "AYARLARI"
        new Band("GameModeText",         133f, TitleH),
        new Band("ModeContainer",        232f, ModeBlock),
        new Band("AudioTitle",           674f, TitleH),
        new Band("MusicRow",             773f, RowH, RowW),
        new Band("SfxRow",               919f, RowH, RowW),
        new Band("LearningTitle",       1056f, TitleH),
        new Band("FlashcardsRow",       1155f, RowH, RowW),
        new Band("HintsRow",            1301f, RowH, RowW),
        new Band("ResetProgressButton", 1472f,  52f, 560f),
    };

    private static readonly string[] ModeButtons = { "NormalModeBtn", "ExamModeBtn", "FreeModeBtn" };

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

        // --- KART: iç pay sprite'ın KENDİ kenarlığından okunur, sabit yazılmaz
        Image glassImage = glass.GetComponent<Image>();
        float inset = UITokens.PanelFrameInset(glassImage != null ? glassImage.sprite : null)
                      + UITokens.PanelPadding;
        float cardW = ContentW + 2f * inset;
        float cardH = ContentH + 2f * inset;

        // Kart SABİT BİRİMDE. Oranla (AspectRatioFitter) kurulursa kartın
        // gerçek ölçüsü ekrana göre değişir ve birimle tanımlı ritim kayar;
        // ilk denemede içerik 944x1816'lık bir kutunun ortasına sıkıştı.
        // CanvasScaler Expand olduğu için tasarım alanı hiçbir ekranda
        // 1080x1920'nin altına düşmez: 904x1609 her ekrana sığar.
        AspectRatioFitter kartFitter = glass.GetComponent<AspectRatioFitter>();
        if (kartFitter != null) b.Apply(glass, kartFitter, f => f.enabled = false);

        b.SetFixed(glass, UITokens.Center, new Vector2(cardW, cardH), Vector2.zero);

        // --- İÇERİK KUTUSU: kartın içinde, her kenardan inset birim
        // adopt: true -> bu obje daha önce elle kurulmuştu, kurucu devralıyor.
        GameObject content = b.Ensure(GlassPath, ContentName, adopt: true);
        if (content == null)
        {
            b.End();
            return;
        }

        // Eski sürümde burada AspectRatioFitter vardı ve oranı eski cam kart
        // sprite'ının (796x1280) ölçüsüydü. Oran artık KARTTA; içerik kutusu
        // kartın içinde sabit payla duruyor.
        AspectRatioFitter eskiFitter = content.GetComponent<AspectRatioFitter>();
        if (eskiFitter != null) b.Apply(content, eskiFitter, f => f.enabled = false);

        b.SetRect(content, UITokens.Zero, UITokens.One, UITokens.Center,
                  new Vector2(inset, inset), new Vector2(-inset, -inset));

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
            Yerlestir(b, child, band);
        }

        KurModSatiri(b, content);
        KurAnahtarSatirlari(b, content);

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

    /// <summary>Band'i içerik kutusunun ÜST-SOL köşesine göre birimle yerleştirir.</summary>
    private static void Yerlestir(UIBuilder b, GameObject go, Band band)
    {
        float cy = ContentH * 0.5f - (band.top + band.height * 0.5f);   // merkezden aşağı

        if (band.width > 0f)
        {
            float cx = float.IsNaN(band.centerX) ? 0f : band.centerX - ContentW * 0.5f;
            b.SetFixed(go, UITokens.Center, new Vector2(band.width, band.height), new Vector2(cx, cy));
        }
        else
        {
            // Tam genişlik: yatayda stretch, dikeyde sabit yükseklik
            b.SetRect(go, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UITokens.Center,
                      new Vector2(0f, cy - band.height * 0.5f),
                      new Vector2(0f, cy + band.height * 0.5f));
        }
    }

    /// <summary>
    /// Mod seçimi: layout grubu DEVRE DIŞI, üç buton tek satırda açık
    /// dikdörtgenle. Grup <c>childControlWidth</c> ile sprite'ın
    /// <c>preferredWidth</c>'ini okuyordu ve 9-slice geçişinde 56'ya çöküyordu.
    /// </summary>
    private static void KurModSatiri(UIBuilder b, GameObject content)
    {
        GameObject container = b.FindObject(GlassPath + "/" + ContentName + "/ModeContainer");
        if (container == null)
        {
            b.Error("ModeContainer yok");
            return;
        }

        HorizontalOrVerticalLayoutGroup grup = container.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (grup != null) b.Apply(container, grup, g => g.enabled = false);

        float pitch = ModeH + ModeGap;
        for (int i = 0; i < ModeButtons.Length; i++)
        {
            GameObject btn = b.FindObject(GlassPath + "/" + ContentName + "/ModeContainer/" + ModeButtons[i]);
            if (btn == null)
            {
                b.Error($"mod butonu yok: {ModeButtons[i]}");
                continue;
            }

            float cy = ModeBlock * 0.5f - ModeH * 0.5f - i * pitch;
            b.SetFixed(btn, UITokens.Center, new Vector2(ModeW, ModeH), new Vector2(0f, cy));

            Transform label = btn.transform.Find("Text (TMP)");
            if (label != null) b.SetStretch(label.gameObject, UITokens.Zero, UITokens.One);
        }
    }

    /// <summary>
    /// Anahtar satırları: etiket solda, anahtar sağda, ikisi de dikeyde
    /// ortalı. <b>Anahtarın grafiği 130×60 kalıyor</b>; dokunma alanı
    /// <c>NexumTouchTargets</c> tarafından saydam çocukla 144'e tamamlanır.
    /// </summary>
    private static void KurAnahtarSatirlari(UIBuilder b, GameObject content)
    {
        string[] satirlar = { "MusicRow", "SfxRow", "FlashcardsRow", "HintsRow" };

        foreach (string satirAd in satirlar)
        {
            GameObject satir = b.FindObject(GlassPath + "/" + ContentName + "/" + satirAd);
            if (satir == null)
            {
                b.Error($"satır yok: {satirAd}");
                continue;
            }

            HorizontalOrVerticalLayoutGroup grup = satir.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (grup != null) b.Apply(satir, grup, g => g.enabled = false);

            foreach (Transform t in satir.transform)
            {
                GameObject go = t.gameObject;
                if (go.name.EndsWith("ToggleBtn"))
                {
                    b.SetFixed(go, UITokens.Center, new Vector2(130f, 60f),
                               new Vector2(RowW * 0.5f - 65f - 10f, 0f));

                    // Serileştirilmiş sprite de kitten olsun: SettingsManager
                    // çalışma zamanında açık/kapalı sprite'ını atıyor ama
                    // edit mode'da (ve gerçek render'da) yerleşik beyaz
                    // dikdörtgen görünüyordu.
                    Image img = go.GetComponent<Image>();
                    Sprite kapali = AssetDatabase.LoadAssetAtPath<Sprite>(ToggleOffPath);
                    if (img != null && kapali != null)
                    {
                        b.Apply(go, img, i =>
                        {
                            i.sprite = kapali;
                            i.type = Image.Type.Simple;
                            i.color = Color.white;
                            i.preserveAspect = true;
                        });
                    }
                }
                else if (go.name.EndsWith("Text"))
                {
                    b.SetFixed(go, UITokens.Center, new Vector2(380f, 65f),
                               new Vector2(-RowW * 0.5f + 190f + 10f, 0f));
                    TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) b.Apply(go, tmp, x => x.alignment = TextAlignmentOptions.Left);
                }
            }
        }
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
