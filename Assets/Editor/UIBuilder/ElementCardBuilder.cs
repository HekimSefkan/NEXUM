using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ansiklopedi kartı: çember kitin diliyle, etiket ÇEMBERİN ALTINDA (3c/6).
///
/// İki kusur ölçüldü:
///   1) Çember <c>ring_progress</c> idi — kalın, parlak, simge ailesinin
///      çizgi diliyle alakasız. Artık <c>compound_ring</c>: simge ailesinin
///      çerçevesinin glifsiz hâli, AYNI üreteçten, aynı kenar kalınlığı ve
///      aynı ölçü ızgarasıyla (<c>Tools/generate_icons_family.py</c>).
///   2) Etiket çemberin İÇİNE basılıyordu ve kutusu 457 birimdi — hücre
///      230 birim, yani etiket komşu kartların üstüne taşıyordu. Artık
///      etiket çemberin ALTINDA, kendi satırında ve hücreye sığıyor.
///
/// Hücre 230x230 -> 230x300: etiket satırı için yer açıldı, sütun sayısı
/// (4) ve yatay ritim değişmedi.
///
/// IDEMPOTENT: prefab ve sahne değeri yerindeyse yazılmaz.
///
/// Menüden: NEXUM → Ansiklopedi Kartını Kur
/// Batch:   -executeMethod ElementCardBuilder.Run
/// </summary>
public static class ElementCardBuilder
{
    private const string Tag = "NEXUM_ANSKART";
    private const string PrefabPath = "Assets/Prefabs/ElementCard.prefab";
    private const string RingPath = "Assets/Art/UI/compound_ring_normal.png";

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string ContentPath =
        "Canvas/SafeAreaRoot/EncyclopediaPanel/Scroll View/Viewport/Content";

    /// <summary>
    /// Hücre: 3 sütun x 300 + 2 x 60 = 1020 <= 1063 (görüntü alanı).
    /// Sütun sayısı ve yatay ritim DEĞİŞMEDİ; yalnızca yükseklik arttı
    /// (300 -> 360), çünkü etiket artık çemberin altında kendi satırında.
    /// </summary>
    private static readonly Vector2 CellSize = new Vector2(300f, 360f);
    private static readonly Vector2 CellSpacing = new Vector2(60f, 40f);
    private const int Sutun = 3;

    private static readonly Vector2 CardSize = new Vector2(300f, 360f);
    private static readonly Vector2 RingSize = new Vector2(200f, 200f);
    private static readonly Vector2 RingPos = new Vector2(0f, 56f);

    /// <summary>Sembol çemberin ortasında; kutu çemberin iç alanını aşmaz.</summary>
    private static readonly Vector2 SymbolSize = new Vector2(150f, 90f);

    /// <summary>Etiket ÇEMBERİN ALTINDA, hücrenin içinde.</summary>
    private static readonly Vector2 NameSize = new Vector2(290f, 64f);
    private static readonly Vector2 NamePos = new Vector2(0f, -124f);

    /// <summary>Pişmiş punto: ölçümle (VariableTextFitTests) korunur.</summary>
    private const float SymbolFont = 44f;
    private const float NameFont = 24f;

    [MenuItem("NEXUM/Ansiklopedi Kartını Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        if (!File.Exists(PrefabPath))
        {
            Debug.LogError($"{Tag}: prefab yok -> {PrefabPath}");
            return;
        }

        NexumTheme tema = UITokens.Theme;
        GameObject kok = PrefabUtility.LoadPrefabContents(PrefabPath);
        bool degisti = false;

        degisti |= Rect(kok, CardSize, Vector2.zero);

        // Kök yalnızca DOKUNMA alanı: görseli yok, tıklamayı yakalar
        Image kokImage = kok.GetComponent<Image>();
        if (kokImage != null)
        {
            degisti |= Uygula(kokImage, i =>
            {
                i.sprite = null;
                i.color = new Color(1f, 1f, 1f, 0f);
                i.raycastTarget = true;
                i.type = Image.Type.Simple;
            });
        }

        GameObject ring = Cocuk(kok, "Ring", ref degisti);
        degisti |= Rect(ring, RingSize, RingPos);
        Image ringImage = ring.GetComponent<Image>();
        if (ringImage == null) { ringImage = ring.AddComponent<Image>(); degisti = true; }
        Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
        if (ringSprite == null) Debug.LogError($"{Tag}: çember sprite'ı yok -> {RingPath}");
        degisti |= Uygula(ringImage, i =>
        {
            i.sprite = ringSprite;
            i.type = Image.Type.Simple;
            i.preserveAspect = true;
            i.color = Color.white;
            i.raycastTarget = false;
        });
        ring.transform.SetSiblingIndex(0);   // metinler çemberin üstünde

        GameObject sembol = Cocuk(kok, "SymbolText", ref degisti);
        degisti |= Rect(sembol, SymbolSize, RingPos);
        degisti |= Metin(sembol, tema.typography.title.font, SymbolFont,
                         tema.palette.textPrimary);

        GameObject isim = Cocuk(kok, "NameText", ref degisti);
        degisti |= Rect(isim, NameSize, NamePos);
        degisti |= Metin(isim, tema.typography.body.font, NameFont,
                         tema.palette.textSecondary);

        if (degisti && !dryRun) PrefabUtility.SaveAsPrefabAsset(kok, PrefabPath);
        PrefabUtility.UnloadPrefabContents(kok);

        Debug.Log($"{Tag}: kart {(degisti ? (dryRun ? "DEĞİŞECEK" : "yazıldı") : "dokunulmadı")}");

        Hucre(dryRun);
    }

    /// <summary>Liste hücresi: etiket satırı için yükseklik.</summary>
    private static void Hucre(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("ElementCard", ScenePath, dryRun);

        GameObject content = b.FindObject(ContentPath);
        if (content == null)
        {
            b.Error($"ansiklopedi içeriği bulunamadı: {ContentPath}");
            b.End();
            return;
        }

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            b.Apply(content, grid, g =>
            {
                g.cellSize = CellSize;
                g.spacing = CellSpacing;
                // Sütun sayısı SABİT: esnek kısıtta sütun sayısı o anki
                // genişlikten türetiliyor ve ContentSizeFitter ile birlikte
                // içeriği kendi tercih ettiği genişliğe daraltıyordu.
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = Sutun;
                g.childAlignment = TextAnchor.UpperCenter;
                // Son satır görüntü alanının dibine yapışmasın
                g.padding = new RectOffset(0, 0, (int)CellSpacing.y, (int)CellSpacing.y * 2);
            });
        }

        b.End();
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

    private static bool Rect(GameObject go, Vector2 size, Vector2 pos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return false;

        bool fark = rt.anchorMin != UITokens.Center || rt.anchorMax != UITokens.Center
                    || rt.pivot != UITokens.Center
                    || rt.sizeDelta != size || rt.anchoredPosition != pos;
        if (!fark) return false;

        rt.anchorMin = UITokens.Center;
        rt.anchorMax = UITokens.Center;
        rt.pivot = UITokens.Center;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return true;
    }

    private static bool Metin(GameObject go, TMP_FontAsset font, float punto, Color renk)
    {
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        bool eklendi = false;
        if (tmp == null) { tmp = go.AddComponent<TextMeshProUGUI>(); eklendi = true; }

        return Uygula(tmp, t =>
        {
            t.font = font;
            t.fontSize = punto;
            t.enableAutoSizing = false;
            t.color = renk;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
        }) || eklendi;
    }

    private static bool Uygula<T>(T bilesen, System.Action<T> ayarla) where T : Component
    {
        string once = JsonUtility.ToJson(bilesen);
        ayarla(bilesen);
        return JsonUtility.ToJson(bilesen) != once;
    }
}
