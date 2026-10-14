using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// Tepkime okunu TMP sprite'i olarak kurar.
///
/// Neden: "→" (U+2192) projenin hiçbir fontunda yok (Oxanium, Orbitron,
/// LiberationSans) ve atlasların hepsi statik. Ok bu yüzden bir sprite.
///
/// Görsel <c>Tools/generate_arrow.py</c> ile üretilir (beyaz, tek renk —
/// TMP tonlayabilsin diye). Bu script onu TMP Sprite Asset'ine çevirir.
///
/// Asset <c>Resources/Sprite Assets/</c> altında durur: TMP'nin
/// <c>&lt;sprite="NexumSymbols" name="arrow"&gt;</c> aramasının baktığı yer
/// burasıdır, böylece denklem, ansiklopedi ve tarif defteri TEK kaynaktan
/// beslenir — her bileşene ayrı ayrı bağlamak gerekmez.
///
/// IDEMPOTENT: asset varsa yeniden oluşturmaz, ölçüleri denetler.
///
/// Menüden: NEXUM → Sembol Sprite'larını Kur
/// Batch mode: -executeMethod NexumSymbolSprites.Run
/// </summary>
public static class NexumSymbolSprites
{
    private const string Tag = "NEXUM_SYMBOLS";

    public const string AssetName = "NexumSymbols";
    public const string ArrowName = "arrow";
    private const string TexturePath = "Assets/Art/Figures/arrow_reaction.png";
    private const string AssetDir = "Assets/Resources/Sprite Assets";
    private const string AssetPath = AssetDir + "/" + AssetName + ".asset";

    // arrow_reaction.png 128x128; opak alan generate_arrow.py tarafından raporlanır.
    // GlyphRect ALTTAN sayar: üstten y 42..75 -> alttan 53..86.
    private const int TextureSize = 128;
    private const int RectX = 10;
    private const int RectY = 53;
    private const int RectW = 108;
    private const int RectH = 33;

    // Ölçek: okun yüksekliği punto × (RectH / pointSize) kadar olur.
    // ÖLÇÜMLE BULUNDU (ArrowCalibrationTests taraması, pointSize 146):
    //   yükseklik%  = 22.6 × scale
    //   merkez farkı% = (-36.0 - 9.0×scale) + (0.68×scale) × bearingY
    // Hedef: ok yüksekliği punto'nun %40'ı, harflerin optik ortasında (fark 0).
    //   -> scale 1.77, bearingY 40.2
    // bearingY ilk turda 43.1 idi; gerçek denklem metninde ("H + H [ok] H2")
    // ölçülen sapma +%3,5 çıktı ve model gereği 3,5 / 1,2036 = 2,9 birim
    // düşürüldü. Değerler ArrowCalibrationTests ile her koşuda doğrulanır
    // (dört canvas boyutu × iki dil).
    private const float PointSize = 146f;
    private const float CharacterScale = 1.77f;

    // Taban hizası: ok, harflerin optik ortasına otursun diye yukarı
    // kaydırılır. Değer ölçümle doğrulanır (ArrowCalibrationTests).
    private const float BearingY = 40.2f;

    [MenuItem("NEXUM/Sembol Sprite'larını Kur")]
    public static void Run()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (texture == null)
        {
            Debug.LogError($"{Tag}: görsel yok: {TexturePath} (python Tools/generate_arrow.py)");
            return;
        }

        EnsureTextureImport();
        Directory.CreateDirectory(AssetDir);

        TMP_SpriteAsset asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetPath);
        bool created = false;

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            created = true;
        }

        asset.spriteSheet = texture;

        // faceInfo salt okunur: serialize edilmiş alanlardan kurulur
        SerializedObject so = new SerializedObject(asset);
        SerializedProperty face = so.FindProperty("m_FaceInfo");
        if (face != null)
        {
            SetIfExists(face, "m_PointSize", (int)PointSize);
            SetIfExists(face, "m_Scale", 1f);
            SetIfExists(face, "m_LineHeight", PointSize);
            SetIfExists(face, "m_AscentLine", PointSize);
            SetIfExists(face, "m_Baseline", 0f);
            SetIfExists(face, "m_DescentLine", 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"{Tag}: m_FaceInfo alanı bulunamadı; TMP sürümü farklı olabilir");
        }

        asset.spriteGlyphTable.Clear();
        asset.spriteCharacterTable.Clear();

        TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
        {
            index = 0,
            metrics = new GlyphMetrics(RectW, RectH, 0f, BearingY, RectW + 10f),
            glyphRect = new GlyphRect(RectX, RectY, RectW, RectH),
            scale = 1f,
            atlasIndex = 0,
        };
        asset.spriteGlyphTable.Add(glyph);

        TMP_SpriteCharacter character = new TMP_SpriteCharacter(0, glyph)
        {
            name = ArrowName,
            scale = CharacterScale,
        };
        asset.spriteCharacterTable.Add(character);

        EnsureMaterial(asset, texture);
        asset.UpdateLookupTables();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"{Tag}: sprite asset {(created ? "oluşturuldu" : "güncellendi")} -> {AssetPath}");
        Debug.Log($"{Tag}: glyphRect {RectX},{RectY} {RectW}x{RectH}  pointSize {PointSize}  " +
                  $"scale {CharacterScale}  bearingY {BearingY}");
        Debug.Log($"{Tag}: OK");
    }

    private static void SetIfExists(SerializedProperty parent, string name, int value)
    {
        SerializedProperty p = parent.FindPropertyRelative(name);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        else p.intValue = value;
    }

    private static void SetIfExists(SerializedProperty parent, string name, float value)
    {
        SerializedProperty p = parent.FindPropertyRelative(name);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(value);
        else p.floatValue = value;
    }

    /// <summary>Sprite dokusu: sıkıştırılmamış, mipmap kapalı, tam alfa.</summary>
    private static void EnsureTextureImport()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
        if (importer.alphaIsTransparency == false) { importer.alphaIsTransparency = true; changed = true; }
        if (importer.maxTextureSize != 256) { importer.maxTextureSize = 256; changed = true; }

        TextureImporterPlatformSettings def = importer.GetDefaultPlatformTextureSettings();
        if (def.format != TextureImporterFormat.RGBA32 || def.textureCompression != TextureImporterCompression.Uncompressed)
        {
            def.format = TextureImporterFormat.RGBA32;
            def.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(def);
            changed = true;
        }

        if (changed) importer.SaveAndReimport();
    }

    private static void EnsureMaterial(TMP_SpriteAsset asset, Texture2D texture)
    {
        if (asset.material == null)
        {
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null)
            {
                Debug.LogError($"{Tag}: TextMeshPro/Sprite shader'ı bulunamadı");
                return;
            }

            Material material = new Material(shader) { name = AssetName + " Material" };
            material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            asset.material = material;
            AssetDatabase.AddObjectToAsset(material, asset);
        }
        else
        {
            asset.material.SetTexture(ShaderUtilities.ID_MainTex, texture);
        }
    }
}
