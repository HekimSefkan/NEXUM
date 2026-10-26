using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyun HUD'unun sağ eylem sütunu ve ipucu karakteri.
///
/// <b>B2 — İPUCU BUTONU.</b> HUD'da ipucu için ayrı bir buton yoktu; ipucu
/// yalnızca karakterin kendisine basılarak açılıyordu, yani eylem
/// GERİ AL / PARÇALA ile aynı ailede değildi ve kalan hak hiçbir yerde
/// görünmüyordu. Artık üçü aynı aile: aynı ölçü, aynı kit çerçevesi, aynı
/// ritim. İpucu butonu ayrıca <see cref="HintBadge"/> rozetiyle kalan hakkı
/// gösterir ve hak bitince devre dışı görünür.
///
/// <b>B5 — KARAKTER ÇERÇEVESİ.</b> Karakter görseli (hint_assistant) kit
/// avatar çerçevesine oturdu ve sağ sütundan SOL tarafa alındı. Sebep ölçüm:
/// tahtanın üstü (290) ile güvenli alanın tepesi (960) arası 670 birim;
/// karakterin dokunma kapsamı 200 + üç eylem butonu 3×144 + duraklat 144 =
/// 776 birim, sütuna sığmıyordu. Karakter solda GoalsContainer ile skor
/// göstergesinin arasındaki boş alanda duruyor.
///
/// Rozet AYRI KATMAN: Faz 5'teki kademeli ipucu rozeti değiştirecek,
/// butonun kendisi değişmeyecek.
///
/// Menüden: NEXUM → Oyun HUD'unu Yerleştir
/// Batch:   -executeMethod GameHudBuilder.Run
/// </summary>
public static class GameHudBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string HudPath = "Canvas/SafeAreaRoot";

    private const string ButtonFramePath = "Assets/Art/UI/btn_ghost_normal.png";
    private const string BadgeFramePath = "Assets/Art/UI/badge_frame_normal.png";
    private const string AvatarFramePath = "Assets/Art/UI/avatar_frame_normal.png";
    private const string CharacterPath = "Assets/Art/Figures/hint_assistant.png";

    private static readonly Vector2 ActionSize = new Vector2(250f, 100f);
    private static readonly Vector2 BadgeSize = new Vector2(64f, 64f);
    private static readonly Vector2 AvatarSize = new Vector2(200f, 200f);

    /// <summary>
    /// Sağ sütun: dokunma kapsamları (144) 2 birim boşlukla dizili.
    /// İPUCU 378..522 · GERİ AL 524..668 · PARÇALA 670..814 · DURAKLAT 816..960
    /// </summary>
    private const float TipY = 450f;
    private const float UndoY = 596f;
    private const float JokerY = 742f;
    private const float ColumnX = 400f;

    /// <summary>Karakter: solda, GoalsContainer ile skor göstergesinin arasında.</summary>
    // y 450 -> 400: 200 birimlik karakter 350..550'ye oturuyordu ve
    // GoalsContainer'ın alt kenarıyla (511) 39 birim çakışıyordu. Beherler
    // çalışma zamanında doğduğu için edit mode render'ında görünmüyor,
    // cihazda görünürdü. Artık 300..500, iki yana da 10 birim boşluk.
    private static readonly Vector2 AvatarCenter = new Vector2(-380f, 400f);

    /// <summary>
    /// HUD ANCHOR'I TAHTAYLA AYNI (CLAUDE.md kararı): grid'le görsel grup
    /// oluşturan her şey GridBoard'un anchor'ında durur, ekran merkezinde
    /// değil — uzun ekranlarda birlikte kaysınlar.
    /// </summary>
    private static readonly Vector2 GridAnchor = new Vector2(0.5f, 0.41666666f);

    /// <summary>Yerel merkez -> bu anchor'daki anchoredPosition (1080x1920).</summary>
    private static Vector2 Pos(Vector2 yerelMerkez)
    {
        return new Vector2(yerelMerkez.x, yerelMerkez.y - (GridAnchor.y * 1920f - 960f));
    }

    [MenuItem("NEXUM/Oyun HUD'unu Yerleştir")]
    public static void RunFromMenu()
    {
        Build(false);
    }

    public static void Run()
    {
        Build(UIBuilder.DryRunRequested);
    }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("GameHud", ScenePath, dryRun);
        NexumTheme theme = UITokens.Theme;

        // --- İPUCU: GERİ AL ve PARÇALA ile aynı aile
        GameObject tip = b.Ensure(HudPath, "TipButton");
        if (tip == null) { b.End(); return; }

        b.SetFixed(tip, GridAnchor, ActionSize, Pos(new Vector2(ColumnX, TipY)));
        b.EnsureComponent<Image>(tip, image =>
        {
            image.sprite = LoadSprite(ButtonFramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = true;
        });
        b.EnsureComponent<Button>(tip, button => button.transition = Selectable.Transition.ColorTint);

        GameObject tipLabel = b.Ensure(HudPath + "/TipButton", "Label");
        b.SetStretch(tipLabel, UITokens.Zero, UITokens.One);
        b.EnsureComponent<TextMeshProUGUI>(tipLabel, tmp =>
        {
            tmp.font = theme.typography.label.font;
            tmp.fontSize = theme.typography.label.size;
            tmp.color = theme.palette.textPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
        });
        EnsureLocalized(b, tipLabel, CodeStrings.HudTipButton);

        // --- Rozet: AYRI KATMAN (Faz 5 kademeli ipucu bunu değiştirecek)
        GameObject badge = b.Ensure(HudPath + "/TipButton", "HintBadge");
        b.SetFixed(badge, UITokens.One, BadgeSize, new Vector2(-8f, -8f));
        b.EnsureComponent<Image>(badge, image =>
        {
            image.sprite = LoadSprite(BadgeFramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = false;
        });

        GameObject badgeText = b.Ensure(HudPath + "/TipButton/HintBadge", "Count");
        b.SetStretch(badgeText, UITokens.Zero, UITokens.One);
        b.EnsureComponent<TextMeshProUGUI>(badgeText, tmp =>
        {
            tmp.font = theme.typography.label.font;
            tmp.fontSize = theme.typography.label.size * 0.8f;
            tmp.color = theme.palette.textPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            tmp.text = "3";
        });

        // --- Sütunun geri kalanı aynı ritme oturur (ölçüler DEĞİŞMEZ)
        GameObject undo = b.Ensure(HudPath, "UndoButton", adopt: true);
        if (undo != null) b.SetFixed(undo, GridAnchor, ActionSize, Pos(new Vector2(ColumnX, UndoY)));

        GameObject joker = b.Ensure(HudPath, "JokerButton", adopt: true);
        if (joker != null) b.SetFixed(joker, GridAnchor, ActionSize, Pos(new Vector2(ColumnX, JokerY)));

        // --- Karakter: kit avatar çerçevesine oturdu, sola alındı
        GameObject hint = b.Ensure(HudPath, "HintButton", adopt: true);
        if (hint != null)
        {
            b.SetFixed(hint, GridAnchor, AvatarSize, Pos(AvatarCenter));
            b.EnsureComponent<Image>(hint, image =>
            {
                image.sprite = LoadSprite(AvatarFramePath);
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.color = Color.white;
                image.preserveAspect = false;
                image.raycastTarget = true;
            });

            GameObject portre = b.Ensure(HudPath + "/HintButton", "Portrait");
            b.SetStretch(portre, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            b.EnsureComponent<Image>(portre, image =>
            {
                image.sprite = LoadSprite(CharacterPath);
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
            });
        }

        // Rozet ve devre dışı durumu çalışma zamanında
        b.EnsureComponent<HintButtonState>(tip, state =>
        {
            state.badge = badge;
            state.badgeText = badgeText != null ? badgeText.GetComponent<TextMeshProUGUI>() : null;
        });

        if (dryRun) { b.End(); return; }

        b.End();
        Wire();
    }

    /// <summary>İPUCU butonunu mevcut <c>GridManager.RequestHint</c>'e bağlar.</summary>
    private static void Wire()
    {
        GridManager grid = Object.FindObjectOfType<GridManager>(true);
        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        GameObject tip = GameObject.Find("Canvas/SafeAreaRoot/TipButton");
        if (tip == null) return;

        Button button = tip.GetComponent<Button>();
        if (button == null || grid == null) return;

        // Mevcut bağlantıları temizleyip yeniden kur: idempotans
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
        }
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, grid.RequestHint);
        if (ui != null)
        {
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, ui.PlayButtonSound);
        }

        EditorUtility.SetDirty(button);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError($"{UIBuilder.Tag}[GameHud]: sprite yok -> {path}");
        return s;
    }

    private static void EnsureLocalized(UIBuilder b, GameObject go, string key)
    {
        if (go == null) return;

        LocalizedText loc = go.GetComponent<LocalizedText>();
        if (loc == null)
        {
            b.EnsureComponent<LocalizedText>(go, l => l.key = key);
            return;
        }
        b.Apply(go, loc, l => l.key = key);
    }
}
