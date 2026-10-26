using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Oxanium SDF atlaslarını TÜRKÇE + İNGİLİZCE tam karakter kümesiyle yeniden
/// üretir.
///
/// NEDEN: ölçüldü (bkz. docs/DESIGN_TOKENS.md §6) — hiçbir statik atlasta
/// `ş Ş ğ Ğ İ ı` yok. Bugün bu harfler DİNAMİK FALLBACK üzerinden
/// LiberationSans ile çiziliyor; yani her Türkçe kelimede iki font karışıyor
/// ve `LiberationSans SDF - Fallback.asset` çalışma zamanında büyüyüp
/// depoda sürekli değişiyor.
///
/// YÖNTEM: asset'in kendisi KORUNUR (GUID aynı kalır, sahnedeki 100+ referans
/// bozulmaz). Atlas içeriği temizlenip hedef karakter kümesiyle yeniden
/// doldurulur, sonra mod tekrar Static'e çekilir.
///
/// IDEMPOTENT: atlasta eksik karakter yoksa dosyaya dokunmaz.
///
/// Menüden: NEXUM → Font Atlaslarını Üret
/// Batch:   -executeMethod NexumFontAtlas.Run
/// </summary>
public static class NexumFontAtlas
{
    private const string Tag = "NEXUM_FONTATLAS";

    /// <summary>Yeniden üretilecek atlaslar (metin rollerinde kullanılanlar).</summary>
    private static readonly string[] Targets =
    {
        "Assets/Fonts/Oxanium-Regular SDF.asset",
        "Assets/Fonts/Oxanium-Medium SDF.asset",
        "Assets/Fonts/Oxanium-SemiBold SDF.asset",
    };

    /// <summary>
    /// TABAN küme: ASCII görünür aralığı + Türkçe harfler + oyunda geçmesi
    /// muhtemel tipografik işaretler. Bir metin henüz yazılmamış olsa bile
    /// bunlar atlasta bulunur.
    ///
    /// Alt simge karakterleri (U+2080+) BİLEREK YOK: Oxanium'da yalnızca
    /// ₀–₄ var, ₅–₉ fontun kendisinde bulunmuyor. Formüller TMP &lt;sub&gt;
    /// etiketiyle yazılır (bkz. docs/DESIGN_TOKENS.md §8.4).
    /// </summary>
    public static string BaseSet
    {
        get
        {
            StringBuilder sb = new StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);   // ASCII
            sb.Append("çÇğĞıİöÖşŞüÜ");                               // Türkçe
            // ′ (U+2032) ve ″ (U+2033) BILEREK YOK: Oxanium TTF'inde
            // bulunmuyorlar (ölçüldü). Tabana konurlarsa atlas her koşuda
            // "eklenemeyen glif" raporlar ve idempotans bozulur.
            sb.Append("×–—’‘“”…€₺°±≤≥≈·");                           // tipografi
            return sb.ToString();
        }
    }

    /// <summary>
    /// Hedef küme = taban + ekranda GÖSTERİLEN metinlerde geçen her karakter
    /// (<see cref="NexumTextSources"/>). Böylece yeni bir metin eklendiğinde
    /// atlas kendiliğinden kapsar; unutulan bir karakter sessizce dinamik
    /// fallback'e düşmez.
    /// </summary>
    public static string CharacterSet
    {
        get
        {
            Dictionary<char, string> nerede;
            string kullanilan = NexumTextSources.Topla(out nerede);

            SortedSet<char> kume = new SortedSet<char>(BaseSet);
            foreach (char c in kullanilan)
            {
                // Alt simgeler kurala göre <sub> ile yazılıyor; fontta
                // bulunmayanları atlasa eklemeye çalışmak gürültü üretir.
                if (c >= '₀' && c <= '₉') continue;
                kume.Add(c);
            }

            return new string(kume.ToArray());
        }
    }

    /// <summary>Tek dokuda kalması için hedef atlas kenarı.</summary>
    private const int AtlasBoyutu = 1024;

    /// <summary>
    /// Atlas dokusunu ve asset'teki boyut alanlarını büyütür. GUID korunur;
    /// doku asset'in alt nesnesi olduğu için yerinde yeniden boyutlandırılır.
    /// </summary>
    private static void Buyut(TMP_FontAsset asset, int kenar)
    {
        SerializedObject so = new SerializedObject(asset);
        SerializedProperty w = so.FindProperty("m_AtlasWidth");
        SerializedProperty h = so.FindProperty("m_AtlasHeight");
        if (w != null && h != null && (w.intValue != kenar || h.intValue != kenar))
        {
            w.intValue = kenar;
            h.intValue = kenar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (asset.atlasTextures == null || asset.atlasTextures.Length == 0) return;

        Texture2D doku = asset.atlasTextures[0];
        if (doku == null || (doku.width == kenar && doku.height == kenar)) return;

        // Piksel icerigi ELLE TEMIZLENMEZ: atlas dokusu okunabilir degil
        // (SetPixels32 duser). Yeniden boyutlandirmak yeter; icerigi
        // ClearFontAssetData + TryAddCharacters dolduruyor.
        doku.Reinitialize(kenar, kenar, doku.format, false);
        EditorUtility.SetDirty(doku);
    }

    [MenuItem("NEXUM/Font Atlaslarını Üret")]
    public static void Run()
    {
        string hedef = CharacterSet;
        int toplamEklenen = 0;
        int dokunulmayan = 0;

        foreach (string yol in Targets)
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(yol);
            if (asset == null)
            {
                Debug.LogError($"{Tag}: asset yok -> {yol}");
                continue;
            }

            HashSet<uint> mevcut = new HashSet<uint>(asset.characterTable.Select(c => c.unicode));
            string eksik = new string(hedef.Where(c => !mevcut.Contains(c)).ToArray());

            if (eksik.Length == 0)
            {
                dokunulmayan++;
                Debug.Log($"{Tag}: {System.IO.Path.GetFileName(yol)} zaten tam " +
                          $"({asset.characterTable.Count} karakter) -> dokunulmadı");
                continue;
            }

            TMP_FontAsset ongoru = asset;
            AtlasPopulationMode eskiMod = ongoru.atlasPopulationMode;

            // 512x512 atlas 97 karakteri zor alıyor; hedef küme taşıyor ve
            // TMP ikinci doku açmaya kalkıp batch mode'da düşüyor. Atlas TEK
            // dokuda kalsın diye önce büyütülür, sonra sıfırdan doldurulur.
            Buyut(ongoru, AtlasBoyutu);

            ongoru.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            ongoru.isMultiAtlasTexturesEnabled = false;
            ongoru.ClearFontAssetData(true);

            string eklenemeyen;
            bool ok = ongoru.TryAddCharacters(hedef, out eklenemeyen, true);

            ongoru.atlasPopulationMode = AtlasPopulationMode.Static;

            EditorUtility.SetDirty(ongoru);
            foreach (Texture2D t in ongoru.atlasTextures)
            {
                if (t != null) EditorUtility.SetDirty(t);
            }

            int atlasW = ongoru.atlasTextures.Length > 0 && ongoru.atlasTextures[0] != null
                ? ongoru.atlasTextures[0].width : 0;
            int atlasH = ongoru.atlasTextures.Length > 0 && ongoru.atlasTextures[0] != null
                ? ongoru.atlasTextures[0].height : 0;

            toplamEklenen += eksik.Length;
            Debug.Log($"{Tag}: {System.IO.Path.GetFileName(yol)} " +
                      $"{mevcut.Count} -> {ongoru.characterTable.Count} karakter " +
                      $"(+{eksik.Length} eklendi), atlas {atlasW}x{atlasH}, " +
                      $"doku sayısı {ongoru.atlasTextures.Length}, mod {eskiMod} -> Static" +
                      (ok ? "" : $", EKLENEMEYEN {eklenemeyen.Length}"));

            if (!ok && !string.IsNullOrEmpty(eklenemeyen))
            {
                string liste = string.Join(" ", eklenemeyen.Select(c => "U+" + ((int)c).ToString("X4")));
                Debug.LogWarning($"{Tag}: fontta olmayan glifler -> {liste}");
            }
        }

        if (toplamEklenen > 0) AssetDatabase.SaveAssets();

        Debug.Log($"{Tag}: hedef küme {hedef.Length} karakter, " +
                  $"eklenen toplam {toplamEklenen}, dokunulmayan atlas {dokunulmayan}");
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>Doğrulama: her atlasta Türkçe harfler var mı?</summary>
    [MenuItem("NEXUM/Font Atlaslarını Doğrula")]
    public static void Verify()
    {
        const string turkce = "çÇğĞıİöÖşŞüÜ";
        int sorun = 0;

        foreach (string yol in Targets)
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(yol);
            if (asset == null) { sorun++; continue; }

            HashSet<uint> mevcut = new HashSet<uint>(asset.characterTable.Select(c => c.unicode));
            string eksik = new string(turkce.Where(c => !mevcut.Contains(c)).ToArray());
            int atlasW = asset.atlasTextures.Length > 0 ? asset.atlasTextures[0].width : 0;

            Debug.Log($"{Tag}_DOGRULA: {System.IO.Path.GetFileName(yol)} " +
                      $"{asset.characterTable.Count} karakter, atlas {atlasW}px, " +
                      $"Türkçe eksik: {(eksik.Length == 0 ? "(yok)" : eksik)}, " +
                      $"mod {asset.atlasPopulationMode}");
            if (eksik.Length > 0) sorun++;
        }

        Debug.Log($"{Tag}_DOGRULA: {(sorun == 0 ? "OK" : "SORUN " + sorun)}");
    }
}
