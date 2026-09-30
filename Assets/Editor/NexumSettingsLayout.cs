using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ayarlar panelinin içeriğini CAM KARTIN üzerine oturtur.
//
// SORUN: GlassBackground tam ekran stretch bir RectTransform, sprite'ı ise
// PreserveAspect ile çiziliyor. Yani görünen kart RectTransform'dan küçük ve
// ekran oranına göre değişiyor. İçerik doğrudan bu tam ekran rect'e
// yerleştirildiği için başlıklar kartın dışına taşıyordu.
//
// ÇÖZÜM: GlassBackground altına, sprite oranını (796/1280) AspectRatioFitter
// ile taşıyan bir CardContent kutusu kurulur — yani görünen kartla birebir
// aynı dikdörtgen. Tüm içerik oraya taşınır ve ORANSAL (stretch) anchor'larla
// yerleştirilir; böylece kart hangi ekranda ne kadar büyürse içerik de aynı
// oranda büyür.
//
// IDEMPOTENT: iki kez çalıştırıldığında aynı sonucu verir; hiçbir obje silinmez.
//
// Menüden: NEXUM -> Ayarlar Panelini Yerleştir
// Batch mode: -executeMethod NexumSettingsLayout.Run
public static class NexumSettingsLayout
{
    private const string Tag = "NEXUM_SETTINGS_LAYOUT";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GlassPath = "Canvas/SafeAreaRoot/SettingsPanel/GlassBackground";
    private const string ContentName = "CardContent";

    private const float SpriteW = 796f;
    private const float SpriteH = 1280f;

    // Çerçevenin iç kenarları (sprite pikselinden orana çevrildi)
    private const float Left = 100f / SpriteW;    // 0.1256
    private const float Right = 690f / SpriteW;   // 0.8668

    private struct Band
    {
        public string name;
        public float x0, x1, y0, y1;   // CardContent içinde oransal
        public Vector2 fixedSize;      // (0,0) değilse: nokta anchor + sabit boyut

        public Band(string n, float ax0, float ax1, float ay0, float ay1)
        { name = n; x0 = ax0; x1 = ax1; y0 = ay0; y1 = ay1; fixedSize = Vector2.zero; }

        public Band(string n, float ax0, float ax1, float ay0, float ay1, Vector2 size)
        { name = n; x0 = ax0; x1 = ax1; y0 = ay0; y1 = ay1; fixedSize = size; }
    }

    // Kartın üç bölümü (cam kartın parlak ayraçları): 0.575 ve 0.3172
    // Dikey ritim her bölümde aynı: başlık 0.043, satır 0.058,
    // başlık sonrası boşluk 0.015, satır arası 0.012.
    private static readonly Band[] Layout =
    {
        // --- 1. bölüm: başlık + oyun modu
        // Bu ikisi sabit boyutlu: kart büyüyünce logo dokusunun ekrandaki
        // kullanım boyutu da büyüyüp çözünürlük uyarısı doğuruyordu.
        new Band("Image",               0.175f, 0.520f, 0.8815f, 0.9315f, new Vector2(372f, 87f)),
        new Band("Text (TMP)",          0.530f, 0.865f, 0.8815f, 0.9315f, new Vector2(367f, 87f)),
        new Band("GameModeText",        Left,   Right,  0.8235f, 0.8665f),
        new Band("ModeContainer",       Left,   Right,  0.6105f, 0.8085f),

        // --- 2. bölüm: ses
        new Band("AudioTitle",          Left,   Right,  0.5220f, 0.5650f),
        new Band("MusicRow",            0.220f, 0.780f, 0.4490f, 0.5070f),
        new Band("SfxRow",              0.220f, 0.780f, 0.3790f, 0.4370f),

        // --- 3. bölüm: öğrenme
        new Band("LearningTitle",       Left,   Right,  0.2642f, 0.3072f),
        new Band("FlashcardsRow",       0.220f, 0.780f, 0.1912f, 0.2492f),
        new Band("HintsRow",            0.220f, 0.780f, 0.1212f, 0.1792f),
        new Band("ResetProgressButton", 0.320f, 0.680f, 0.0762f, 0.1062f),
    };

    [MenuItem("NEXUM/Ayarlar Panelini Yerleştir")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject glass = GameObject.Find(GlassPath);
        if (glass == null)
        {
            Debug.LogError($"{Tag}: bulunamadı: {GlassPath}");
            return;
        }

        bool changed = EnsureContent(glass, out RectTransform content);

        foreach (Band band in Layout)
        {
            Transform t = glass.transform.Find(band.name) ?? content.Find(band.name);
            if (t == null)
            {
                Debug.LogError($"{Tag}: içerik bulunamadı: {band.name}");
                continue;
            }

            if (t.parent != content)
            {
                t.SetParent(content, false);
                changed = true;
            }

            if (ApplyBand(t as RectTransform, band)) changed = true;
        }

        // Logo gerilmesin
        Transform logo = content.Find("Image");
        if (logo != null)
        {
            Image img = logo.GetComponent<Image>();
            if (img != null && !img.preserveAspect) { img.preserveAspect = true; changed = true; }
        }

        // "AYARLARI" logonun hemen sağında başlasın; bölüm başlıkları ortalı kalsın
        if (SetAlignment(content, "Text (TMP)", TextAlignmentOptions.Left)) changed = true;
        foreach (string title in new[] { "GameModeText", "AudioTitle", "LearningTitle" })
        {
            if (SetAlignment(content, title, TextAlignmentOptions.Center)) changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"{Tag}: {(changed ? "yerleştirildi/güncellendi" : "zaten güncel")}");
        Debug.Log($"{Tag}: OK");
    }

    private static bool SetAlignment(RectTransform content, string name, TextAlignmentOptions align)
    {
        Transform t = content.Find(name);
        if (t == null) return false;

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp == null || tmp.alignment == align) return false;

        tmp.alignment = align;
        EditorUtility.SetDirty(tmp);
        return true;
    }

    private static bool EnsureContent(GameObject glass, out RectTransform content)
    {
        bool changed = false;
        Transform existing = glass.transform.Find(ContentName);
        GameObject go;
        if (existing == null)
        {
            go = new GameObject(ContentName, typeof(RectTransform), typeof(AspectRatioFitter));
            go.transform.SetParent(glass.transform, false);
            go.transform.SetAsFirstSibling();
            changed = true;
        }
        else
        {
            go = existing.gameObject;
        }

        content = go.GetComponent<RectTransform>();
        if (content.anchorMin != Vector2.zero) { content.anchorMin = Vector2.zero; changed = true; }
        if (content.anchorMax != Vector2.one) { content.anchorMax = Vector2.one; changed = true; }
        if (content.pivot != new Vector2(0.5f, 0.5f)) { content.pivot = new Vector2(0.5f, 0.5f); changed = true; }
        if (content.offsetMin != Vector2.zero) { content.offsetMin = Vector2.zero; changed = true; }
        if (content.offsetMax != Vector2.zero) { content.offsetMax = Vector2.zero; changed = true; }
        if (content.localScale != Vector3.one) { content.localScale = Vector3.one; changed = true; }

        AspectRatioFitter fitter = go.GetComponent<AspectRatioFitter>();
        if (fitter == null) { fitter = go.AddComponent<AspectRatioFitter>(); changed = true; }
        if (fitter.aspectMode != AspectRatioFitter.AspectMode.FitInParent)
        { fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; changed = true; }

        float ratio = SpriteW / SpriteH;
        if (!Mathf.Approximately(fitter.aspectRatio, ratio)) { fitter.aspectRatio = ratio; changed = true; }
        if (!fitter.enabled) { fitter.enabled = true; changed = true; }

        return changed;
    }

    private static bool ApplyBand(RectTransform rect, Band band)
    {
        if (rect == null) return false;

        bool changed = false;
        if (rect.pivot != new Vector2(0.5f, 0.5f)) { rect.pivot = new Vector2(0.5f, 0.5f); changed = true; }
        if (rect.localScale != Vector3.one) { rect.localScale = Vector3.one; changed = true; }

        if (band.fixedSize != Vector2.zero)
        {
            // Nokta anchor bandın ortasında; boyut sabit, konum kartla birlikte kayar
            Vector2 point = new Vector2((band.x0 + band.x1) * 0.5f, (band.y0 + band.y1) * 0.5f);
            if (rect.anchorMin != point) { rect.anchorMin = point; changed = true; }
            if (rect.anchorMax != point) { rect.anchorMax = point; changed = true; }
            if (rect.sizeDelta != band.fixedSize) { rect.sizeDelta = band.fixedSize; changed = true; }
            if (rect.anchoredPosition != Vector2.zero) { rect.anchoredPosition = Vector2.zero; changed = true; }
            return changed;
        }

        Vector2 min = new Vector2(band.x0, band.y0);
        Vector2 max = new Vector2(band.x1, band.y1);

        if (rect.anchorMin != min) { rect.anchorMin = min; changed = true; }
        if (rect.anchorMax != max) { rect.anchorMax = max; changed = true; }
        if (rect.offsetMin != Vector2.zero) { rect.offsetMin = Vector2.zero; changed = true; }
        if (rect.offsetMax != Vector2.zero) { rect.offsetMax = Vector2.zero; changed = true; }
        if (rect.anchoredPosition != Vector2.zero) { rect.anchoredPosition = Vector2.zero; changed = true; }

        return changed;
    }
}
