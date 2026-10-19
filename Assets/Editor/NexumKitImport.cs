using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Metal kiti sprite'larını <c>Tools/ui-kit</c> altından <c>Assets/Art/UI</c>
/// ve <c>Assets/Art/Tiles</c> altına alır ve import ayarlarını kurar.
///
/// Kit Assets DIŞINDA üretiliyor (üreteç çıktısı, build'e girmesin diye);
/// kullanılacak olanlar buradan içeri alınır. Böylece hangi sprite'ın oyuna
/// girdiği tek yerden görülür.
///
/// 9-SLICE: kenar payları <c>Tools/ui-kit/manifest.json</c> içinde sprite
/// başına yazılı; buradan okunup <c>spriteBorder</c> alanına geçirilir.
/// Elle değer girilmez.
///
/// IMPORT KURALI (CLAUDE.md "Doku import kuralı"):
///   maxTextureSize = kullanımın 1,5 katından büyük en küçük 2'nin kuvveti
///   mipmap kapalı, Read/Write kapalı, UI sprite
///
/// IDEMPOTENT: dosya ve ayarlar aynıysa hiçbir şey yazmaz.
///
/// Menüden: NEXUM → Kit Sprite'larını İçeri Al
/// Batch:   -executeMethod NexumKitImport.Run
/// </summary>
public static class NexumKitImport
{
    private const string Tag = "NEXUM_KITIMPORT";

    private const string KitRoot = "Tools/ui-kit";
    private const string UiDir = "Assets/Art/UI";
    private const string TileDir = "Assets/Art/Tiles";

    /// <summary>Taş 200 birimde çizilir; 1440 genişlikte 266 px, ×1,5 = 400 → 512.</summary>
    private const int TileMaxSize = 512;

    /// <summary>Kit çerçeveleri en çok ~1000 birim genişlikte kullanılıyor.</summary>
    private const int UiMaxSize = 512;

    /// <summary>Oyuna girecek kit sprite'ları (varyant C).</summary>
    private static readonly string[] UiSprites =
    {
        "btn_primary_normal", "btn_primary_pressed", "btn_primary_disabled", "btn_primary_selected",
        "btn_secondary_normal", "btn_secondary_pressed", "btn_secondary_disabled", "btn_secondary_selected",
        "btn_ghost_normal", "btn_ghost_pressed", "btn_ghost_disabled", "btn_ghost_selected",
        "btn_destructive_normal", "btn_destructive_pressed", "btn_destructive_disabled", "btn_destructive_selected",
        "chip_normal", "chip_selected",
        "panel_lg_normal", "panel_md_normal", "panel_sm_normal", "modal_normal",
        "tab_normal", "tab_selected", "tab_disabled",
        "list_row_normal", "list_row_selected", "list_row_disabled",
        "progress_track_normal", "progress_fill_selected",
        "toast_normal", "tooltip_normal",
        "quiz_option_normal", "quiz_option_selected", "quiz_option_disabled",
        "input_field_normal", "input_field_disabled",
        "avatar_frame_normal", "badge_frame_normal",
        "icon_button_normal", "icon_button_pressed", "icon_button_disabled",
        "toggle_on_normal", "toggle_off_normal",
        "star_full_normal", "star_empty_normal",
        "level_node_locked", "level_node_open", "level_node_done",
        "slider_track", "slider_fill", "slider_knob",
    };

    [MenuItem("NEXUM/Kit Sprite'larını İçeri Al")]
    public static void Run()
    {
        string manifestYol = Path.Combine(KitRoot, "manifest.json");
        if (!File.Exists(manifestYol))
        {
            Debug.LogError($"{Tag}: manifest yok -> {manifestYol} (önce Tools/generate_ui_kit.py)");
            return;
        }

        Dictionary<string, Vector4> borderlar = BorderlariOku(manifestYol);

        Directory.CreateDirectory(UiDir);
        Directory.CreateDirectory(TileDir);

        int kopyalanan = 0, ayarlanan = 0, dokunulmayan = 0, eksik = 0;

        // --- UI kiti
        foreach (string ad in UiSprites)
        {
            string kaynak = Path.Combine(KitRoot, "C", ad + ".png");
            if (!File.Exists(kaynak))
            {
                Debug.LogError($"{Tag}: kit sprite'ı yok -> {kaynak}");
                eksik++;
                continue;
            }

            string hedef = UiDir + "/" + ad + ".png";
            if (Kopyala(kaynak, hedef)) kopyalanan++;

            Vector4 border;
            borderlar.TryGetValue(ad + ".png", out border);
            if (Ayarla(hedef, UiMaxSize, border)) ayarlanan++; else dokunulmayan++;
        }

        // --- taşlar: seviye × durum, tek çözünürlük (512)
        foreach (string kaynak in Directory.GetFiles(Path.Combine(KitRoot, "tiles"), "*_512.png"))
        {
            string ad = Path.GetFileNameWithoutExtension(kaynak).Replace("_512", "");
            string hedef = TileDir + "/" + ad + ".png";
            if (Kopyala(kaynak, hedef)) kopyalanan++;
            if (Ayarla(hedef, TileMaxSize, Vector4.zero)) ayarlanan++; else dokunulmayan++;
        }

        if (kopyalanan > 0 || ayarlanan > 0) AssetDatabase.Refresh();

        Debug.Log($"{Tag}: kopyalanan {kopyalanan}, import ayarı {ayarlanan}, " +
                  $"dokunulmayan {dokunulmayan}, eksik {eksik}");
        Debug.Log($"{Tag}: {(eksik == 0 ? "OK" : "FAIL")}");
    }

    /// <summary>İçerik farklıysa kopyalar; aynıysa dosyaya dokunmaz.</summary>
    private static bool Kopyala(string kaynak, string hedef)
    {
        if (File.Exists(hedef))
        {
            byte[] a = File.ReadAllBytes(kaynak);
            byte[] b = File.ReadAllBytes(hedef);
            if (a.Length == b.Length && a.SequenceEqual(b)) return false;
        }

        File.Copy(kaynak, hedef, true);
        return true;
    }

    /// <summary>Import ayarlarını kurar; zaten doğruysa dokunmaz.</summary>
    private static bool Ayarla(string yol, int maxSize, Vector4 border)
    {
        AssetDatabase.ImportAsset(yol, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (imp == null) return false;

        bool degisti = false;

        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; degisti = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; degisti = true; }
        if (imp.mipmapEnabled) { imp.mipmapEnabled = false; degisti = true; }
        if (imp.isReadable) { imp.isReadable = false; degisti = true; }
        if (imp.alphaIsTransparency != true) { imp.alphaIsTransparency = true; degisti = true; }
        if (imp.maxTextureSize != maxSize) { imp.maxTextureSize = maxSize; degisti = true; }
        if (imp.spriteBorder != border) { imp.spriteBorder = border; degisti = true; }

        // Atlasa girecekleri çift sıkıştırmadan korumak için Uncompressed
        TextureImporterPlatformSettings varsayilan = imp.GetDefaultPlatformTextureSettings();
        if (varsayilan.format != TextureImporterFormat.RGBA32 || varsayilan.maxTextureSize != maxSize)
        {
            varsayilan.format = TextureImporterFormat.RGBA32;
            varsayilan.maxTextureSize = maxSize;
            varsayilan.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SetPlatformTextureSettings(varsayilan);
            degisti = true;
        }

        if (degisti)
        {
            imp.SaveAndReimport();
        }
        return degisti;
    }

    /// <summary>manifest.json içindeki sprite başına kenar paylarını okur.</summary>
    private static Dictionary<string, Vector4> BorderlariOku(string yol)
    {
        Dictionary<string, Vector4> sonuc = new Dictionary<string, Vector4>();
        string metin = File.ReadAllText(yol);

        // Basit tarama: "dosya.png": { ... "border": [l, b, r, t] }
        int i = 0;
        while (true)
        {
            int adBas = metin.IndexOf(".png\"", i);
            if (adBas < 0) break;

            int tirnak = metin.LastIndexOf('"', adBas);
            string ad = metin.Substring(tirnak + 1, adBas + 4 - tirnak - 1);

            int bBas = metin.IndexOf("\"border\"", adBas);
            if (bBas < 0) break;
            int kose = metin.IndexOf('[', bBas);
            int kapa = metin.IndexOf(']', kose);
            string[] parca = metin.Substring(kose + 1, kapa - kose - 1).Split(',');

            if (parca.Length == 4)
            {
                sonuc[ad] = new Vector4(
                    float.Parse(parca[0].Trim()), float.Parse(parca[1].Trim()),
                    float.Parse(parca[2].Trim()), float.Parse(parca[3].Trim()));
            }

            i = kapa;
        }

        Debug.Log($"{Tag}: manifest'ten {sonuc.Count} sprite kenar payı okundu");
        return sonuc;
    }
}
