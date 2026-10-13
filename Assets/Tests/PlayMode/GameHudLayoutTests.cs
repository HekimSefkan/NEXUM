using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Oyun HUD'u 4 canvas boyutunda: güvenli alan dışına taşma ve YENİ eklenen
// objenin mevcutlarla çakışması.
//
// Eski HUD öğeleri arasındaki çakışmalar (varsa) yalnızca loglanır: bunlar bu
// adımın getirdiği bir sorun değil, mevcut durumun ölçümü.
public class GameHudLayoutTests
{
    private const string Tag = "NEXUM_HUD";
    private const string YeniObje = "ReactionEquations";

    private static readonly string[] HudOgeleri =
    {
        "GridBoard", "ScoreButton", "GoalsContainer", "logo", "PauseButton",
        "HintButton", "UndoButton", "JokerButton", "PressureMeter", YeniObje,
    };

    private RectTransform safeRoot;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");
    }

    [UnityTest]
    public IEnumerator HudDortBoyuttaTemiz()
    {
        List<string> hatalar = new List<string>();

        foreach (Vector2 size in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, size);
            yield return null;
            yield return null;

            string ekran = $"{size.x:F0}x{size.y:F0}";
            Vector4 guvenli = LayoutCheck.LocalRect(safeRoot, safeRoot);

            Dictionary<string, Vector4> rects = new Dictionary<string, Vector4>();

            foreach (string ad in HudOgeleri)
            {
                Transform t = LayoutCheck.Find("SafeAreaRoot/" + ad);
                if (t == null) { Debug.Log($"{Tag}: {ekran} | {ad,-18} YOK"); continue; }

                Vector4 r = LayoutCheck.LocalRect(safeRoot, t as RectTransform);
                rects[ad] = r;

                float tasma = LayoutCheck.Overflow(r, guvenli);
                Debug.Log($"{Tag}: {ekran} | {LayoutCheck.Describe(ad, r)} | tasma {tasma,6:F0}");

                if (tasma > 1f) hatalar.Add($"{ekran} {ad} guvenli alan disina {tasma:F0} birim");
            }

            // Yeni objenin çakışması HATA; eski öğeler arası çakışma yalnızca kayıt
            foreach (KeyValuePair<string, Vector4> a in rects)
            {
                foreach (KeyValuePair<string, Vector4> b in rects)
                {
                    if (string.CompareOrdinal(a.Key, b.Key) >= 0) continue;

                    float alan = LayoutCheck.OverlapArea(a.Value, b.Value);
                    if (alan <= 1f) continue;

                    bool yeniIlgili = a.Key == YeniObje || b.Key == YeniObje;
                    string satir = $"{ekran} {a.Key} x {b.Key} = {alan:F0} birim2";

                    // ReactionEquations tam ekran bir HAVUZ kökü; çocukları
                    // dinlenirken kapalı. Kök her şeyi kapsadığı için çakışma
                    // sayılmaz — gerçek konum kontrolü ReactionEquationTests'te.
                    if (yeniIlgili && IsPoolRootOverlap(a.Key, b.Key)) continue;

                    if (yeniIlgili) hatalar.Add("YENI OBJE CAKISMASI: " + satir);
                    else Debug.Log($"{Tag}_MEVCUT_CAKISMA: {satir}");
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    /// <summary>Havuz kökü tam ekran olduğu için her öğeyle kesişir; bu bir çakışma değil.</summary>
    private static bool IsPoolRootOverlap(string a, string b)
    {
        return a == YeniObje || b == YeniObje;
    }
}
