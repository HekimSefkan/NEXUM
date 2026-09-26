using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Modal panel açıkken girdinin GridManager'a ulaşmadığını doğrular (CA).
// Oyun scriptleri Assembly-CSharp'ta olduğu için erişim reflection ile yapılır.
// Çalıştırma:
//   Unity.exe -batchmode -runTests -projectPath <proje> -testPlatform PlayMode -testResults <xml> -logFile <log>
public class InputLockTests
{
    private const string Tag = "NEXUM_INPUT_TEST";

    // Girdiyi engellemesi gereken paneller (UIManager alan adları)
    private static readonly string[] ModalPanels =
    {
        "tutorialPanel", "pausePanel", "quizPanel", "gameOverPanel",
        "winPanel", "hypothesisPanel", "undoPanel"
    };

    // Oyunu durdurmayan paneller: girdi engellenmemeli
    private static readonly string[] NonModalPanels = { "assistantPanel", "focusPanel" };

    private MonoBehaviour gameManager;
    private MonoBehaviour uiManager;
    private MonoBehaviour gridManager;
    private MethodInfo isInputBlocked;

    private readonly Dictionary<string, int> savedPrefs = new Dictionary<string, int>();
    private readonly List<GameObject> openedByTest = new List<GameObject>();

    private void SavePref(string key)
    {
        savedPrefs[key] = PlayerPrefs.GetInt(key, -1);
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Tutorial panelini atla; eski değerler TearDown'da geri yazılır
        SavePref("SelectedLevel");
        SavePref("TutorialRead_Level_0");
        PlayerPrefs.SetInt("SelectedLevel", 0);
        PlayerPrefs.SetInt("TutorialRead_Level_0", 1);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
        gameManager = all.FirstOrDefault(m => m.GetType().Name == "GameManager");
        uiManager = all.FirstOrDefault(m => m.GetType().Name == "UIManager");
        gridManager = all.FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(gameManager, "GameManager bulunamadı");
        Assert.IsNotNull(uiManager, "UIManager bulunamadı");
        Assert.IsNotNull(gridManager, "GridManager bulunamadı");

        isInputBlocked = gameManager.GetType().GetMethod("IsInputBlocked",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(isInputBlocked, "GameManager.IsInputBlocked yok");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in openedByTest)
        {
            if (go != null) go.SetActive(false);
        }
        openedByTest.Clear();

        foreach (KeyValuePair<string, int> pair in savedPrefs)
        {
            if (pair.Value < 0) PlayerPrefs.DeleteKey(pair.Key);
            else PlayerPrefs.SetInt(pair.Key, pair.Value);
        }
        PlayerPrefs.Save();
    }

    private bool Blocked()
    {
        return (bool)isInputBlocked.Invoke(gameManager, null);
    }

    private GameObject Panel(string fieldName)
    {
        FieldInfo f = uiManager.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, "UIManager." + fieldName + " alanı yok");
        GameObject go = f.GetValue(uiManager) as GameObject;
        Assert.IsNotNull(go, "UIManager." + fieldName + " atanmamış");
        return go;
    }

    private void Open(GameObject panel)
    {
        panel.SetActive(true);
        openedByTest.Add(panel);
    }

    [UnityTest]
    public IEnumerator ModalPanelAcikkenGirdiEngellenir()
    {
        // Panel yokken girdi serbest olmalı
        foreach (string name in ModalPanels.Concat(NonModalPanels)) Panel(name).SetActive(false);
        yield return null;
        Assert.IsFalse(Blocked(), "Hiçbir panel açık değilken girdi engellenmemeli");
        Debug.Log(Tag + ": panel yok -> engellendi=False (beklenen False)");

        foreach (string name in ModalPanels)
        {
            GameObject panel = Panel(name);
            Open(panel);
            yield return null;
            bool blocked = Blocked();
            Debug.Log(string.Format("{0}: {1} acik -> engellendi={2} (beklenen True)", Tag, name, blocked));
            Assert.IsTrue(blocked, name + " açıkken girdi engellenmeli");

            panel.SetActive(false);
            yield return null;
            Assert.IsFalse(Blocked(), name + " kapandıktan sonra girdi yeniden serbest olmalı");
        }

        foreach (string name in NonModalPanels)
        {
            GameObject panel = Panel(name);
            Open(panel);
            yield return null;
            bool blocked = Blocked();
            Debug.Log(string.Format("{0}: {1} acik -> engellendi={2} (beklenen False)", Tag, name, blocked));
            Assert.IsFalse(blocked, name + " oyunu durdurmaz, girdi engellenmemeli");
            panel.SetActive(false);
        }
    }

    [UnityTest]
    public IEnumerator GridHazirDegilkenGirdiEngellenir()
    {
        // Tutorial sırasında GridManager devre dışı bırakılıyor
        gridManager.enabled = false;
        yield return null;
        bool blocked = Blocked();
        Debug.Log(string.Format("{0}: GridManager.enabled=false -> engellendi={1} (beklenen True)", Tag, blocked));
        Assert.IsTrue(blocked, "GridManager devre dışıyken girdi engellenmeli");

        gridManager.enabled = true;
        yield return null;
        Assert.IsFalse(Blocked(), "GridManager etkinleşince girdi serbest olmalı");
    }
}
