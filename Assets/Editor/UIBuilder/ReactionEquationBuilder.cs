using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tepkime denklemi gösterimini kurar: Game sahnesinde bir havuz kökü ve
/// altında <see cref="ReactionEquationDisplay.PoolSize"/> adet hazır TMP metni.
///
/// Havuz sahnede önceden kurulduğu için oyun sırasında Instantiate/Destroy
/// yapılmaz.
///
/// Menüden: NEXUM → Tepkime Denklemini Kur
/// Batch mode: -executeMethod ReactionEquationBuilder.Run   (rapor için -dryRun)
/// </summary>
public static class ReactionEquationBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string ParentPath = "Canvas/SafeAreaRoot";
    private const string RootName = "ReactionEquations";
    private const string LabelPrefix = "Equation";

    // Denklem kutusu: iki sembol + ok sığacak kadar geniş, tek satır
    private static readonly Vector2 LabelSize = new Vector2(440f, 54f);

    [MenuItem("NEXUM/Tepkime Denklemini Kur")]
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
        UIBuilder b = UIBuilder.Begin("ReactionEquation", ScenePath, dryRun);

        GameObject root = b.Ensure(ParentPath, RootName);
        if (root == null)
        {
            b.End();
            return;
        }

        // Havuz kökü tüm grid alanını kaplar; kutular konumlarını koddan alır
        b.SetStretch(root, UITokens.Zero, UITokens.One);

        NexumTheme theme = UITokens.Theme;

        b.EnsureComponent<ReactionEquationDisplay>(root, display =>
        {
            display.theme = theme;
        });

        string rootPath = ParentPath + "/" + RootName;

        for (int i = 0; i < ReactionEquationDisplay.PoolSize; i++)
        {
            GameObject label = b.Ensure(rootPath, LabelPrefix + i);
            if (label == null) continue;

            b.SetFixed(label, UITokens.Center, LabelSize);

            b.EnsureComponent<TextMeshProUGUI>(label, tmp =>
            {
                tmp.text = "";
                tmp.font = theme.typography.body.font;
                tmp.fontSize = theme.typography.body.size;
                tmp.enableAutoSizing = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.raycastTarget = false;
                tmp.color = theme.palette.textPrimary;
            });

            if (!dryRun && label.activeSelf) label.SetActive(false);
        }

        b.End();
    }
}
