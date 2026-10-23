using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GERÇEK ekran görüntüsü: Unity'nin kendi render'ı, istenen canvas
/// boyutunda, batch mode'da.
///
/// NEDEN GEREKLİ: batch mode her zaman 640×480 yüzeyle açılır ve
/// <c>-screen-width</c> işlemez. <c>LayoutCheck</c> bu yüzden hedef boyutu
/// SafeAreaRoot'a DAYATIYOR — ama Canvas 640×480 kalıyor, yani
/// CanvasScaler'ın ölçeği ve canvas'a bağlı her hesap gerçek cihazdakinden
/// FARKLI çıkıyor. Burada Canvas bir kameraya bağlanıp kamera hedef boyutta
/// bir RenderTexture'a çizdiriliyor; böylece CanvasScaler cihazdaki gibi
/// çalışıyor ve gördüğümüz şey oyunda görülecek şey oluyor.
///
/// Sahneler DEĞİŞTİRİLMEZ: açılır, bellekte düzenlenir, render alınır,
/// kaydedilmeden kapatılır.
///
/// Batch: Unity.exe -batchmode -quit -executeMethod NexumDeviceShot.Run
///        (-nographics VERİLMEZ; render bağlamı gerekir)
/// </summary>
public static class NexumDeviceShot
{
    private const string Tag = "NEXUM_DEVICESHOT";
    private const string OutDir = "docs/device-shots";

    private static readonly Vector2Int[] Boyutlar =
    {
        new Vector2Int(1080, 1920),
        new Vector2Int(1440, 1920),
    };

    private struct Ekran
    {
        public string ad;
        public string scene;
        public string[] ac;      // açılacak obje yolları (Canvas altından)
        public string[] kapa;    // kapatılacak obje yolları
        public bool tahtayiDoldur;
    }

    private static readonly Ekran[] Ekranlar =
    {
        new Ekran { ad = "ana-menu", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/MainMenuPanel" },
                    kapa = new[] { "SafeAreaRoot/SettingsPanel", "SafeAreaRoot/HowToPlayPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "ayarlar", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/SettingsPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/HowToPlayPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "nasil-oynanir", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/HowToPlayPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "tarif-defteri", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/RecipeBookPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/HowToPlayPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "oyun", scene = "Assets/Scenes/Game.unity",
                    ac = new string[0],
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true },

        new Ekran { ad = "duraklat", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "PausePanel" },
                    kapa = new[] { "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true },
    };

    [MenuItem("NEXUM/Gerçek Ekran Görüntüsü Al")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        int alinan = 0;

        foreach (Ekran e in Ekranlar)
        {
            foreach (Vector2Int boy in Boyutlar)
            {
                Scene scene = EditorSceneManager.OpenScene(e.scene, OpenSceneMode.Single);
                GameObject canvasGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
                if (canvasGo == null)
                {
                    Debug.LogError($"{Tag}: Canvas yok -> {e.scene}");
                    continue;
                }

                Hazirla(canvasGo.transform, e);
                if (e.tahtayiDoldur) TahtayiDoldur(scene);

                string yol = $"{OutDir}/{e.ad}_{boy.x}x{boy.y}.png";
                if (Cek(canvasGo, boy, yol)) alinan++;
            }
        }

        Debug.Log($"{Tag}: {alinan} görüntü -> {OutDir}");
        Debug.Log($"{Tag}: OK");
    }

    private static void Hazirla(Transform canvas, Ekran e)
    {
        foreach (string p in e.kapa)
        {
            Transform t = canvas.Find(p);
            if (t != null) t.gameObject.SetActive(false);
        }
        foreach (string p in e.ac)
        {
            Transform t = canvas.Find(p);
            if (t != null)
            {
                t.gameObject.SetActive(true);
                t.localScale = Vector3.one;      // panel geçişi tween'i ölçeği 0'da bırakmış olabilir
            }
        }
    }

    /// <summary>Oyun ekranı için tahtaya dört seviyeden taş yerleştirir.</summary>
    private static void TahtayiDoldur(Scene scene)
    {
        GridManager grid = Object.FindObjectOfType<GridManager>(true);
        if (grid == null) return;

        var cellsField = typeof(GridManager).GetField("cells",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cells = cellsField != null ? cellsField.GetValue(grid) as List<Transform> : null;

        // Hücreler Start'ta kuruluyor; edit mode'da yoklar. GridBoard'un
        // çocukları doğrudan kullanılır.
        Transform board = Object.FindObjectsOfType<RectTransform>(true)
            .FirstOrDefault(r => r.name == "GridBoard");
        if (board == null) return;

        string[] dizilim =
        {
            "Tile_H", "Tile_O", "Tile_H2O", "Tile_C",
            "Tile_Na", "Tile_Cl", "Tile_O2", "Tile_N",
            "Tile_Fe2O3", "Tile_CaO", "Tile_H", "Tile_O",
            "Tile_CO2", "Tile_N2", "Tile_Ca", "Tile_CaCO3",
        };

        for (int i = 0; i < board.childCount && i < dizilim.Length; i++)
        {
            Transform hucre = board.GetChild(i);
            for (int j = hucre.childCount - 1; j >= 0; j--) Object.DestroyImmediate(hucre.GetChild(j).gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + dizilim[i] + ".prefab");
            if (prefab == null) continue;

            GameObject tas = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hucre);
            tas.transform.localScale = Vector3.one;
            tas.transform.localPosition = Vector3.zero;
        }

        // Tahtanın bir hücresi boş kalsın: dolu/boş ayrımı görülebilsin
        if (board.childCount > 7)
        {
            Transform bos = board.GetChild(7);
            for (int j = bos.childCount - 1; j >= 0; j--) Object.DestroyImmediate(bos.GetChild(j).gameObject);
        }
    }

    /// <summary>
    /// Canvas'ı geçici olarak bir kameraya bağlar, hedef boyutta
    /// RenderTexture'a çizdirir ve PNG yazar.
    /// </summary>
    private static bool Cek(GameObject canvasGo, Vector2Int boy, string yol)
    {
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (canvas == null) return false;

        RenderMode eskiMod = canvas.renderMode;
        Camera eskiKamera = canvas.worldCamera;
        float eskiMesafe = canvas.planeDistance;

        GameObject kameraGo = new GameObject("NexumShotCamera");
        Camera kamera = kameraGo.AddComponent<Camera>();
        kamera.orthographic = true;
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = new Color(0.02f, 0.05f, 0.08f, 1f);
        kamera.nearClipPlane = 0.1f;
        kamera.farClipPlane = 1000f;
        kamera.transform.position = new Vector3(0f, 0f, -500f);

        RenderTexture rt = new RenderTexture(boy.x, boy.y, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        kamera.targetTexture = rt;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = kamera;
        canvas.planeDistance = 100f;

        // SafeArea scripti kendi hesabını dayatmasın: gerçek cihazda güvenli
        // alan tam ekran olabilir, burada ölçüm netliği için kapatılıyor.
        Transform safe = canvasGo.transform.Find("SafeAreaRoot");
        MonoBehaviour safeArea = safe != null
            ? safe.GetComponents<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea")
            : null;
        if (safeArea != null) safeArea.enabled = false;

        Canvas.ForceUpdateCanvases();
        foreach (TMP_Text t in canvasGo.GetComponentsInChildren<TMP_Text>(true)) t.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        kamera.Render();

        RenderTexture onceki = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D png = new Texture2D(boy.x, boy.y, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, boy.x, boy.y), 0, 0);
        png.Apply();
        RenderTexture.active = onceki;

        File.WriteAllBytes(yol, png.EncodeToPNG());
        Debug.Log($"{Tag}: {yol}  canvas {canvas.pixelRect.width:F0}x{canvas.pixelRect.height:F0}  " +
                  $"scaleFactor {canvas.scaleFactor:F3}" +
                  (scaler != null ? $"  referans {scaler.referenceResolution.x:F0}x{scaler.referenceResolution.y:F0}" : ""));

        Object.DestroyImmediate(png);
        kamera.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(kameraGo);

        canvas.renderMode = eskiMod;
        canvas.worldCamera = eskiKamera;
        canvas.planeDistance = eskiMesafe;
        return true;
    }
}
