using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ansiklopedi AYRINTI panelini oyun sahnesine de kurar.
///
/// <b>Neden gerekli (PANEL ERİŞİMİ kuralı).</b> Tarif defteri artık duraklat
/// menüsünden açılıyor ve defterdeki bir satıra basmak
/// <c>EncyclopediaManager.OpenDetailPanel</c> çağırıyor. Ama ansiklopedi
/// YALNIZCA <c>MainMenu.unity</c>'de kuruluydu; oyunda
/// <c>RecipeBookPanel.encyclopedia</c> null kalıyor ve satıra basınca
/// SESSİZCE hiçbir şey olmuyordu. Tarif defteri hatasının aynısının bir
/// adım ilerisi.
///
/// Tam ansiklopedi listesi oyuna kurulmaz — orada gereken yalnızca ayrıntı
/// kartı. <c>OpenDetailPanel</c> beş serileştirilmiş alandan başka hiçbir
/// şeye dokunmuyor (ölçüldü), bu yüzden kart tek başına çalışıyor.
///
/// Menüden: NEXUM → Ansiklopedi Kartını Oyuna Kur
/// Batch:   -executeMethod EncyclopediaDetailBuilder.Run
/// </summary>
public static class EncyclopediaDetailBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string ParentPath = "Canvas/SafeAreaRoot";
    private const string PanelName = "ElementDetailPanel";

    private const string FramePath = "Assets/Art/UI/panel_md_normal.png";
    private const string ClosePath = "Assets/Art/Figures/x.png";

    private static readonly Vector2 PanelSize = new Vector2(800f, 1285f);
    private static readonly Vector2 CloseSize = new Vector2(100f, 100f);

    private static string PanelPath { get { return ParentPath + "/" + PanelName; } }

    [MenuItem("NEXUM/Ansiklopedi Kartını Oyuna Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("EncyclopediaDetailGame", ScenePath, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject panel = b.Ensure(ParentPath, PanelName);
        if (panel == null) { b.End(); return; }

        b.SetFixed(panel, UITokens.Center, PanelSize, Vector2.zero);
        b.EnsureComponent<Image>(panel, image =>
        {
            image.sprite = LoadSprite(FramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = true;
        });

        // Ana menüdeki kartla AYNI ritim (ölçülen değerler)
        GameObject name = Metin(b, theme, "NameText", new Vector2(613f, 155f), 530f,
                                theme.typography.title);
        GameObject score = Metin(b, theme, "ScoreText", new Vector2(622f, 100f), 345f,
                                 theme.typography.body);
        GameObject formula = Metin(b, theme, "FormulaText", new Vector2(622f, 100f), 245f,
                                   theme.typography.body);
        GameObject desc = Metin(b, theme, "DescriptionText", new Vector2(539f, 660f), -188f,
                                theme.typography.body);

        GameObject close = b.Ensure(PanelPath, "CloseButton");
        b.SetFixed(close, UITokens.Center, CloseSize, new Vector2(359f, 625f));
        b.EnsureComponent<Image>(close, image =>
        {
            image.sprite = LoadSprite(ClosePath);
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
        });
        b.EnsureComponent<Button>(close, button => button.transition = Selectable.Transition.ColorTint);

        // Ayrıntı kartını süren yönetici: oyun sahnesinde yalnızca KART için
        b.EnsureComponent<EncyclopediaManager>(panel, mgr =>
        {
            mgr.detailPanel = panel;
            mgr.detailNameText = name != null ? name.GetComponent<TextMeshProUGUI>() : null;
            mgr.detailScoreText = score != null ? score.GetComponent<TextMeshProUGUI>() : null;
            mgr.detailRecipeText = formula != null ? formula.GetComponent<TextMeshProUGUI>() : null;
            mgr.detailDescText = desc != null ? desc.GetComponent<TextMeshProUGUI>() : null;
        });

        if (!dryRun)
        {
            EncyclopediaManager mgr = panel.GetComponent<EncyclopediaManager>();
            Button closeButton = close != null ? close.GetComponent<Button>() : null;
            if (mgr != null && closeButton != null) Wire(closeButton, mgr, mgr.CloseDetailPanel);

            // Defterin ayrıntı kartı referansı
            RecipeBookPanel defter = Object.FindObjectsOfType<RecipeBookPanel>(true)
                .FirstOrDefault();
            if (defter != null && defter.encyclopedia != mgr)
            {
                SerializedObject so = new SerializedObject(defter);
                SerializedProperty prop = so.FindProperty("encyclopedia");
                if (prop != null)
                {
                    prop.objectReferenceValue = mgr;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(defter);
                }
            }

            // UIManager'a bağla (girdi kilidi ve geri tuşu zinciri için)
            UIManager ui = Object.FindObjectOfType<UIManager>(true);
            if (ui != null)
            {
                SerializedObject so = new SerializedObject(ui);
                SerializedProperty prop = so.FindProperty("elementDetailPanel");
                if (prop != null && prop.objectReferenceValue != panel)
                {
                    prop.objectReferenceValue = panel;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(ui);
                }
            }

            if (panel.activeSelf) panel.SetActive(false);
        }

        b.End();
    }

    private static GameObject Metin(UIBuilder b, NexumTheme theme, string ad,
                                    Vector2 boyut, float y, NexumTheme.TypeStyle stil)
    {
        GameObject go = b.Ensure(PanelPath, ad);
        b.SetFixed(go, UITokens.Center, boyut, new Vector2(0f, y));
        b.EnsureComponent<TextMeshProUGUI>(go, tmp =>
        {
            tmp.font = stil.font;
            tmp.fontSize = stil.size;
            tmp.color = theme.palette.textPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        });
        return go;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError($"{UIBuilder.Tag}[EncyclopediaDetailGame]: sprite yok -> {path}");
        return s;
    }

    private static void Wire(Button button, Object target, UnityEngine.Events.UnityAction action)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
        }
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }
}
