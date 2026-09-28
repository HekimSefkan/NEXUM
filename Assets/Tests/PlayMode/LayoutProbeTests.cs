using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Yerleşim ölçüm aracı: sahnedeki öğelerin ÇALIŞMA ZAMANI dikdörtgenlerini
// referans (1080x1920) birimine çevirip loglar. Sahnedeki kayıtlı değerler
// TMP metinleri ve layout grupları yüzünden yanıltabildiği için ölçüm şart.
//
// Çıktı satırları: NEXUM_LAYOUT: <ekran> | <yol> | x0..x1 y0..y1 (gen x yuk) [ek bilgi]
public class LayoutProbeTests
{
    private const string Tag = "NEXUM_LAYOUT";

    private static Canvas canvas;

    private static void LogRect(string screen, string label, RectTransform rect, string extra = "")
    {
        if (rect == null)
        {
            Debug.Log($"{Tag}: {screen} | {label} | YOK");
            return;
        }

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        // Overlay canvas'ta dünya köşeleri piksel cinsindedir; referans birimine çevir
        float s = canvas != null ? canvas.scaleFactor : 1f;
        float halfW = Screen.width / (2f * s);
        float halfH = Screen.height / (2f * s);

        float x0 = corners[0].x / s - halfW;
        float x1 = corners[2].x / s - halfW;
        float y0 = corners[0].y / s - halfH;
        float y1 = corners[1].y / s - halfH;

        Debug.Log($"{Tag}: {screen} | {label} | x {x0:F1}..{x1:F1} y {y0:F1}..{y1:F1} " +
                  $"({x1 - x0:F1} x {y1 - y0:F1}) {extra}");
    }

    private static string TmpInfo(GameObject go)
    {
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) return "";
        tmp.ForceMeshUpdate();
        return $"[TMP satir={tmp.textInfo.lineCount} tercih={tmp.preferredWidth:F0}x{tmp.preferredHeight:F0} " +
               $"punto={tmp.fontSize:F0} \"{tmp.text.Replace("\n", " ")}\"]";
    }

    private static Transform Find(string path)
    {
        Transform root = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        return root == null ? null : root.Find(path);
    }

    private static void DumpSubtree(string screen, Transform root, int maxDepth)
    {
        if (root == null) return;
        Walk(screen, root, "", 0, maxDepth);
    }

    private static void Walk(string screen, Transform t, string prefix, int depth, int maxDepth)
    {
        string path = prefix.Length == 0 ? t.name : prefix + "/" + t.name;
        LogRect(screen, path, t as RectTransform, TmpInfo(t.gameObject));
        if (depth >= maxDepth) return;
        for (int i = 0; i < t.childCount; i++)
        {
            if (t.GetChild(i).gameObject.activeInHierarchy) Walk(screen, t.GetChild(i), path, depth + 1, maxDepth);
        }
    }

    private static void CacheCanvas()
    {
        canvas = Object.FindObjectOfType<Canvas>();
    }

    private static void Call(MonoBehaviour target, string method)
    {
        MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
        if (m != null) m.Invoke(target, null);
    }

    [UnityTest]
    public IEnumerator MainMenuPanelleriniOlc()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);
        CacheCanvas();

        string screen = $"{Screen.width}x{Screen.height}";
        Debug.Log($"{Tag}_SCREEN: {screen} scaleFactor={canvas.scaleFactor:F3}");

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");

        Call(menu, "OpenHowToPlay");
        yield return new WaitForSeconds(1f);
        DumpSubtree("HOWTOPLAY", Find("SafeAreaRoot/HowToPlayPanel"), 2);

        Call(menu, "BackToMainMenu");
        yield return new WaitForSeconds(1f);
        Call(menu, "OpenSettingsPanel");
        yield return new WaitForSeconds(1f);
        DumpSubtree("SETTINGS", Find("SafeAreaRoot/SettingsPanel"), 3);

        Assert.Pass();
    }

    [UnityTest]
    public IEnumerator OyunHudOlc()
    {
        // Level 1 (tek hedef) ve Level 4 (iki hedef)
        int savedLevel = PlayerPrefs.GetInt("SelectedLevel", 0);
        foreach (int level in new[] { 0, 3 })
        {
            PlayerPrefs.SetInt("SelectedLevel", level);
            PlayerPrefs.SetInt("TutorialRead_Level_" + level, 1);

            SceneManager.LoadScene("Game");
            yield return null;
            yield return new WaitForSeconds(1.2f);
            CacheCanvas();

            string screen = $"L{level + 1}";
            Debug.Log($"{Tag}_SCREEN: {Screen.width}x{Screen.height} scaleFactor={canvas.scaleFactor:F3} level={level + 1}");

            foreach (string name in new[] { "ScoreButton", "GoalsContainer", "logo", "PauseButton",
                                            "HintButton", "UndoButton", "JokerButton", "GridBoard" })
            {
                Transform t = Find("SafeAreaRoot/" + name);
                LogRect(screen, name, t as RectTransform, t != null ? TmpInfo(t.gameObject) : "");
                if (t != null && name == "GoalsContainer")
                {
                    for (int i = 0; i < t.childCount; i++)
                    {
                        Transform beaker = t.GetChild(i);
                        LogRect(screen, "GoalsContainer/" + beaker.name + i, beaker as RectTransform);
                        foreach (Transform sub in beaker)
                        {
                            LogRect(screen, "GoalsContainer/" + beaker.name + i + "/" + sub.name,
                                sub as RectTransform, TmpInfo(sub.gameObject));
                        }
                    }
                }
            }

            // Kombo yazısı: UIManager üzerinden gerçek konumda örnek oluştur
            MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>()
                .FirstOrDefault(m => m.GetType().Name == "UIManager");
            if (ui != null)
            {
                MethodInfo show = ui.GetType().GetMethod("ShowComboText", BindingFlags.Public | BindingFlags.Instance);
                Transform grid = Find("SafeAreaRoot/GridBoard");
                if (show != null && grid != null)
                {
                    Vector3 spawn = grid.GetChild(0) != null ? grid.GetChild(0).position : grid.position;
                    show.Invoke(ui, new object[] { 2, spawn });
                    yield return null;

                    Transform canvasT = canvas.transform;
                    for (int i = 0; i < canvasT.childCount; i++)
                    {
                        Transform c = canvasT.GetChild(i);
                        if (c.GetComponent<TextMeshProUGUI>() != null && c.name.Contains("Combo"))
                        {
                            LogRect(screen, "ComboText", c as RectTransform, TmpInfo(c.gameObject));
                        }
                    }
                }
            }
        }

        PlayerPrefs.SetInt("SelectedLevel", savedLevel);
        Assert.Pass();
    }
}
