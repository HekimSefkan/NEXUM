using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bölüm hedefi gösterimi: BEHER DEĞİL, HEDEFİN KENDİ TAŞI (3c/3).
///
/// Eski gösterim bir beher çiziyor, hedefin sprite'ını behere küçük bir
/// rozet olarak basıyor ve formülü yanına metin olarak yazıyordu. Yani
/// oyuncunun tahtada arayacağı şey ile hedef kutusunda gördüğü şey AYNI
/// GÖRSEL DEĞİLDİ: bir tarafta cam beher + minik ikon, diğer tarafta
/// metal karo. Artık hedef, tahtadaki taşın birebir kendisidir — aynı
/// prefab, aynı sprite, aynı pişmiş punto — altında sayaç durur.
///
/// TEK KAYNAK: taş görseli burada ÇOĞALTILMAZ. <c>UIManager.InitGoalsUI</c>
/// doğrudan <c>goal.targetPrefab</c>'ı klonlar; bu kurucu yalnızca klonun
/// oturacağı yuvayı ve altındaki sayaç satırını üretir.
///
/// IDEMPOTENT: prefab varsa içeriği karşılaştırılır, fark yoksa yazılmaz.
///
/// Menüden: NEXUM → Hedef Taşı Kutusunu Kur
/// Batch:   -executeMethod GoalTileBuilder.Run
/// </summary>
public static class GoalTileBuilder
{
    private const string Tag = "NEXUM_HEDEFTAS";
    private const string PrefabPath = "Assets/Prefabs/GoalTilePrefab.prefab";
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string HudPath = "Canvas/SafeAreaRoot";

    /// <summary>Kutu: taş yuvası + sayaç satırı.</summary>
    private static readonly Vector2 ItemSize = new Vector2(200f, 248f);

    /// <summary>
    /// Taş yuvası 160: tahtadaki hücre 200, yani hedef taş tahtadakinin
    /// %80'i. Klon <see cref="UIManager"/> içinde CANLI hücre boyutundan
    /// ölçeklenir, buraya oran yazılmaz.
    /// </summary>
    private static readonly Vector2 SlotSize = new Vector2(160f, 160f);
    private static readonly Vector2 SlotPos = new Vector2(0f, 44f);

    private static readonly Vector2 CountSize = new Vector2(200f, 56f);
    private static readonly Vector2 CountPos = new Vector2(0f, -80f);

    /// <summary>HUD: hedef satırı ve skor göstergesinin yeni yeri.</summary>
    private static readonly Vector2 GridAnchor = new Vector2(0.5f, 0.41666666f);
    private static readonly Vector2 GoalsSize = new Vector2(520f, 260f);
    private static readonly Vector2 GoalsCenter = new Vector2(-250f, 640f);
    private static readonly Vector2 ScoreSize = new Vector2(250f, 100f);
    private static readonly Vector2 ScoreCenter = new Vector2(-380f, 400f);

    private static Vector2 Pos(Vector2 yerelMerkez)
    {
        return new Vector2(yerelMerkez.x, yerelMerkez.y - (GridAnchor.y * 1920f - 960f));
    }

    [MenuItem("NEXUM/Hedef Taşı Kutusunu Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        bool yeni = !File.Exists(PrefabPath);
        GameObject kok = yeni
            ? new GameObject("GoalItem", typeof(RectTransform))
            : PrefabUtility.LoadPrefabContents(PrefabPath);

        bool degisti = yeni;
        NexumTheme theme = UITokens.Theme;

        degisti |= Rect(kok, UITokens.Center, ItemSize, Vector2.zero);

        GameObject slot = Cocuk(kok, "TileSlot", ref degisti);
        degisti |= Rect(slot, UITokens.Center, SlotSize, SlotPos);

        GameObject count = Cocuk(kok, "CountText", ref degisti);
        degisti |= Rect(count, UITokens.Center, CountSize, CountPos);

        TextMeshProUGUI tmp = count.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = count.AddComponent<TextMeshProUGUI>();
            degisti = true;
        }
        degisti |= Uygula(tmp, t =>
        {
            t.font = theme.typography.label.font;
            t.fontSize = theme.typography.label.size;
            t.color = theme.palette.textPrimary;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            t.text = "0 / 1";
        });

        if (degisti && !dryRun)
        {
            PrefabUtility.SaveAsPrefabAsset(kok, PrefabPath);
        }

        if (yeni) Object.DestroyImmediate(kok);
        else PrefabUtility.UnloadPrefabContents(kok);

        Debug.Log($"{Tag}: prefab {(degisti ? (dryRun ? "DEĞİŞECEK" : "yazıldı") : "dokunulmadı")} -> {PrefabPath}");

        Yerlesim(dryRun);
    }

    /// <summary>Hedef satırı ve skor göstergesi: karakter kalkınca boşalan alan.</summary>
    private static void Yerlesim(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("GoalTiles", ScenePath, dryRun);

        GameObject goals = b.Ensure(HudPath, "GoalsContainer", adopt: true);
        if (goals != null)
        {
            b.SetFixed(goals, GridAnchor, GoalsSize, Pos(GoalsCenter));
            b.EnsureComponent<HorizontalLayoutGroup>(goals, g =>
            {
                g.childAlignment = TextAnchor.MiddleCenter;
                g.spacing = UITokens.SpaceMd;
                g.childForceExpandWidth = false;
                g.childForceExpandHeight = false;
                g.childControlWidth = false;
                g.childControlHeight = false;
            });
        }

        // Skor, karakterin boşalttığı sol alana iner: eski yerinde dokunma
        // alanı İPUCU butonununkiyle kesişiyordu (ölçüm PR'da).
        GameObject score = b.Ensure(HudPath, "ScoreButton", adopt: true);
        if (score != null) b.SetFixed(score, GridAnchor, ScoreSize, Pos(ScoreCenter));

        b.End();

        if (dryRun) return;
        Bagla();
    }

    /// <summary>UIManager.goalTilePrefab -> üretilen prefab.</summary>
    private static void Bagla()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        if (ui == null || prefab == null) return;

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty prop = so.FindProperty("goalTilePrefab");
        if (prop == null || prop.objectReferenceValue == prefab) return;

        prop.objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"{Tag}: UIManager.goalTilePrefab bağlandı");
    }

    // --------------------------------------------------------- yardımcılar

    private static GameObject Cocuk(GameObject kok, string ad, ref bool degisti)
    {
        Transform t = kok.transform.Find(ad);
        if (t != null) return t.gameObject;

        GameObject go = new GameObject(ad, typeof(RectTransform));
        go.transform.SetParent(kok.transform, false);
        degisti = true;
        return go;
    }

    private static bool Rect(GameObject go, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return false;

        bool fark = rt.anchorMin != anchor || rt.anchorMax != anchor
                    || rt.pivot != UITokens.Center
                    || rt.sizeDelta != size || rt.anchoredPosition != pos;
        if (!fark) return false;

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = UITokens.Center;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return true;
    }

    private static bool Uygula<T>(T bilesen, System.Action<T> ayarla) where T : Component
    {
        string once = JsonUtility.ToJson(bilesen);
        ayarla(bilesen);
        return JsonUtility.ToJson(bilesen) != once;
    }
}
