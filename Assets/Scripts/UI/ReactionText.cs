using TMPro;
using UnityEngine;

/// <summary>
/// Tepkime metninin TEK KAYNAĞI: "A + B [ok] C".
///
/// Üç yerde gösteriliyor — birleşme anındaki denklem gösterimi
/// (<see cref="ReactionEquationDisplay"/>), tarif defteri
/// (<see cref="RecipeBookPanel"/>) ve ansiklopedi detay kartı
/// (<see cref="EncyclopediaManager"/>). Üçü de bu sınıftan geçer; üç ayrı
/// string kalıbı YOK.
///
/// Ok karakteri "→" (U+2192) proje fontlarının HİÇBİRİNDE yok ve atlaslar
/// statik. Bu yüzden ok bir TMP sprite'ı olarak gömülüdür
/// (<c>NexumSymbolSprites</c> üretir). Sprite yüklenemezse sessizce değil,
/// bir kez hata loglanarak düz metin ("->") kalıbına düşülür.
///
/// Kalıpların kendisi sözlükte (<c>reaction.*</c>): dil değişince yalnızca
/// sözlük değişir, kod değişmez.
/// </summary>
public static class ReactionText
{
    /// <summary>Ok sprite asset'i (Resources yolu).</summary>
    public const string SpriteAssetResource = "Sprite Assets/NexumSymbols";

    private static TMP_SpriteAsset arrowAsset;
    private static bool arrowChecked;

    /// <summary>Ok sprite'ı yüklenebiliyor mu? Yüklenemiyorsa bir kez loglanır.</summary>
    public static bool ArrowAvailable
    {
        get
        {
            if (!arrowChecked)
            {
                arrowChecked = true;
                arrowAsset = Resources.Load<TMP_SpriteAsset>(SpriteAssetResource);
                if (arrowAsset == null)
                {
                    Debug.LogError("NEXUM_REACTION: ok sprite'ı yüklenemedi " +
                                   "(Resources/" + SpriteAssetResource + "); düz metin oka düşülüyor");
                }
            }
            return arrowAsset != null;
        }
    }

    /// <summary>Sprite asset'i (TMP bileşenlerine atanır). Yoksa null.</summary>
    public static TMP_SpriteAsset SpriteAsset
    {
        get { return ArrowAvailable ? arrowAsset : null; }
    }

    /// <summary>Test için: yüklenme denetimini sıfırlar.</summary>
    public static void ResetArrowCheck()
    {
        arrowChecked = false;
        arrowAsset = null;
    }

    /// <summary>Metni gösterecek TMP'ye sprite asset'ini bağlar.</summary>
    public static void Bind(TMP_Text text)
    {
        if (text == null) return;
        TMP_SpriteAsset asset = SpriteAsset;
        if (asset != null) text.spriteAsset = asset;
    }

    // ------------------------------------------------------------- kalıplar

    /// <summary>"A + B" — girdilerin birleşimi.</summary>
    public static string Inputs(string a, string b)
    {
        return Loc.Format(CodeStrings.ReactionInputs, a, b);
    }

    /// <summary>"A + B [ok] C" — girdiler ayrı ayrı verilir.</summary>
    public static string Equation(string a, string b, string product)
    {
        return Equation(Inputs(a, b), product);
    }

    /// <summary>
    /// "&lt;girdiler&gt; [ok] C" — girdiler zaten birleşikse (ör. ElementData.recipe).
    /// </summary>
    public static string Equation(string inputs, string product)
    {
        string key = ArrowAvailable ? CodeStrings.ReactionEquation
                                    : CodeStrings.ReactionEquationFallback;
        return Loc.Format(key, inputs, product);
    }

    /// <summary>Girdiler sönük, ürün vurgulu renkli denklem.</summary>
    public static string Colored(string a, string b, string product,
                                 Color inputColor, Color productColor)
    {
        return Equation(Tint(a, inputColor), Tint(b, inputColor), Tint(product, productColor));
    }

    /// <summary>Girdileri birleşik alan renkli biçim (ansiklopedi satırı).</summary>
    public static string ColoredFromInputs(string inputs, string product,
                                           Color inputColor, Color productColor)
    {
        return Equation(Tint(inputs, inputColor), Tint(product, productColor));
    }

    private static string Tint(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }

    // --------------------------------------------------------------- semboller

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
}
