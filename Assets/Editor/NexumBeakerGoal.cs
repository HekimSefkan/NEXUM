using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hedef beheri: formül ve sayaç beherin İÇİNDEN YANINA alındı (3b/B3).
///
/// Eski hâlde <c>AmountText</c> beherin tam altına (y −132) oturuyordu ve
/// 120×80'lik kutuya iki satır sığmadığı için etiket kabın 22 birim dışına
/// taşıyordu (bilinen borç, 2d-2'den beri açık). Metnin kendisi de cam
/// gövdenin üstünde kaldığı için okunmuyordu.
///
/// Yeni yerleşim — tek yuva 250 birim:
///   beher 150×180 solda · etiket 90×150 sağda, sola hizalı, dikeyde ortalı
/// <c>GoalsContainer</c> 500 birim ve iki hedefli bölümlerde iki yuva
/// (250 + 250) tam oturuyor; aralık sıfırlandı.
///
/// GÖRSEL ÜRETİLMEDİ: aynı <c>Beherglas.png</c>, yalnızca ölçü ve konum.
/// Metin içeriğini <c>UIManager</c> yazar, buraya dokunulmadı.
///
/// Menüden: NEXUM → Hedef Beherini Yerleştir
/// Batch:   -executeMethod NexumBeakerGoal.Run
/// </summary>
public static class NexumBeakerGoal
{
    private const string Tag = "NEXUM_BEHER";
    private const string PrefabPath = "Assets/Prefabs/BeakerGoalPrefab.prefab";

    private static readonly Vector2 SlotSize = new Vector2(250f, 190f);
    private static readonly Vector2 FrameSize = new Vector2(150f, 180f);
    private static readonly Vector2 LabelSize = new Vector2(90f, 150f);
    private const float FrameX = -48f;      // yuva merkezine göre
    private const float LabelX = 78f;
    private const float LabelFont = 26f;

    [MenuItem("NEXUM/Hedef Beherini Yerleştir")]
    public static void Run()
    {
        GameObject kok = PrefabUtility.LoadPrefabContents(PrefabPath);
        bool degisti = false;

        RectTransform root = kok.GetComponent<RectTransform>();
        degisti |= Olcu(root, SlotSize, Vector2.zero, "kök");

        degisti |= Yerlestir(kok, "BeakerFrame", FrameSize, new Vector2(FrameX, 0f));
        degisti |= Yerlestir(kok, "LiquidFill", new Vector2(57f, 90f), new Vector2(FrameX + 1f, 0f));
        degisti |= Yerlestir(kok, "ElementIcon", new Vector2(60f, 60f), new Vector2(FrameX + 1f, 6f));
        degisti |= Yerlestir(kok, "AmountText", LabelSize, new Vector2(LabelX, 0f));

        Transform etiket = Bul(kok.transform, "AmountText");
        if (etiket != null)
        {
            TMP_Text tmp = etiket.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                if (!Mathf.Approximately(tmp.fontSize, LabelFont)) { tmp.fontSize = LabelFont; degisti = true; }
                if (tmp.alignment != TextAlignmentOptions.Left) { tmp.alignment = TextAlignmentOptions.Left; degisti = true; }
                if (!tmp.enableWordWrapping) { tmp.enableWordWrapping = true; degisti = true; }
            }
        }

        Transform frame = Bul(kok.transform, "BeakerFrame");
        if (frame != null)
        {
            Image img = frame.GetComponent<Image>();
            if (img != null && !img.preserveAspect) { img.preserveAspect = true; degisti = true; }
        }

        if (degisti) PrefabUtility.SaveAsPrefabAsset(kok, PrefabPath);
        PrefabUtility.UnloadPrefabContents(kok);
        Debug.Log($"{Tag}: prefab {(degisti ? "güncellendi" : "zaten yerinde")}");

        KonteyneriKur();

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>İki yuva (250+250) 500 birimlik kaba tam otursun: aralık 0.</summary>
    private static void KonteyneriKur()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        HorizontalOrVerticalLayoutGroup grup = Object.FindObjectsOfType<HorizontalOrVerticalLayoutGroup>(true)
            .FirstOrDefault(g => g.name == "GoalsContainer");
        if (grup == null)
        {
            Debug.LogError($"{Tag}: GoalsContainer yok");
            return;
        }

        bool degisti = false;
        if (!Mathf.Approximately(grup.spacing, 0f)) { grup.spacing = 0f; degisti = true; }
        if (grup.childAlignment != TextAnchor.MiddleCenter)
        {
            grup.childAlignment = TextAnchor.MiddleCenter;
            degisti = true;
        }

        if (degisti)
        {
            EditorUtility.SetDirty(grup);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"{Tag}_KONTEYNER: aralık {grup.spacing:F0}, hizalama {grup.childAlignment}");
    }

    private static bool Yerlestir(GameObject kok, string ad, Vector2 boyut, Vector2 konum)
    {
        Transform t = Bul(kok.transform, ad);
        if (t == null)
        {
            Debug.LogError($"{Tag}: {ad} yok");
            return false;
        }
        return Olcu((RectTransform)t, boyut, konum, ad);
    }

    private static bool Olcu(RectTransform rt, Vector2 boyut, Vector2 konum, string ad)
    {
        Vector2 merkez = new Vector2(0.5f, 0.5f);
        if (rt.anchorMin == merkez && rt.anchorMax == merkez && rt.pivot == merkez
            && rt.sizeDelta == boyut && rt.anchoredPosition == konum) return false;

        rt.anchorMin = rt.anchorMax = rt.pivot = merkez;
        rt.sizeDelta = boyut;
        rt.anchoredPosition = konum;
        EditorUtility.SetDirty(rt);
        Debug.Log($"{Tag}_OGE: {ad} -> {boyut.x:F0}x{boyut.y:F0} @ ({konum.x:F0},{konum.y:F0})");
        return true;
    }

    private static Transform Bul(Transform kok, string ad)
    {
        if (kok.name == ad) return kok;
        foreach (Transform t in kok)
        {
            Transform b = Bul(t, ad);
            if (b != null) return b;
        }
        return null;
    }
}
