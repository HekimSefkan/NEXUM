using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Katalizör göstergesinin başlığını (ne işe yaradığını söyleyen metin) kurar
// ve UIManager'daki alana bağlar.
//
// IDEMPOTENT: etiket varsa yeniden oluşturmaz, yalnızca değerlerini denetler.
//
// Menüden: NEXUM -> Katalizör Göstergesini Kur
// Batch mode: -executeMethod NexumCatalystMeter.Run
public static class NexumCatalystMeter
{
    private const string Tag = "NEXUM_CATALYST_METER";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string MeterPath = "Canvas/SafeAreaRoot/PressureMeter";
    private const string LabelName = "MeterLabel";

    [MenuItem("NEXUM/Katalizör Göstergesini Kur")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        GameObject meter = GameObject.Find(MeterPath);
        if (meter == null)
        {
            Debug.LogError($"{Tag}: gösterge bulunamadı: {MeterPath}");
            return;
        }

        bool changed = false;

        Transform existing = meter.transform.Find(LabelName);
        GameObject label;
        if (existing == null)
        {
            label = new GameObject(LabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(meter.transform, false);
            changed = true;
        }
        else
        {
            label = existing.gameObject;
        }

        // Çubuğun hemen üstünde, çubukla aynı genişlikte
        RectTransform rect = label.GetComponent<RectTransform>();
        if (ApplyRect(rect)) changed = true;

        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        if (ApplyText(text)) changed = true;

        // UIManager alanına bağla
        UIManager ui = Object.FindObjectOfType<UIManager>();
        if (ui == null)
        {
            Debug.LogError($"{Tag}: UIManager yok");
            return;
        }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty prop = so.FindProperty("catalystLabelText");
        if (prop == null)
        {
            Debug.LogError($"{Tag}: UIManager.catalystLabelText alanı yok");
            return;
        }
        if (prop.objectReferenceValue != text)
        {
            prop.objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"{Tag}: {(changed ? "kuruldu/güncellendi" : "zaten güncel")}");
        Debug.Log($"{Tag}: OK");
    }

    private static bool ApplyRect(RectTransform rect)
    {
        bool changed = false;
        if (rect.anchorMin != new Vector2(0.5f, 0.5f)) { rect.anchorMin = new Vector2(0.5f, 0.5f); changed = true; }
        if (rect.anchorMax != new Vector2(0.5f, 0.5f)) { rect.anchorMax = new Vector2(0.5f, 0.5f); changed = true; }
        if (rect.pivot != new Vector2(0.5f, 0.5f)) { rect.pivot = new Vector2(0.5f, 0.5f); changed = true; }
        if (rect.sizeDelta != new Vector2(600f, 44f)) { rect.sizeDelta = new Vector2(600f, 44f); changed = true; }
        if (rect.anchoredPosition != new Vector2(0f, 52f)) { rect.anchoredPosition = new Vector2(0f, 52f); changed = true; }
        if (rect.localScale != Vector3.one) { rect.localScale = Vector3.one; changed = true; }
        return changed;
    }

    private static bool ApplyText(TextMeshProUGUI text)
    {
        bool changed = false;
        if (text.text != "KATALİZÖR ŞARJI 0/5") { text.text = "KATALİZÖR ŞARJI 0/5"; changed = true; }
        if (text.fontSize != 28f) { text.fontSize = 28f; changed = true; }
        if (text.enableAutoSizing) { text.enableAutoSizing = false; changed = true; }
        if (text.alignment != TextAlignmentOptions.Center) { text.alignment = TextAlignmentOptions.Center; changed = true; }
        if (text.raycastTarget) { text.raycastTarget = false; changed = true; }

        Color target = new Color32(0x8F, 0xD8, 0xFF, 0xFF);
        if (text.color != target) { text.color = target; changed = true; }

        // Sahnedeki diğer HUD metinleriyle aynı font
        TextMeshProUGUI reference = FindHudFont();
        if (reference != null && reference.font != null && text.font != reference.font)
        {
            text.font = reference.font;
            changed = true;
        }
        return changed;
    }

    private static TextMeshProUGUI FindHudFont()
    {
        GameObject score = GameObject.Find("Canvas/SafeAreaRoot/ScoreButton");
        return score != null ? score.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }
}
