using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Taş seviyesinin TEK KAYNAĞI.
///
/// Seviye elle atanmaz; tarif grafiğinden türetilir. Her taşın "üretim
/// derinliği" = o taşı elde etmek için gereken en kısa birleşme zinciri:
///
///   derinlik(temel element) = 0
///   derinlik(ürün)          = min( 1 + max(derinlik(girdiler)) )   tüm tarifler üzerinden
///
/// "min" çünkü bir bileşik birden fazla yoldan üretilebiliyorsa EN KISA yol
/// esas alınır; "max" çünkü bir tarif, en derin girdisi hazır olmadan
/// yapılamaz.
///
/// Seviye eşikleri <see cref="LevelThresholds"/> tablosundadır ve seviye
/// sayısına sabitlenmemiştir: dördüncü seviye tabloya tek satır eklenerek
/// gelir.
///
/// ÖNEMLİ: Bu sınıf yalnızca BİLGİ verir. Doğma oranı, puanlama, katalizör
/// ve zorluk bu bilgiye göre DEĞİŞMEZ; mevcut davranış birebir korunur
/// (bkz. Assets/Tests/PlayMode/TileLevelTests.cs).
/// </summary>
public static class TileLevels
{
    private const string Tag = "NEXUM_TILELEVEL";

    /// <summary>
    /// Seviye eşikleri: bir taşın seviyesi, derinliğinin geçtiği eşik
    /// sayısıdır. {0, 1, 2, 3} → seviye 1 = derinlik 0, seviye 2 = derinlik 1,
    /// seviye 3 = derinlik 2, seviye 4 = derinlik 3+.
    ///
    /// Dağılım (21 tarif, 26 taş): 8 / 10 / 4 / 4 — alt seviyeler kalabalık,
    /// üst seviyeler nadir. Beşinci seviye tabloya tek değer eklenerek gelir;
    /// kod değişmez.
    /// </summary>
    public static readonly int[] LevelThresholds = { 0, 1, 2, 3 };

    /// <summary>Derinliği bilinmeyen taş.</summary>
    public const int UnknownDepth = -1;

    private static Dictionary<string, int> depths;

    /// <summary>Test ve katalog yeniden üretimi için: hesabı sıfırlar.</summary>
    public static void Clear()
    {
        depths = null;
    }

    // ------------------------------------------------------------- türetme

    private static Dictionary<string, int> Depths
    {
        get
        {
            if (depths == null) depths = Compute();
            return depths;
        }
    }

    private static Dictionary<string, int> Compute()
    {
        Dictionary<string, int> sonuc = new Dictionary<string, int>();

        RecipeCatalog catalog = RecipeCatalog.Load();
        if (catalog == null)
        {
            Debug.LogError($"{Tag}: RecipeCatalog yok; seviye türetilemiyor");
            return sonuc;
        }

        // 1) Üreticisi olmayan her taş temel elementtir (derinlik 0).
        HashSet<string> urun = new HashSet<string>();
        foreach (RecipeCatalog.Entry e in catalog.entries) urun.Add(e.productName);

        foreach (RecipeCatalog.TileInfo t in catalog.tiles)
        {
            if (!urun.Contains(t.name)) sonuc[t.name] = 0;
        }

        // Katalogda taş listesi yoksa girdilerden topla (eski asset'e dayanıklılık)
        if (catalog.tiles.Count == 0)
        {
            foreach (RecipeCatalog.Entry e in catalog.entries)
            {
                if (!urun.Contains(e.inputAName)) sonuc[e.inputAName] = 0;
                if (!urun.Contains(e.inputBName)) sonuc[e.inputBName] = 0;
            }
        }

        // 2) Sabit noktaya kadar yinele: her tur daha kısa bir yol bulunabilir.
        //    Tur sayısı tarif sayısıyla sınırlı (döngülü tabloda sonsuza gitmez).
        for (int tur = 0; tur <= catalog.entries.Count; tur++)
        {
            bool degisti = false;

            foreach (RecipeCatalog.Entry e in catalog.entries)
            {
                int a, b;
                if (!sonuc.TryGetValue(e.inputAName, out a)) continue;
                if (!sonuc.TryGetValue(e.inputBName, out b)) continue;

                int aday = 1 + Mathf.Max(a, b);
                int mevcut;
                if (!sonuc.TryGetValue(e.productName, out mevcut) || aday < mevcut)
                {
                    sonuc[e.productName] = aday;
                    degisti = true;
                }
            }

            if (!degisti) break;
        }

        return sonuc;
    }

    // ----------------------------------------------------------- sorgular

    /// <summary>Taşın üretim derinliği; bilinmiyorsa <see cref="UnknownDepth"/>.</summary>
    public static int Depth(string tileName)
    {
        if (string.IsNullOrEmpty(tileName)) return UnknownDepth;

        int d;
        return Depths.TryGetValue(tileName.Replace("(Clone)", ""), out d) ? d : UnknownDepth;
    }

    public static int Depth(ElementData data)
    {
        return Depth(TileName(data));
    }

    /// <summary>Derinlikten seviye: eşik tablosunu geçen eşik sayısı (en az 1).</summary>
    public static int LevelForDepth(int depth)
    {
        if (depth == UnknownDepth) return 1;

        int seviye = 0;
        foreach (int esik in LevelThresholds)
        {
            if (depth >= esik) seviye++;
        }
        return Mathf.Max(1, seviye);
    }

    public static int Level(string tileName)
    {
        return LevelForDepth(Depth(tileName));
    }

    public static int Level(ElementData data)
    {
        return LevelForDepth(Depth(data));
    }

    /// <summary>Temel element mi? (üreticisi olmayan taş, derinlik 0)</summary>
    public static bool IsBasic(string tileName)
    {
        return Depth(tileName) == 0;
    }

    public static bool IsBasic(ElementData data)
    {
        return Depth(data) == 0;
    }

    /// <summary>Bilinen tüm taş adları (test ve rapor için).</summary>
    public static IEnumerable<string> KnownTiles
    {
        get { return Depths.Keys; }
    }

    private static string TileName(ElementData data)
    {
        if (data == null) return null;

        RecipeCatalog catalog = RecipeCatalog.Load();
        if (catalog == null) return null;

        RecipeCatalog.TileInfo t = catalog.FindTileByData(data);
        return t != null ? t.name : null;
    }
}
