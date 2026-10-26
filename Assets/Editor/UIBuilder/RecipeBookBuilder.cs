using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarif defteri panelini kurar (MainMenu sahnesi).
///
/// Yapı:
///   RecipeBookPanel (stretch, kapalı)
///     Backdrop        tam ekran zemin
///     TitleText       "TARİF DEFTERİ"
///     ProgressRing    halka (Cember.png, radial dolum)
///     CounterText     "8 / 21 tepkime keşfedildi"
///     Scroll View     ScrollRect
///       Viewport      Mask
///         Content     VerticalLayoutGroup + ContentSizeFitter
///           Row0..20  Button + Label
///     BackButton
///
/// Menüden: NEXUM → Tarif Defterini Kur
/// Batch mode: -executeMethod RecipeBookBuilder.Run   (rapor için -dryRun)
/// </summary>
public static class RecipeBookBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string ParentPath = "Canvas/SafeAreaRoot";
    private const string PanelName = "RecipeBookPanel";
    private const int RowCount = 21;

    /// <summary>
    /// Satır yüksekliği. 76 birimde satır metni kutusunun %94'ünü dolduruyordu
    /// (%85 kuralı); 92 birimde %77'ye iniyor.
    /// </summary>
    // 92 -> 144: LİSTEDE DOKUNMA ALANI TAŞIRILMAZ. 92'lik satıra 144'lük
    // saydam alan konunca komşu satırlarla 52 birim örtüşüyordu ve oyuncu
    // yanlış tarifi açabiliyordu (ölçüm: 20 çift, 6336 birim²). Liste
    // bağlamında doğru çözüm satırı GERÇEKTEN büyütmek: kaydırılabilir bir
    // listede satır yüksekliği tasarımın parçası, kalabalık değil.
    private const float RowHeight = UITokens.TouchMin;

    /// <summary>Satır sonundaki kilit ikonu.</summary>
    private static readonly Vector2 LockSize = new Vector2(44f, 44f);

    // --- paylaşılan varlıklar (yeni görsel üretilmez, mevcutlar paylaşılır)
    private const string MenuBackgroundPath = "Assets/Art/Figures/arkaplan.png";
    private const float MenuBackgroundAspect = 0.56279904f;   // 941x1672, MainMenu/Background ile aynı
    private const string LogoPath = "Assets/Art/Figures/NEXUM_logo.png";
    private static readonly Vector2 LogoSize = new Vector2(500f, 130f);
    private static readonly Vector2 LogoPosition = new Vector2(0f, -83.88f);   // Game sahnesindeki logo ile aynı

    private const string RingSpritePath = "Assets/Art/Figures/ring_progress.png";
    private static readonly Vector2 RingAnchor = new Vector2(0.5f, 0.785f);
    // 200 birimde ortadaki "21/21" 178 birim istiyordu, delik 144 birimdi.
    private static readonly Vector2 RingSize = new Vector2(220f, 220f);

    /// <summary>Menü butonlarının ortak ölçüsü (ANSİKLOPEDİ / AYARLAR ile aynı).</summary>
    private static readonly Vector2 MenuButtonSize = new Vector2(522f, 220f);

    /// <summary>Beş butonun merkez aralığı (mevcut ritim: 265 birim).</summary>
    private const float MenuButtonSpacing = 265f;
    private const float EncyclopediaY = -137f;

    // Metinsiz buton çerçevesi. Diğer dört menü butonunun metni GÖRSELİN
    // İÇİNDE basılı olduğu için birebir aynı sprite kullanılamaz; siluet olarak
    // en yakın olanı seçildi (köşeleri pahlı dikdörtgen çerçeve, ANSİKLOPEDİ /
    // AYARLAR ile aynı dil). İç dolgu açık olduğu için metin koyu renkte.
    private const string MenuButtonFramePath = "Assets/Art/UI/btn_secondary_normal.png";
    private static readonly Vector2 MenuButtonLabelSize = new Vector2(430f, 120f);

    /// <summary>Duraklat kartındaki defter girişi (cam alanın altı).</summary>
    // Duraklat kartı: kart 688x958, çerçeve payı sprite'ın kenarlığından +
    // UITokens.PanelPadding. Buton GRAFİKLERİ eski ölçülerinde; dokunma
    // alanları saydam TouchArea çocuğuyla tamamlanıyor.
    private static readonly Vector2 PauseCardSize = new Vector2(688f, 958f);
    private static readonly Vector2 PauseButtonSize = new Vector2(560f, 64f);
    // -265 -> -195: 64 birimlik grafiğin 144'lük dokunma alanı DEVAM ET /
    // ANA MENÜ satırının alanıyla 58 birim kesişiyordu.
    private const float PauseButtonY = -195f;
    private const float PauseActionY = -350.59f;
    private static readonly Vector2 ResumeSize = new Vector2(236.08f, 54.86f);
    private static readonly Vector2 MainMenuSize = new Vector2(239.6f, 62.55f);
    private const float PauseActionX = 180f;
    private const string ModalFramePath = "Assets/Art/UI/modal_normal.png";

    /// <summary>Katalizör göstergesinin çerçeve görseli (yerel parıltı aynı şekli alır).</summary>
    private const string MeterFramePath = "Assets/Art/Figures/Entropitup.png";

    private static string PanelPath { get { return ParentPath + "/" + PanelName; } }
    private static string ContentPath { get { return PanelPath + "/Scroll View/Viewport/Content"; } }

    [MenuItem("NEXUM/Tarif Defterini Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    public static void RunDryRun() { Build(true); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("RecipeBook", ScenePath, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject panel = b.Ensure(ParentPath, PanelName);
        if (panel == null) { b.End(); return; }
        b.SetStretch(panel, UITokens.Zero, UITokens.One);

        // --- zemin: ANA MENÜ ile aynı arka plan (tek kaynak, yeni görsel yok).
        // AspectRatioFitter Envelope Parent: dolgu görseli gerilmez, kırpılır
        // (CLAUDE.md "Arka planlar" kuralı).
        GameObject backdrop = b.Ensure(PanelPath, "Backdrop");
        b.SetStretch(backdrop, UITokens.Zero, UITokens.One);
        b.EnsureComponent<Image>(backdrop, image =>
        {
            image.sprite = LoadSprite(MenuBackgroundPath);
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = true;     // arkadaki panele tıklama geçmesin
        });
        b.EnsureComponent<AspectRatioFitter>(backdrop, fitter =>
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = MenuBackgroundAspect;
        });

        // --- logo: Game sahnesindeki NEXUM logosunun aynısı, aynı üst bant
        GameObject logo = b.Ensure(PanelPath, "Logo");
        b.SetFixed(logo, new Vector2(0.5f, 1f), LogoSize, LogoPosition);
        b.EnsureComponent<Image>(logo, image =>
        {
            image.sprite = LoadSprite(LogoPath);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        });

        // --- başlık
        // Logo eklendikten sonra yeniden ölçüldü: logo y 811..941, geri butonu
        // y 814..934 (x -470..-350). Başlık ikisinin de ALTINDA kendi bandında.
        GameObject title = b.Ensure(PanelPath, "TitleText");
        b.SetBand(title, 0.22f, 0.78f, 0.862f, 0.912f);
        b.EnsureComponent<TextMeshProUGUI>(title, tmp => ApplyText(tmp, theme,
            theme.typography.title, theme.palette.primaryBright, TextAlignmentOptions.Center));
        EnsureLocalized(b, title, CodeStrings.RecipeBookTitle);

        // --- ilerleme halkası
        // Eski hâli DOLU daireydi (Cember.png); radyal dolgu halka değil pasta
        // dilimi çiziyordu. Artık gerçek halka (Tools/generate_ring.py) ve
        // altında dolmamış kısmı gösteren soluk "track" var.
        GameObject track = b.Ensure(PanelPath, "ProgressRingTrack");
        b.SetFixed(track, RingAnchor, RingSize);
        b.EnsureComponent<Image>(track, image =>
        {
            image.sprite = LoadSprite(RingSpritePath);
            image.color = new Color(theme.palette.textSecondary.r, theme.palette.textSecondary.g,
                                    theme.palette.textSecondary.b, 0.25f);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        });

        GameObject ring = b.Ensure(PanelPath, "ProgressRing");
        b.SetFixed(ring, RingAnchor, RingSize);
        b.EnsureComponent<Image>(ring, image =>
        {
            image.sprite = LoadSprite(RingSpritePath);
            image.color = theme.palette.primary;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.preserveAspect = true;
            image.raycastTarget = false;
        });

        GameObject ringValue = b.Ensure(PanelPath, "RingValueText");
        b.SetFixed(ringValue, RingAnchor, new Vector2(RingSize.x * 0.72f, RingSize.y * 0.45f));
        b.EnsureComponent<TextMeshProUGUI>(ringValue, tmp => ApplyText(tmp, theme,
            theme.typography.subtitle, theme.palette.primaryBright, TextAlignmentOptions.Center));

        // --- sayaç
        GameObject counter = b.Ensure(PanelPath, "CounterText");
        b.SetBand(counter, 0.10f, 0.90f, 0.676f, 0.716f);
        b.EnsureComponent<TextMeshProUGUI>(counter, tmp => ApplyText(tmp, theme,
            theme.typography.body, theme.palette.textSecondary, TextAlignmentOptions.Center));

        // --- kaydırma
        GameObject scroll = b.Ensure(PanelPath, "Scroll View");
        b.SetBand(scroll, 0.07f, 0.93f, 0.085f, 0.665f);
        b.EnsureComponent<Image>(scroll, image =>
        {
            image.color = theme.palette.cardBackground;
            image.raycastTarget = true;
        });

        GameObject viewport = b.Ensure(PanelPath + "/Scroll View", "Viewport");
        b.SetStretch(viewport, UITokens.Zero, UITokens.One);
        b.EnsureComponent<Image>(viewport, image =>
        {
            image.color = theme.palette.cardBackground;
            image.raycastTarget = true;
        });
        b.EnsureComponent<Mask>(viewport, mask => mask.showMaskGraphic = false);

        GameObject content = b.Ensure(PanelPath + "/Scroll View/Viewport", "Content");
        b.SetRect(content, new Vector2(0f, 1f), new Vector2(1f, 1f),
                  new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        b.EnsureComponent<VerticalLayoutGroup>(content, layout =>
        {
            layout.spacing = theme.spacing.xs;
            layout.padding = new RectOffset((int)theme.spacing.sm, (int)theme.spacing.sm,
                                            (int)theme.spacing.sm, (int)theme.spacing.sm);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
        });
        b.EnsureComponent<ContentSizeFitter>(content, fitter =>
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        });

        ScrollRect scrollRect = b.EnsureComponent<ScrollRect>(scroll, rect =>
        {
            rect.horizontal = false;
            rect.vertical = true;
            rect.movementType = ScrollRect.MovementType.Elastic;
            rect.scrollSensitivity = 30f;
        });

        // --- satırlar
        for (int i = 0; i < RowCount; i++)
        {
            GameObject row = b.Ensure(ContentPath, "Row" + i);
            if (row == null) continue;

            b.SetFixed(row, UITokens.Center, new Vector2(0f, RowHeight));
            b.EnsureComponent<Image>(row, image =>
            {
                image.color = theme.palette.panelBackground;
                image.raycastTarget = true;
            });
            b.EnsureComponent<Button>(row, button => button.transition = Selectable.Transition.ColorTint);
            b.EnsureComponent<LayoutElement>(row, element =>
            {
                element.minHeight = RowHeight;
                element.preferredHeight = RowHeight;
            });

            GameObject label = b.Ensure(ContentPath + "/Row" + i, "Label");
            b.SetStretch(label, UITokens.Zero, UITokens.One);
            b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(tmp, theme,
                theme.typography.body, theme.palette.textPrimary, TextAlignmentOptions.Center));

            // Keşfedilmemiş satırın kilidi; panel açılınca açılıp kapanır
            GameObject lockIcon = b.Ensure(ContentPath + "/Row" + i, "LockIcon");
            b.SetFixed(lockIcon, new Vector2(1f, 0.5f), LockSize, new Vector2(-LockSize.x, 0f));
            b.EnsureComponent<Image>(lockIcon, image =>
            {
                image.sprite = LoadSprite("Assets/Art/Figures/lock_icon.png");
                image.color = theme.palette.textSecondary;
                image.preserveAspect = true;
                image.raycastTarget = false;
            });
        }

        // --- geri
        GameObject back = b.Ensure(PanelPath, "BackButton");
        b.SetFixed(back, new Vector2(0.12f, 0.955f), new Vector2(120f, 120f));
        b.EnsureComponent<Image>(back, image =>
        {
            image.sprite = LoadSprite("Assets/Art/Figures/geri.png");
            image.color = theme.palette.textPrimary;
            image.preserveAspect = true;
        });
        b.EnsureComponent<Button>(back, button => button.transition = Selectable.Transition.ColorTint);

        // --- denetleyici
        RecipeBookPanel controller = b.EnsureComponent<RecipeBookPanel>(panel, panelScript =>
        {
            panelScript.theme = theme;
            panelScript.scrollRect = scrollRect;
            panelScript.counterText = counter != null ? counter.GetComponent<TextMeshProUGUI>() : null;
            panelScript.progressRing = ring != null ? ring.GetComponent<Image>() : null;
            panelScript.ringValueText = ringValue != null ? ringValue.GetComponent<TextMeshProUGUI>() : null;
            panelScript.rowContainer = content != null ? content.transform : null;
            panelScript.encyclopedia = Object.FindObjectOfType<EncyclopediaManager>(true);
        });

        // ScrollRect bağları (bileşenler kurulduktan sonra)
        if (!dryRun && scrollRect != null && viewport != null && content != null)
        {
            b.Apply(scroll, scrollRect, rect =>
            {
                rect.viewport = viewport.GetComponent<RectTransform>();
                rect.content = content.GetComponent<RectTransform>();
            });
        }

        // --- geri butonu: ana menüye dön
        if (!dryRun && back != null)
        {
            MainMenuManager menu = Object.FindObjectOfType<MainMenuManager>(true);
            Button backButton = back.GetComponent<Button>();
            if (menu != null && backButton != null) Wire(backButton, menu, menu.BackToMainMenu);

            // Panel referansını yöneticiye bağla
            if (menu != null && menu.recipeBookPanel != panel)
            {
                SerializedObject so = new SerializedObject(menu);
                SerializedProperty prop = so.FindProperty("recipeBookPanel");
                if (prop != null)
                {
                    prop.objectReferenceValue = panel;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(menu);
                }
            }
        }

        if (!dryRun && panel.activeSelf) panel.SetActive(false);

        BuildMenuEntry(b, theme, dryRun);
        b.End();

        BuildPauseEntry(dryRun);
    }

    /// <summary>Ana menüde Ansiklopedi'nin yanında ikincil giriş.</summary>
    private static void BuildMenuEntry(UIBuilder b, NexumTheme theme, bool dryRun)
    {
        const string menuPath = "Canvas/SafeAreaRoot/MainMenuPanel";

        // Defter girişi dikey yığında ANSİKLOPEDİ ile AYARLAR'ın ARASINA girer.
        // Diğer dört buton 522x220 ve merkez aralıkları 265 birim; bu ritim
        // korunur, yalnızca AYARLAR bir sıra aşağı iner.
        //   Oyna 395 · Nasıl Oynanır 121 · Ansiklopedi -137 · DEFTER -402 · Ayarlar -667
        GameObject entry = b.Ensure(menuPath, "RecipeBookButton");
        if (entry == null) return;

        b.SetFixed(entry, UITokens.Center, MenuButtonSize,
                   new Vector2(0f, EncyclopediaY - MenuButtonSpacing));

        // Diğer menü butonları metni görselin içinde taşıyor; defter için metinsiz
        // çerçeve (level_selection_button) + TMP etiketi kullanılır. Yeni görsel
        // üretilmez. preserveAspect kapalı: kardeşleri de kutularına geriliyor.
        b.EnsureComponent<Image>(entry, image =>
        {
            image.sprite = LoadSprite(MenuButtonFramePath);
            image.color = Color.white;
            image.preserveAspect = false;
        });
        b.EnsureComponent<Button>(entry, button => button.transition = Selectable.Transition.ColorTint);

        GameObject label = b.Ensure(menuPath + "/RecipeBookButton", "Label");
        b.SetFixed(label, UITokens.Center, MenuButtonLabelSize);
        b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(tmp, theme,
            theme.typography.subtitle, theme.palette.textOnLight, TextAlignmentOptions.Center));
        EnsureLocalized(b, label, CodeStrings.RecipeBookButtonLabel);

        // AYARLAR bir sıra aşağı: kurucunun kaydında olmadığı için devralınır
        GameObject settings = b.Ensure(menuPath, "SettingButton", adopt: true);
        if (settings != null)
        {
            b.SetFixed(settings, UITokens.Center, MenuButtonSize,
                       new Vector2(0f, EncyclopediaY - 2f * MenuButtonSpacing));
        }

        if (dryRun) return;

        MainMenuManager menu = Object.FindObjectOfType<MainMenuManager>(true);
        Button entryButton = entry.GetComponent<Button>();
        if (menu != null && entryButton != null) Wire(entryButton, menu, menu.OpenRecipeBook);
    }

    /// <summary>
    /// Oyun ekranındaki duraklat kartı: defter girişi + kart içi denge.
    ///
    /// Kart görseli (gamepanel.png) ALT KENARINDA iki boyalı buton yuvası
    /// taşıyor; DEVAM ET ve ANA MENÜ bu yuvaların üzerinde duruyor. Üçüncü bir
    /// yuva yok, bu yüzden defter girişi yuva satırının ALTINA değil, cam
    /// içerik alanının ALTINA (bilgi metni ile yuva satırı arasına) konur.
    /// Stil ikisiyle aynı: görselsiz, yalnızca TMP metni.
    ///
    /// Ölçüm (ContainerBox yerel, -479..479):
    ///   başlık 506 -> 425   (eski hâli kutunun 52 birim dışındaydı)
    ///   alt başlık 441 -> 350
    ///   bilgi metni 610 -> 480 birim yükseklik, merkez 88 -> 55
    ///   defter girişi 560x64 @ -265  (bilgi metnine 48, yuva satırına 26 pay)
    /// </summary>
    private static void BuildPauseEntry(bool dryRun)
    {
        const string gameScene = "Assets/Scenes/Game.unity";
        const string boxPath = "Canvas/PausePanel/ContainerBox";

        UIBuilder b = UIBuilder.Begin("RecipeBookPauseEntry", gameScene, dryRun);
        NexumTheme theme = UITokens.Theme;

        // --- ÇERÇEVE KARTA TAŞINDI (2d-3a düzeltmesi)
        // Eski gamepanel.png BOYALI bir karttı: kenar payı pikselin içinde
        // pişmişti, bu yüzden 1080x1519'luk PausePanel'in üzerinde "daha
        // küçük bir kart" görünüyordu. 9-slice karşılığının iç payı yok;
        // dikdörtgeni kenardan kenara doldurdu ve kart ekranı kapladı.
        // Çözüm: PausePanel yalnızca karartma katmanı, çerçeve ContainerBox'ta.
        GameObject panel = b.Ensure("Canvas", "PausePanel", adopt: true);
        if (panel != null)
        {
            // Karartma TAM EKRAN: panel 1080x1519 iken üstteki HUD ve alttaki
            // katalizör göstergesi karartmanın dışında kalıyordu (eski opak
            // kart onları örttüğü için görünmüyordu).
            b.SetStretch(panel, UITokens.Zero, UITokens.One);

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                b.Apply(panel, panelImage, i =>
                {
                    i.sprite = null;
                    i.type = Image.Type.Simple;
                    // Karartma rengi PALETTEN: saf siyah tema dışı
                    // (validator uyarıyor) ve diğer karartma katmanları da
                    // 0,78 alfada çalışıyor.
                    i.color = new Color(theme.palette.cardBackground.r,
                                        theme.palette.cardBackground.g,
                                        theme.palette.cardBackground.b, 0.78f);
                    i.raycastTarget = true;
                });
            }
        }

        GameObject box = b.Ensure("Canvas/PausePanel", "ContainerBox", adopt: true);
        if (box != null)
        {
            b.SetFixed(box, UITokens.Center, PauseCardSize, Vector2.zero);
            b.EnsureComponent<Image>(box, image =>
            {
                image.sprite = LoadSprite(ModalFramePath);
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.color = Color.white;
                image.raycastTarget = true;
            });
        }

        // --- kart içi denge (mevcut objeler devralınır)
        // Dikey ritim kartın iç kenarından (479 - kenarlık - PanelPadding)
        // başlar; üç buton da 144 birim yüksekliğinde.
        GameObject pauseTitle = b.Ensure(boxPath, "TitleText", adopt: true);
        if (pauseTitle != null) b.SetFixed(pauseTitle, UITokens.Center, new Vector2(468.8f, 50f), new Vector2(0f, 400f));

        GameObject pauseSubtitle = b.Ensure(boxPath, "SubtitleText", adopt: true);
        if (pauseSubtitle != null) b.SetFixed(pauseSubtitle, UITokens.Center, new Vector2(474.9f, 45.8f), new Vector2(0f, 330f));

        GameObject pauseFact = b.Ensure(boxPath, "FactText", adopt: true);
        // Defter girişi yukarı alınınca bilgi metni 480 -> 420 kısaldı.
        if (pauseFact != null) b.SetFixed(pauseFact, UITokens.Center, new Vector2(596.7f, 420f), new Vector2(0f, 73f));

        // --- DEVAM ET / ANA MENÜ: GRAFİK ölçüleri değişmedi
        GameObject resume = b.Ensure(boxPath, "ResumeButton", adopt: true);
        if (resume != null) b.SetFixed(resume, UITokens.Center, ResumeSize, new Vector2(-PauseActionX, PauseActionY));

        GameObject toMenu = b.Ensure(boxPath, "MainMenuButton", adopt: true);
        if (toMenu != null) b.SetFixed(toMenu, UITokens.Center, MainMenuSize, new Vector2(PauseActionX, PauseActionY));

        // --- defter girişi
        GameObject entry = b.Ensure(boxPath, "RecipeBookButton");
        if (entry == null) { b.End(); return; }

        b.SetFixed(entry, UITokens.Center, PauseButtonSize, new Vector2(0f, PauseButtonY));

        // DEVAM ET / ANA MENÜ ile aynı stil: görsel yok, yalnızca metin
        b.EnsureComponent<Image>(entry, image =>
        {
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.preserveAspect = false;
            image.raycastTarget = true;
        });
        b.EnsureComponent<Button>(entry, button => button.transition = Selectable.Transition.ColorTint);

        GameObject label = b.Ensure(boxPath + "/RecipeBookButton", "Label");
        b.SetStretch(label, UITokens.Zero, UITokens.One);
        b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(tmp, theme,
            theme.typography.body, theme.palette.textPrimary, TextAlignmentOptions.Center));
        EnsureLocalized(b, label, CodeStrings.RecipeBookButtonLabel);

        // --- katalizör göstergesinin YEREL parıltısı
        // Eski hâli tam ekran yeşil tintti (DangerGlowImage 1080x1920, alfa 0.35).
        // Artık göstergenin kendi çerçevesi düşük alfada parlıyor.
        GameObject glow = b.Ensure("Canvas/SafeAreaRoot/PressureMeter", "ReadyGlow");
        if (glow != null)
        {
            b.SetStretch(glow, new Vector2(-0.04f, -0.6f), new Vector2(1.04f, 1.6f));
            b.EnsureComponent<Image>(glow, image =>
            {
                image.sprite = LoadSprite(MeterFramePath);
                image.color = new Color(theme.palette.success.r, theme.palette.success.g,
                                        theme.palette.success.b, 0f);
                image.preserveAspect = true;
                image.raycastTarget = false;
            });
        }

        if (!dryRun)
        {
            UIManager ui = Object.FindObjectOfType<UIManager>(true);
            Button entryButton = entry.GetComponent<Button>();
            if (ui != null && entryButton != null) Wire(entryButton, ui, ui.OpenRecipeBook);

            if (ui != null)
            {
                SerializedObject so = new SerializedObject(ui);
                bool changed = false;

                if (glow != null) changed |= SetReference(so, "catalystGlowImage", glow.GetComponent<Image>());
                changed |= SetHiddenHud(so, b);

                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(ui);
                }
            }
        }

        b.End();
    }

    /// <summary>Alan zaten doğru nesneye bakıyorsa dokunmaz (idempotans).</summary>
    private static bool SetReference(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue == value) return false;

        prop.objectReferenceValue = value;
        return true;
    }

    /// <summary>
    /// Duraklat kartı açıkken gizlenecek HUD objeleri. Kart görseli
    /// x -375..375 / y -448..734 arasında; JokerButton (y 732..832) ve
    /// UndoButton (x 275..525) kartın dışına taşıp kenarından sızıyordu.
    /// </summary>
    private static bool SetHiddenHud(SerializedObject so, UIBuilder b)
    {
        string[] names = { "JokerButton", "UndoButton" };

        SerializedProperty list = so.FindProperty("hudHiddenWhilePaused");
        if (list == null) return false;

        GameObject[] targets = new GameObject[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject go = b.FindObject("Canvas/SafeAreaRoot/" + names[i]);
            if (go == null) return false;
            targets[i] = go;
        }

        bool same = list.arraySize == targets.Length;
        if (same)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue != targets[i]) { same = false; break; }
            }
        }
        if (same) return false;

        list.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
        }
        return true;
    }

    /// <summary>OnClick'e kalıcı dinleyici ekler (zaten varsa dokunmaz).</summary>
    private static void Wire(Button button, Object target, UnityEngine.Events.UnityAction action)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == action.Method.Name) return;
        }
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void ApplyText(TextMeshProUGUI tmp, NexumTheme theme,
                                  NexumTheme.TypeStyle style, Color color, TextAlignmentOptions align)
    {
        tmp.font = style.font;
        tmp.fontSize = style.size;
        tmp.enableAutoSizing = false;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.spriteAsset = Resources.Load<TMP_SpriteAsset>(ReactionText.SpriteAssetResource);
    }

    /// <summary>Sabit metin sözlükten gelsin.</summary>
    private static void EnsureLocalized(UIBuilder b, GameObject go, string key)
    {
        b.EnsureComponent<LocalizedText>(go, loc => loc.key = key);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
