using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Her birleşmede tepkime denklemini birleşmenin olduğu hücrenin üstünde
/// gösterir: "H<sub>2</sub> + O -> H<sub>2</sub>O".
///
/// HAVUZ: her birleşmede Instantiate/Destroy yapılmaz. Sahnede önceden kurulmuş
/// <see cref="PoolSize"/> adet TMP metni dönüşümlü kullanılır; oyun sırasında
/// hiç çöp üretilmez.
///
/// ÇAKIŞMA: aynı hamlede birden fazla birleşme olursa gösterimler sıraya
/// alınmaz (oyunu yavaşlatırdı) ve tek kutuda birleştirilmez (hangi hücrede ne
/// olduğu kaybolurdu). Hepsi aynı anda çıkar; kutular çakışacaksa sonradan
/// gelen bir satır yukarı itilir.
///
/// Sahnedeki obje UIBuilder ile kurulur: <c>ReactionEquationBuilder</c>.
/// </summary>
public class ReactionEquationDisplay : MonoBehaviour
{
    /// <summary>Bir hamlede olabilecek en çok birleşme: 4 satır × 2 çift.</summary>
    public const int PoolSize = 8;

    /// <summary>Kutular arası dikey boşluk (referans birimi).</summary>
    private const float StackGap = 6f;

    /// <summary>Hücrenin kaç birim üstünde belirsin.</summary>
    private const float RiseAbove = 60f;

    /// <summary>Sönerken kaç birim yükselsin.</summary>
    private const float FloatUp = 34f;

    /// <summary>Kombo yazısının bandına girmesin: üst sınır (referans birimi).</summary>
    private const float TopLimit = 270f;

    public static ReactionEquationDisplay Instance;

    [Tooltip("Renkler buradan gelir (UIBuilder bağlar).")]
    public NexumTheme theme;

    private TMP_Text[] pool;
    private readonly List<Rect> activeRects = new List<Rect>(PoolSize);
    private int next;

    private void Awake()
    {
        Instance = this;
        pool = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in pool) t.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Oyuncu kapattıysa hiç gösterme.</summary>
    public static bool Enabled
    {
        get { return SaveService.Data.showReactionEquations == 1; }
    }

    /// <summary>Bir hamlenin başında çağrılır: çakışma defteri temizlenir.</summary>
    public void BeginShift()
    {
        activeRects.Clear();
    }

    /// <summary>Tarifin denklemini birleşme hücresinin üstünde gösterir.</summary>
    public void Show(MergeRecipe recipe, Vector3 worldPosition)
    {
        if (!Enabled || pool == null || pool.Length == 0) return;

        TMP_Text label = pool[next];
        next = (next + 1) % pool.Length;

        label.text = BuildEquation(recipe);
        label.gameObject.SetActive(true);

        RectTransform rect = label.rectTransform;
        rect.DOKill(true);
        label.DOKill(true);

        float scale = GetCanvasScale();
        Vector3 target = Place(rect, worldPosition, scale);
        rect.position = target;
        rect.localScale = Vector3.zero;

        float appear = theme != null ? theme.motion.durationShort : 0.15f;
        float hold = theme != null ? theme.motion.durationLong : 0.5f;
        float fade = theme != null ? theme.motion.durationFade : 0.35f;

        label.alpha = 1f;

        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOScale(Vector3.one, appear).SetEase(Ease.OutBack));
        seq.AppendInterval(hold);
        seq.Append(label.DOFade(0f, fade).SetEase(Ease.InQuad));
        seq.Join(rect.DOMoveY(target.y + FloatUp * scale, fade).SetEase(Ease.OutQuad));
        seq.OnComplete(() => label.gameObject.SetActive(false));
    }

    /// <summary>"H&lt;sub&gt;2&lt;/sub&gt; + O -&gt; H&lt;sub&gt;2&lt;/sub&gt;O" — girdiler sönük, ürün vurgulu.</summary>
    public string BuildEquation(MergeRecipe recipe)
    {
        string dim = ColorTag(theme != null ? theme.palette.textSecondary : Color.gray);
        string bright = ColorTag(theme != null ? theme.palette.primary : Color.cyan);

        return Loc.Format(CodeStrings.ReactionEquation,
            dim + Symbol(recipe.element1) + "</color>",
            dim + Symbol(recipe.element2) + "</color>",
            bright + Symbol(recipe.resultPrefab) + "</color>");
    }

    private static string ColorTag(Color c)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">";
    }

    /// <summary>Sembol ElementData'dan gelir; yoksa prefab adına düşülür.</summary>
    public static string Symbol(GameObject tilePrefab)
    {
        if (tilePrefab == null) return "?";

        TileInteraction tile = tilePrefab.GetComponent<TileInteraction>();
        if (tile != null && tile.myElementData != null && !string.IsNullOrEmpty(tile.myElementData.symbol))
        {
            return tile.myElementData.symbol;
        }
        return tilePrefab.name.Replace("Tile_", "");
    }

    // ------------------------------------------------------------- yerleşim

    private float GetCanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null ? canvas.scaleFactor : 1f;
    }

    /// <summary>
    /// Hücrenin üstü; kombo bandına girmez ve daha önce yerleşmiş bir kutuyla
    /// çakışıyorsa bir satır yukarı (yer yoksa aşağı) itilir.
    /// </summary>
    private Vector3 Place(RectTransform rect, Vector3 worldPosition, float scale)
    {
        float width = rect.rect.width * scale;
        float height = rect.rect.height * scale;
        float step = (rect.rect.height + StackGap) * scale;

        float half = width * 0.5f;

        // Yatayda GRID sınırları esas: kutu tahtanın dışına taşmamalı.
        // Tahta yoksa (test/ön izleme) ekran kenarına düşülür.
        float solSinir = half;
        float sagSinir = Screen.width - half;
        RectTransform board = GridManager.Instance != null ? GridManager.Instance.BoardRect : null;
        if (board != null)
        {
            Vector3[] corners = new Vector3[4];
            board.GetWorldCorners(corners);
            solSinir = corners[0].x + half;
            sagSinir = corners[2].x - half;
            if (solSinir > sagSinir)
            {
                // Kutu tahtadan geniş: ortala
                solSinir = sagSinir = (corners[0].x + corners[2].x) * 0.5f;
            }
        }

        float x = Mathf.Clamp(worldPosition.x, solSinir, sagSinir);
        float y = worldPosition.y + RiseAbove * scale;
        float ceiling = Screen.height * 0.5f + TopLimit * scale;

        // Çakışma varsa yukarı it; tavana dayanırsa aşağı doğru dene
        for (int i = 0; i < PoolSize; i++)
        {
            Rect candidate = new Rect(x - half, y - height * 0.5f, width, height);
            if (!Overlaps(candidate)) break;
            y += (y + step <= ceiling) ? step : -step;
        }

        y = Mathf.Min(y, ceiling);
        activeRects.Add(new Rect(x - half, y - height * 0.5f, width, height));
        return new Vector3(x, y, 0f);
    }

    private bool Overlaps(Rect candidate)
    {
        foreach (Rect r in activeRects)
        {
            if (r.Overlaps(candidate)) return true;
        }
        return false;
    }

    /// <summary>Test için: o an görünen gösterim sayısı.</summary>
    public int ActiveCount
    {
        get
        {
            if (pool == null) return 0;
            int n = 0;
            foreach (TMP_Text t in pool) if (t.gameObject.activeSelf) n++;
            return n;
        }
    }

    /// <summary>Test için: görünen gösterimlerin ekran dikdörtgenleri.</summary>
    public List<Rect> ActiveScreenRects
    {
        get
        {
            List<Rect> list = new List<Rect>();
            if (pool == null) return list;

            float scale = GetCanvasScale();
            foreach (TMP_Text t in pool)
            {
                if (!t.gameObject.activeSelf) continue;
                RectTransform rt = t.rectTransform;
                float w = rt.rect.width * scale;
                float h = rt.rect.height * scale;
                list.Add(new Rect(rt.position.x - w * 0.5f, rt.position.y - h * 0.5f, w, h));
            }
            return list;
        }
    }
}
