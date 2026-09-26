using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// MainMenu'deki kaydırmalı panellerin (Profil, Ansiklopedi) art arda açılıp kapanmasında
// içerik boyutunun ve kart sayısının değişmediğini, kaydırmanın en üstten başladığını doğrular.
// Çalıştırma:
//   Unity.exe -batchmode -runTests -projectPath <proje> -testPlatform PlayMode -testResults <xml> -logFile <log>
public class PanelScrollTests
{
    private const string Tag = "NEXUM_PANEL_TEST";
    private const int Cycles = 5;
    private const float SwitchWait = 0.9f;   // SwitchPanel: 0.3 kapanma + 0.3 açılma tween'i

    private MonoBehaviour menu;
    private Transform canvasRoot;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);   // Start'lar ve kart üretimi

        menu = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager bulunamadı");

        canvasRoot = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvasRoot, "Canvas bulunamadı");
    }

    private void Call(string method)
    {
        MethodInfo m = menu.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, method + " metodu yok");
        m.Invoke(menu, null);
    }

    private ScrollRect FindScroll(string panelPath)
    {
        Transform t = canvasRoot.Find(panelPath);       // Find kapalı objelerde de çalışır
        Assert.IsNotNull(t, panelPath + " bulunamadı");
        ScrollRect scroll = t.GetComponentInChildren<ScrollRect>(true);
        Assert.IsNotNull(scroll, panelPath + " altında ScrollRect yok");
        return scroll;
    }

    private IEnumerator RunCycles(string panelPath, string openMethod, List<string> rows, List<float> heights,
                                  List<int> childCounts, List<float> topOffsets)
    {
        for (int i = 1; i <= Cycles; i++)
        {
            Call(openMethod);
            yield return new WaitForSeconds(SwitchWait);

            ScrollRect scroll = FindScroll(panelPath);
            RectTransform content = scroll.content;
            float height = content.rect.height;
            float posY = content.anchoredPosition.y;
            int count = content.childCount;

            heights.Add(height);
            childCounts.Add(count);
            topOffsets.Add(posY);
            rows.Add(string.Format("{0} #{1}: içerik yüksekliği={2:F1} anchoredPos.y={3:F1} normalizedPos={4:F3} çocuk={5} viewport={6:F1}",
                panelPath.Split('/').Last(), i, height, posY, scroll.verticalNormalizedPosition, count, scroll.viewport.rect.height));

            Call("BackToMainMenu");
            yield return new WaitForSeconds(SwitchWait);
        }
    }

    [UnityTest]
    public IEnumerator Paneller_BesKezAcilipKapaninca_IcerikSabitKalirVeUsttenBaslar()
    {
        var rows = new List<string>();
        var profileH = new List<float>(); var profileC = new List<int>(); var profileTop = new List<float>();
        var encyH = new List<float>(); var encyC = new List<int>(); var encyTop = new List<float>();

        yield return RunCycles("SafeAreaRoot/ProfilePanel", "OpenProfile", rows, profileH, profileC, profileTop);
        yield return RunCycles("SafeAreaRoot/EncyclopediaPanel", "OpenEncyclopedia", rows, encyH, encyC, encyTop);

        foreach (string row in rows) Debug.Log(Tag + ": " + row);
        Debug.Log(string.Format("{0}_SUMMARY: profil yükseklikleri=[{1}] kart=[{2}] | ansiklopedi yükseklikleri=[{3}] kart=[{4}]",
            Tag, string.Join(", ", profileH.Select(h => h.ToString("F1"))), string.Join(", ", profileC),
            string.Join(", ", encyH.Select(h => h.ToString("F1"))), string.Join(", ", encyC)));

        AssertStable("Profil", profileH, profileC, profileTop);
        AssertStable("Ansiklopedi", encyH, encyC, encyTop);
    }

    private void AssertStable(string label, List<float> heights, List<int> counts, List<float> topOffsets)
    {
        for (int i = 1; i < heights.Count; i++)
        {
            Assert.AreEqual(heights[0], heights[i], 1f,
                string.Format("{0}: {1}. açılışta içerik yüksekliği değişti ({2:F1} -> {3:F1})", label, i + 1, heights[0], heights[i]));
            Assert.AreEqual(counts[0], counts[i],
                string.Format("{0}: {1}. açılışta çocuk sayısı değişti", label, i + 1));
        }

        for (int i = 0; i < topOffsets.Count; i++)
        {
            Assert.AreEqual(0f, topOffsets[i], 1f,
                string.Format("{0}: {1}. açılışta kaydırma en üstte değil (anchoredPos.y={2:F1})", label, i + 1, topOffsets[i]));
        }
    }
}
