using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Geçersiz birleştirme mesajının bandı (Faz 1 / 1.3).
///
/// <b>NEDEN BU BANT.</b> Tahtanın altındaki şerit üç şey tarafından zaten
/// paylaşılıyor ve ikisi daha önce çakıştı. Yerleşim ÖLÇÜLDÜ (1080x1920
/// cihaz görüntüsünde boyalı satır taraması):
///
///   tahta alt kenarı            -610
///   BOŞ                         -610 .. -675   &lt;-- mesaj buraya
///   katalizör etiketi           -680 .. -710
///   katalizör tüpü              -730 .. -815
///   sistem bildirimi (merkez)   -850   (UIManager.SystemBelowBoard = 240)
///
/// Yani tahtanın hemen altındaki 65 birimlik şerit, üç bandın hiçbirine
/// ait değil. Mesaj TEK SATIR olmak zorunda: iki satır ancak katalizörün
/// ya da sistem bildiriminin bandına girerek sığardı.
///
/// Çakışmasızlık <c>InvalidMergeFeedbackTests</c> içinde GERÇEK RENDER'dan
/// ölçülerek doğrulanır (rect ölçmek yanıltır: katalizörün dekoratif
/// çerçevesinin rect'i çiziminden çok daha büyük).
///
/// Menüden: NEXUM → Geçersiz Birleştirme Bandını Kur
/// Batch:   -executeMethod InvalidMergeBuilder.Run
/// </summary>
public static class InvalidMergeBuilder
{
    private const string Tag = "NEXUM_GECERSIZ";

    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string HudPath = "Canvas/SafeAreaRoot";
    private const string PanelName = "InvalidMergePanel";

    /// <summary>Tahtayla aynı anchor: uzun ekranlarda birlikte kayarlar.</summary>
    private static readonly Vector2 GridAnchor = new Vector2(0.5f, 0.41666666f);

    /// <summary>Bant: tahtanın alt kenarı (-610) ile katalizör etiketi (-675) arası.</summary>
    private static readonly Vector2 BandSize = new Vector2(1000f, 58f);
    private static readonly float BandCenterY = -642f;

    private static Vector2 Pos(Vector2 yerelMerkez)
    {
        return new Vector2(yerelMerkez.x, yerelMerkez.y - (GridAnchor.y * 1920f - 960f));
    }

    [MenuItem("NEXUM/Geçersiz Birleştirme Bandını Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("InvalidMerge", ScenePath, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject panel = b.Ensure(HudPath, PanelName);
        if (panel == null) { b.End(); return; }

        b.SetFixed(panel, GridAnchor, BandSize, Pos(new Vector2(0f, BandCenterY)));
        b.EnsureComponent<CanvasGroup>(panel, g =>
        {
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;    // mesaj tahtayı engellemez
        });

        GameObject satir = b.Ensure(HudPath + "/" + PanelName, "Line");
        b.SetStretch(satir, UITokens.Zero, UITokens.One);
        b.EnsureComponent<TextMeshProUGUI>(satir, tmp =>
        {
            tmp.font = theme.typography.body.font;
            tmp.fontSize = theme.typography.body.size;
            tmp.color = theme.palette.textSecondary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;
        });

        b.EnsureComponent<InvalidMergeFeedback>(panel, f =>
        {
            f.lineText = satir != null ? satir.GetComponent<TextMeshProUGUI>() : null;
            f.theme = theme;
            f.group = panel.GetComponent<CanvasGroup>();
        });

        if (!dryRun)
        {
            // Sprite asset'i edit mode'da da bağlı olsun (cihaz görüntüsü için)
            TextMeshProUGUI tmp = satir != null ? satir.GetComponent<TextMeshProUGUI>() : null;
            if (tmp != null)
            {
                TMP_SpriteAsset sprite = Resources.Load<TMP_SpriteAsset>(ReactionText.SpriteAssetResource);
                if (sprite != null && tmp.spriteAsset != sprite)
                {
                    tmp.spriteAsset = sprite;
                    EditorUtility.SetDirty(tmp);
                }
            }
        }

        Debug.Log($"{Tag}: bant {BandSize.x:F0}x{BandSize.y:F0} merkez y={BandCenterY:F0} " +
                  $"({BandCenterY + BandSize.y * 0.5f:F0} .. {BandCenterY - BandSize.y * 0.5f:F0}); " +
                  $"tahta alt kenarı -610, katalizör etiketi -675'ten aşağıda");

        b.End();
    }
}
