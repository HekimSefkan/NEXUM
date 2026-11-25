using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RÜTBE ATLAMA KUTLAMASI PANELİ (Faz 2 / 2.1).
///
/// <b>İKİ SAHNEYE BİRDEN kurulur.</b> Kutlama iki yerden gerekiyor: bölüm
/// bitişinde (Game, sonuç kartının üstünde) ve ana menüde (MainMenu,
/// geriye dönük hesapla rütbe atlayan oyuncuya bir kez). "Paneli aç"
/// eylemi asla sahne yüklemesi olarak yazılmaz — sahne yüklemek oyun
/// durumunu yok eder (CLAUDE.md panel erişimi kuralı; tarif defteri bu
/// hatayı 2d-3c/1'de yaşadı).
///
/// <b>Canvas'ın DOĞRUDAN çocuğu, SafeAreaRoot'un değil.</b> Tam ekran
/// karartma katmanları güvenli alanın dışında kalır (CLAUDE.md SafeArea
/// kararı) ve panel Canvas'ın SON çocuğu olduğu için her şeyin üstünde
/// çizilir — Game sahnesinde sonuç kartının (LevelCompletePanel) da
/// üstünde.
///
/// <b>ÖLÇÜLÜ KUTLAMA.</b> Zemin sabit alfalı düz bir perde: tam ekran
/// parlama, hale ya da yanıp sönen katman YOK. Tek hareket kartın bir
/// seferlik ölçek tween'i ve süresi temadan geliyor
/// (<c>NexumTheme.motion.durationMedium</c>), yeni bir sabit girmiyor.
/// Kapanış oyuncunun dokunuşuyla: perdenin kendisi butondur, karttaki
/// hiçbir grafik raycast yakalamaz, yani ekranın her yeri kapatır.
///
/// Menüden: NEXUM → Rütbe Kutlamasını Kur
/// Batch:   -executeMethod RankUpPanelBuilder.Run
/// </summary>
public static class RankUpPanelBuilder
{
    private const string Tag = "NEXUM_RANKUP";

    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    private const string ParentPath = "Canvas";
    private const string PanelName = "RankUpPanel";
    private static string PanelPath { get { return ParentPath + "/" + PanelName; } }

    private const string CardFramePath = "Assets/Art/UI/panel_lg_normal.png";
    private const string BadgeFramePath = "Assets/Art/UI/badge_frame_normal.png";

    private static readonly Vector2 CardSize = new Vector2(760f, 620f);

    // Kart merkezine göre bantlar (kart yarı yüksekliği 310):
    //   başlık      275..185
    //   çerçeve     160..-40
    //   rütbe adı   -78..-142
    //   toplam XP   -158..-202
    //   ipucu       -230..-270
    private const float TitleY = 230f;
    private const float FrameY = 60f;
    private const float RankNameY = -110f;
    private const float XpY = -180f;
    private const float HintY = -250f;

    private static readonly Vector2 FrameSize = new Vector2(200f, 200f);

    /// <summary>
    /// Perde alfası. Sabit: nabız atmaz, parlamaz. Perdenin TONU temadan
    /// gelir (<c>palette.scrim</c>); kurucuya hex yazılmaz.
    /// </summary>
    private const float ScrimAlpha = 0.72f;

    [MenuItem("NEXUM/Rütbe Kutlamasını Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        BuildScene(MenuScenePath, dryRun);
        BuildScene(GameScenePath, dryRun);
    }

    private static void BuildScene(string scene, bool dryRun)
    {
        bool oyunSahnesi = scene == GameScenePath;
        UIBuilder b = UIBuilder.Begin(oyunSahnesi ? "RankUpGame" : "RankUpMenu", scene, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject panel = b.Ensure(ParentPath, PanelName);
        if (panel == null) { b.End(); return; }

        b.SetStretch(panel, UITokens.Zero, UITokens.One);
        b.EnsureComponent<Image>(panel, image =>
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            Color perde = theme.palette.scrim;
            perde.a = ScrimAlpha;
            image.color = perde;
            image.raycastTarget = true;     // dokunuşu perde yakalar
        });

        // Perdenin KENDİSİ buton: kartın hiçbir grafiği raycast yakalamıyor,
        // bu yüzden ekranın her yerine dokunmak kapatır. Ayrı bir "kapat"
        // butonu YOK — kutlama bir eylem değil, bir bildirim.
        Button closeButton = b.EnsureComponent<Button>(panel, button =>
        {
            button.transition = Selectable.Transition.None;
        });

        // --- kart
        GameObject card = b.Ensure(PanelPath, "Card");
        b.SetFixed(card, UITokens.Center, CardSize, Vector2.zero);
        b.EnsureComponent<Image>(card, image =>
        {
            image.sprite = LoadSprite(CardFramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = false;
        });

        // 50 punto 70 birimlik kutuda %90 dolduruyordu (ProgressionFitTests);
        // 90 birimde %67.
        GameObject title = Metin(b, theme, "TitleText", new Vector2(680f, 90f), TitleY,
                                 theme.typography.title, theme.palette.primaryBright);
        EnsureLocalized(b, title, CodeStrings.RankUpTitle);

        // --- yeni çerçeve: kitteki madalya çerçevesi, içinde rütbe sırası
        GameObject frame = b.Ensure(PanelPath + "/Card", "RankFrame");
        b.SetFixed(frame, UITokens.Center, FrameSize, new Vector2(0f, FrameY));
        b.EnsureComponent<Image>(frame, image =>
        {
            image.sprite = LoadSprite(BadgeFramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = false;
        });

        GameObject ordinal = b.Ensure(PanelPath + "/Card/RankFrame", "OrdinalText");
        b.SetFixed(ordinal, UITokens.Center, new Vector2(160f, 100f), Vector2.zero);
        b.EnsureComponent<TextMeshProUGUI>(ordinal, tmp => ApplyText(
            tmp, theme, theme.typography.value, theme.palette.warning));

        GameObject rankName = Metin(b, theme, "RankNameText", new Vector2(680f, 64f), RankNameY,
                                    theme.typography.subtitle, theme.palette.textPrimary);

        GameObject xp = Metin(b, theme, "XpText", new Vector2(680f, 44f), XpY,
                              theme.typography.label, theme.palette.textSecondary);

        GameObject hint = Metin(b, theme, "HintText", new Vector2(680f, 40f), HintY,
                                theme.typography.label, theme.palette.textMuted);
        EnsureLocalized(b, hint, CodeStrings.RankUpHint);

        // --- denetleyici
        RankUpPanel controller = b.EnsureComponent<RankUpPanel>(panel, rc =>
        {
            rc.card = card;
            rc.rankNameText = Tmp(rankName);
            rc.rankFrame = frame != null ? frame.GetComponent<Image>() : null;
            rc.xpTotalText = Tmp(xp);
            rc.rankOrdinalText = Tmp(ordinal);
            rc.appearDuration = theme.motion.durationMedium;
        });

        if (!dryRun)
        {
            if (closeButton != null && controller != null) Wire(closeButton, controller);

            // Game sahnesinde UIManager paneli tanımalı: girdi kilidi
            // (IsModalPanelOpen) ve sonuç kartından sonraki çağrı bu alanı
            // okuyor.
            if (oyunSahnesi)
            {
                UIManager ui = Object.FindObjectOfType<UIManager>(true);
                if (ui != null) SetReference(ui, "rankUpPanel", panel);
            }

            // Panel KAPALI başlar: kutlama yalnızca ShowIfPending ile açılır.
            if (panel.activeSelf) panel.SetActive(false);
        }

        Debug.Log($"{Tag}: {System.IO.Path.GetFileNameWithoutExtension(scene)} -> kart " +
                  $"{CardSize.x:F0}x{CardSize.y:F0}, perde alfa {ScrimAlpha:F2}, " +
                  $"tween {theme.motion.durationMedium:F2}s (temadan)");

        b.End();
    }

    // --------------------------------------------------------------- parçalar

    private static GameObject Metin(UIBuilder b, NexumTheme theme, string ad, Vector2 boyut,
                                    float y, NexumTheme.TypeStyle stil, Color renk)
    {
        GameObject go = b.Ensure(PanelPath + "/Card", ad);
        b.SetFixed(go, UITokens.Center, boyut, new Vector2(0f, y));
        b.EnsureComponent<TextMeshProUGUI>(go, tmp => ApplyText(tmp, theme, stil, renk));
        return go;
    }

    private static void ApplyText(TextMeshProUGUI tmp, NexumTheme theme,
                                  NexumTheme.TypeStyle style, Color color)
    {
        tmp.font = style.font;
        tmp.fontSize = style.size;
        tmp.enableAutoSizing = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
    }

    private static void EnsureLocalized(UIBuilder b, GameObject go, string key)
    {
        if (go == null) return;
        b.EnsureComponent<LocalizedText>(go, loc => loc.key = key);
    }

    private static TextMeshProUGUI Tmp(GameObject go)
    {
        return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void Wire(Button button, RankUpPanel target)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == "Close") return;
        }

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            button.onClick, new UnityEngine.Events.UnityAction(target.Close));
        EditorUtility.SetDirty(button);
    }

    private static void SetReference(Object owner, string field, Object value)
    {
        SerializedObject so = new SerializedObject(owner);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue == value) return;

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(owner);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
