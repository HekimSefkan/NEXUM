using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TEŞHİS ARACI — hiçbir şeyi değiştirmez, yalnızca ölçer.
///
/// Aynı objeleri İKİ REJİMDE ölçüp yan yana koyar:
///
///   A) "LayoutCheck rejimi" — batch mode'un 640×480 canvas'ı, SafeAreaRoot'a
///      hedef boyut DAYATILMIŞ. Testlerin gördüğü dünya.
///   B) "Gerçek rejim" — Canvas bir kameraya bağlı, kamera hedef boyutta bir
///      RenderTexture'a çiziyor. Cihazın gördüğü dünya.
///
/// Fark varsa testin neyi kaçırdığı buradan okunur.
///
/// Batch: -executeMethod NexumLayoutDiag.Run   (-nographics VERİLMEZ)
/// </summary>
public static class NexumLayoutDiag
{
    private const string Tag = "NEXUM_DIAG";

    private static readonly string[] AyarlarYollari =
    {
        "SafeAreaRoot/SettingsPanel",
        "SafeAreaRoot/SettingsPanel/GlassBackground",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/Text (TMP)",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/GameModeText",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ModeContainer",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ModeContainer/NormalModeBtn",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ModeContainer/ExamModeBtn",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ModeContainer/FreeModeBtn",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/AudioTitle",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/MusicRow",
        "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ResetProgressButton",
    };

    [MenuItem("NEXUM/Teşhis: Yerleşim Karşılaştırması")]
    public static void Run()
    {
        Olc("ayarlar", "Assets/Scenes/MainMenu.unity", "SafeAreaRoot/SettingsPanel",
            new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/HowToPlayPanel",
                    "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel" },
            AyarlarYollari);

        BilesenDokumu();
        SpriteOlcuKarsilastirmasi();
        TarifDefteriButonu();
    }

    private static void Olc(string ad, string scene, string acilacak, string[] kapanacak, string[] yollar)
    {
        Vector2 hedef = new Vector2(1080f, 1920f);

        Debug.Log($"{Tag}: ===== {ad} — A) LayoutCheck rejimi (canvas 640x480, SafeAreaRoot dayatılmış)");
        Dictionary<string, Vector4> a = OlcRejimA(scene, acilacak, kapanacak, yollar, hedef);

        Debug.Log($"{Tag}: ===== {ad} — B) Gerçek rejim (canvas kamerayla {hedef.x}x{hedef.y})");
        Dictionary<string, Vector4> b = OlcRejimB(scene, acilacak, kapanacak, yollar, hedef);

        Debug.Log($"{Tag}: ===== KARŞILAŞTIRMA (genişlik x yükseklik)");
        foreach (string y in yollar)
        {
            string kisa = y.Substring(y.LastIndexOf('/') + 1);
            Vector4 ra, rb;
            if (!a.TryGetValue(y, out ra) || !b.TryGetValue(y, out rb))
            {
                Debug.Log($"{Tag}_FARK: {kisa,-22} (obje bulunamadı)");
                continue;
            }

            float aw = ra.z - ra.x, ah = ra.w - ra.y;
            float bw = rb.z - rb.x, bh = rb.w - rb.y;
            string isaret = (Mathf.Abs(aw - bw) > 2f || Mathf.Abs(ah - bh) > 2f) ? "  <<< FARKLI" : "";
            Debug.Log($"{Tag}_FARK: {kisa,-22} A {aw,7:F1}x{ah,-7:F1}  B {bw,7:F1}x{bh,-7:F1}{isaret}");
        }
    }

    private static Dictionary<string, Vector4> OlcRejimA(string scene, string acilacak, string[] kapanacak,
                                                         string[] yollar, Vector2 hedef)
    {
        Scene s = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        Transform canvas = Kur(s, acilacak, kapanacak);
        if (canvas == null) return new Dictionary<string, Vector4>();

        // LayoutCheck'in yaptığı: SafeArea kapat + SafeAreaRoot'a boyut dayat
        RectTransform safe = canvas.Find("SafeAreaRoot") as RectTransform;
        MonoBehaviour sa = safe.GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea");
        if (sa != null) sa.enabled = false;

        safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
        safe.pivot = new Vector2(0.5f, 0.5f);
        safe.sizeDelta = hedef;
        safe.anchoredPosition = Vector2.zero;

        Canvas c = canvas.GetComponent<Canvas>();
        Debug.Log($"{Tag}_REJIM_A: canvas {c.pixelRect.width:F0}x{c.pixelRect.height:F0} " +
                  $"scaleFactor {c.scaleFactor:F3}  SafeAreaRoot {safe.rect.width:F0}x{safe.rect.height:F0}");

        Canvas.ForceUpdateCanvases();
        return Topla(canvas, safe, yollar);
    }

    private static Dictionary<string, Vector4> OlcRejimB(string scene, string acilacak, string[] kapanacak,
                                                         string[] yollar, Vector2 hedef)
    {
        Scene s = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        Transform canvas = Kur(s, acilacak, kapanacak);
        if (canvas == null) return new Dictionary<string, Vector4>();

        RectTransform safe = canvas.Find("SafeAreaRoot") as RectTransform;
        MonoBehaviour sa = safe.GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea");
        if (sa != null) sa.enabled = false;

        Canvas c = canvas.GetComponent<Canvas>();
        GameObject kameraGo = new GameObject("DiagCamera");
        Camera kamera = kameraGo.AddComponent<Camera>();
        kamera.orthographic = true;
        kamera.transform.position = new Vector3(0, 0, -500);
        RenderTexture rt = new RenderTexture((int)hedef.x, (int)hedef.y, 24);
        kamera.targetTexture = rt;

        RenderMode eski = c.renderMode;
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = kamera;
        c.planeDistance = 100f;

        Canvas.ForceUpdateCanvases();
        Debug.Log($"{Tag}_REJIM_B: canvas {c.pixelRect.width:F0}x{c.pixelRect.height:F0} " +
                  $"scaleFactor {c.scaleFactor:F3}  SafeAreaRoot {safe.rect.width:F0}x{safe.rect.height:F0}");

        Dictionary<string, Vector4> sonuc = Topla(canvas, safe, yollar);

        c.renderMode = eski;
        kamera.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(kameraGo);
        return sonuc;
    }

    private static Transform Kur(Scene s, string acilacak, string[] kapanacak)
    {
        GameObject canvasGo = s.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
        if (canvasGo == null) return null;

        foreach (string p in kapanacak)
        {
            Transform t = canvasGo.transform.Find(p);
            if (t != null) t.gameObject.SetActive(false);
        }

        Transform a = canvasGo.transform.Find(acilacak);
        if (a != null)
        {
            a.gameObject.SetActive(true);
            a.localScale = Vector3.one;
        }
        return canvasGo.transform;
    }

    private static Dictionary<string, Vector4> Topla(Transform canvas, RectTransform kok, string[] yollar)
    {
        Dictionary<string, Vector4> sonuc = new Dictionary<string, Vector4>();
        foreach (string y in yollar)
        {
            RectTransform rt = canvas.Find(y) as RectTransform;
            if (rt == null) continue;

            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 p0 = kok.InverseTransformPoint(c[0]);
            Vector3 p2 = kok.InverseTransformPoint(c[2]);
            sonuc[y] = new Vector4(Mathf.Min(p0.x, p2.x), Mathf.Min(p0.y, p2.y),
                                   Mathf.Max(p0.x, p2.x), Mathf.Max(p0.y, p2.y));
        }
        return sonuc;
    }

    /// <summary>Çökmenin sebebi hangi bileşen? Layout zinciri dökülür.</summary>
    private static void BilesenDokumu()
    {
        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        GameObject canvasGo = s.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
        Transform mode = canvasGo.transform.Find("SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ModeContainer");
        if (mode == null) { Debug.LogError($"{Tag}: ModeContainer yok"); return; }

        Debug.Log($"{Tag}: ===== ModeContainer bileşen zinciri");
        Dok(mode, 0);

        foreach (Transform ch in mode)
        {
            Dok(ch, 1);
            foreach (Transform g in ch) Dok(g, 2);
        }
    }

    private static void Dok(Transform t, int derinlik)
    {
        string girinti = new string(' ', derinlik * 2);
        List<string> parcalar = new List<string>();

        RectTransform rt = t as RectTransform;
        if (rt != null)
        {
            parcalar.Add($"rect {rt.rect.width:F1}x{rt.rect.height:F1}");
            parcalar.Add($"anchor {rt.anchorMin.x:F2},{rt.anchorMin.y:F2}-{rt.anchorMax.x:F2},{rt.anchorMax.y:F2}");
            parcalar.Add($"sizeDelta {rt.sizeDelta.x:F0},{rt.sizeDelta.y:F0}");
        }

        HorizontalOrVerticalLayoutGroup hv = t.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (hv != null)
        {
            parcalar.Add($"{hv.GetType().Name}(ctrlW={hv.childControlWidth} expW={hv.childForceExpandWidth} " +
                         $"ctrlH={hv.childControlHeight} spacing={hv.spacing})");
        }

        ContentSizeFitter csf = t.GetComponent<ContentSizeFitter>();
        if (csf != null) parcalar.Add($"CSF(h={csf.horizontalFit} v={csf.verticalFit})");

        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le != null)
        {
            parcalar.Add($"LE(minW={le.minWidth} prefW={le.preferredWidth} flexW={le.flexibleWidth})");
        }

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            string sp = img.sprite != null ? img.sprite.name : "(None)";
            Vector2 native = img.sprite != null ? img.sprite.rect.size : Vector2.zero;
            parcalar.Add($"Image({sp} {native.x:F0}x{native.y:F0} type={img.type} " +
                         $"prefW={((ILayoutElement)img).preferredWidth:F1})");
        }

        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            parcalar.Add($"TMP(\"{tmp.text}\" punto={tmp.fontSize} prefW={tmp.preferredWidth:F1})");
        }

        Debug.Log($"{Tag}_BILESEN: {girinti}{t.name}  " + string.Join("  ", parcalar));
    }

    /// <summary>
    /// Layout'u süren değer: Image'ın preferredWidth'i. Eski ve yeni sprite
    /// aynı Image üzerinde sırayla denenip ölçülür (sahne KAYDEDİLMEZ).
    /// </summary>
    private static void SpriteOlcuKarsilastirmasi()
    {
        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        GameObject canvasGo = s.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
        Transform btn = canvasGo.transform.Find(
            "SafeAreaRoot/SettingsPanel/GlassBackground/CardContent/ModeContainer/NormalModeBtn");
        if (btn == null) { Debug.LogError($"{Tag}: NormalModeBtn yok"); return; }

        Image img = btn.GetComponent<Image>();
        Sprite yeni = img.sprite;
        Sprite eski = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Figures/SettingsPanel_modsecimkartı.png");

        Debug.Log($"{Tag}: ===== Layout'u süren değer: Image.preferredWidth");

        foreach (var ikili in new[] { new { ad = "ESKI", sp = eski, tip = Image.Type.Simple },
                                      new { ad = "YENI", sp = yeni, tip = Image.Type.Sliced } })
        {
            if (ikili.sp == null) { Debug.LogError($"{Tag}: {ikili.ad} sprite yok"); continue; }

            img.sprite = ikili.sp;
            img.type = ikili.tip;
            ILayoutElement le = img;
            le.CalculateLayoutInputHorizontal();
            le.CalculateLayoutInputVertical();

            Debug.Log($"{Tag}_SPRITEOLCU: {ikili.ad,-5} {ikili.sp.name,-34} " +
                      $"doku {ikili.sp.rect.width:F0}x{ikili.sp.rect.height:F0}  " +
                      $"border {ikili.sp.border}  PPU {ikili.sp.pixelsPerUnit:F0}  type {ikili.tip}  " +
                      $"=> preferredWidth {le.preferredWidth:F1}  preferredHeight {le.preferredHeight:F1}");
        }

        // Sahne kaydedilmiyor; bellekteki değişiklik atılıyor.
        img.sprite = yeni;
        img.type = Image.Type.Sliced;
    }

    /// <summary>Tarif defteri butonunun etiket rengi ve yüzeyle kontrastı.</summary>
    private static void TarifDefteriButonu()
    {
        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        GameObject canvasGo = s.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
        Transform btn = canvasGo.transform.Find("SafeAreaRoot/MainMenuPanel/RecipeBookButton");
        if (btn == null) { Debug.LogError($"{Tag}: RecipeBookButton yok"); return; }

        Image img = btn.GetComponent<Image>();
        TMP_Text etiket = btn.GetComponentInChildren<TMP_Text>(true);

        string sp = img != null && img.sprite != null ? img.sprite.name : "(None)";
        Debug.Log($"{Tag}_DEFTERBTN: sprite={sp}  Image.color=#{ColorUtility.ToHtmlStringRGB(img.color)}");
        if (etiket != null)
        {
            Debug.Log($"{Tag}_DEFTERBTN: etiket=\"{etiket.text}\" " +
                      $"renk=#{ColorUtility.ToHtmlStringRGB(etiket.color)} punto={etiket.fontSize}");
        }
    }
}
