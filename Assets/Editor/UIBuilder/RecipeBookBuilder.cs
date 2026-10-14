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
    private const float RowHeight = 92f;

    /// <summary>Satır sonundaki kilit ikonu.</summary>
    private static readonly Vector2 LockSize = new Vector2(44f, 44f);

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

        // --- zemin
        GameObject backdrop = b.Ensure(PanelPath, "Backdrop");
        b.SetStretch(backdrop, UITokens.Zero, UITokens.One);
        b.EnsureComponent<Image>(backdrop, image =>
        {
            image.color = theme.palette.panelBackground;
            image.raycastTarget = true;     // arkadaki panele tıklama geçmesin
        });

        // --- başlık
        // Başlık bandı geri butonunun SAĞINDAN başlar: ölçümde 0.10 bandı
        // geri butonuyla (x -470..-350) 3329 birim² çakışıyordu.
        GameObject title = b.Ensure(PanelPath, "TitleText");
        b.SetBand(title, 0.22f, 0.78f, 0.885f, 0.945f);
        b.EnsureComponent<TextMeshProUGUI>(title, tmp => ApplyText(tmp, theme,
            theme.typography.title, theme.palette.primaryBright, TextAlignmentOptions.Center));
        EnsureLocalized(b, title, CodeStrings.RecipeBookTitle);

        // --- ilerleme halkası
        GameObject ring = b.Ensure(PanelPath, "ProgressRing");
        b.SetFixed(ring, new Vector2(0.5f, 0.845f), new Vector2(96f, 96f));
        b.EnsureComponent<Image>(ring, image =>
        {
            image.sprite = LoadSprite("Assets/Art/Figures/Cember.png");
            image.color = theme.palette.primary;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.preserveAspect = true;
            image.raycastTarget = false;
        });

        // --- sayaç
        GameObject counter = b.Ensure(PanelPath, "CounterText");
        b.SetBand(counter, 0.10f, 0.90f, 0.760f, 0.808f);
        b.EnsureComponent<TextMeshProUGUI>(counter, tmp => ApplyText(tmp, theme,
            theme.typography.body, theme.palette.textSecondary, TextAlignmentOptions.Center));

        // --- kaydırma
        GameObject scroll = b.Ensure(PanelPath, "Scroll View");
        b.SetBand(scroll, 0.07f, 0.93f, 0.085f, 0.735f);
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

        GameObject entry = b.Ensure(menuPath, "RecipeBookButton");
        if (entry == null) return;

        // Ansiklopedi butonu y = -137; defter girişi onun sağında
        b.SetFixed(entry, UITokens.Center, new Vector2(150f, 150f), new Vector2(360f, -137f));

        b.EnsureComponent<Image>(entry, image =>
        {
            image.sprite = LoadSprite("Assets/Art/Figures/SettingsPanel_modsecimkartı.png");
            image.color = theme.palette.textPrimary;
            image.preserveAspect = true;
        });
        b.EnsureComponent<Button>(entry, button => button.transition = Selectable.Transition.ColorTint);

        GameObject label = b.Ensure(menuPath + "/RecipeBookButton", "Label");
        b.SetStretch(label, UITokens.Zero, UITokens.One);
        b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(tmp, theme,
            theme.typography.label, theme.palette.textOnLight, TextAlignmentOptions.Center));
        EnsureLocalized(b, label, CodeStrings.RecipeBookTitle);

        if (dryRun) return;

        MainMenuManager menu = Object.FindObjectOfType<MainMenuManager>(true);
        Button entryButton = entry.GetComponent<Button>();
        if (menu != null && entryButton != null) Wire(entryButton, menu, menu.OpenRecipeBook);
    }

    /// <summary>Oyun ekranındaki duraklat menüsünde giriş.</summary>
    private static void BuildPauseEntry(bool dryRun)
    {
        const string gameScene = "Assets/Scenes/Game.unity";
        const string boxPath = "Canvas/PausePanel/ContainerBox";

        UIBuilder b = UIBuilder.Begin("RecipeBookPauseEntry", gameScene, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject entry = b.Ensure(boxPath, "RecipeBookButton");
        if (entry == null) { b.End(); return; }

        // Ölçüm: ContainerBox y -479..479; ResumeButton -378..-323,
        // MainMenuButton -382..-319. Defter girişi bu satırın ALTINDAKİ
        // boşluğa oturur (-479..-382 arası 97 birim).
        b.SetFixed(entry, UITokens.Center, new Vector2(300f, 64f), new Vector2(0f, -430.5f));

        b.EnsureComponent<Image>(entry, image =>
        {
            image.sprite = LoadSprite("Assets/Art/Figures/SettingsPanel_modsecimkartı.png");
            image.color = theme.palette.textPrimary;
            image.preserveAspect = true;
        });
        b.EnsureComponent<Button>(entry, button => button.transition = Selectable.Transition.ColorTint);

        GameObject label = b.Ensure(boxPath + "/RecipeBookButton", "Label");
        b.SetStretch(label, UITokens.Zero, UITokens.One);
        b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(tmp, theme,
            theme.typography.label, theme.palette.textOnLight, TextAlignmentOptions.Center));
        EnsureLocalized(b, label, CodeStrings.RecipeBookTitle);

        if (!dryRun)
        {
            UIManager ui = Object.FindObjectOfType<UIManager>(true);
            Button entryButton = entry.GetComponent<Button>();
            if (ui != null && entryButton != null) Wire(entryButton, ui, ui.OpenRecipeBook);
        }

        b.End();
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
