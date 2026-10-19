using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tarif defteri mantığı: hangi tarif keşfedildi, geriye dönük doldurma.
///
/// Keşif kuralı: oyuncu o birleşmeyi ilk kez yaptığında tarif keşfedilmiş sayılır.
/// </summary>
public static class RecipeBook
{
    private const string Tag = "NEXUM_RECIPEBOOK";

    /// <summary>Yeni bir tarif keşfedildiğinde tetiklenir (anahtar).</summary>
    public static event System.Action<string> RecipeDiscovered;

    public static bool IsDiscovered(string key)
    {
        return !string.IsNullOrEmpty(key) && SaveService.Data.discoveredRecipes.Contains(key);
    }

    /// <summary>
    /// Tarifi keşfedilmiş olarak kaydeder. YENİ bir keşifse true döner;
    /// aynı tarif ikinci kez keşif sayılmaz.
    /// </summary>
    public static bool Discover(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (SaveService.Data.discoveredRecipes.Contains(key)) return false;

        SaveService.Data.discoveredRecipes.Add(key);
        SaveService.MarkDirty();

        RecipeDiscovered?.Invoke(key);
        return true;
    }

    public static int DiscoveredCount
    {
        get { return SaveService.Data.discoveredRecipes.Count; }
    }

    public static int TotalCount
    {
        get
        {
            RecipeCatalog catalog = RecipeCatalog.Load();
            return catalog != null ? catalog.entries.Count : 0;
        }
    }

    // ------------------------------------------------- geriye dönük doldurma

    /// <summary>
    /// Mevcut oyuncular boş defterle karşılaşmasın: açılan bölümlerden
    /// ZORUNLU olarak kullanılmış tarifler hesaplanıp keşfedilmiş sayılır.
    ///
    /// YÖNTEM (üretim grafiği, muhafazakâr):
    ///  1. Açılan her bölüm (0 .. maxLevelUnlocked-1) için hedef bileşikler
    ///     "gerekli" kümesine konur.
    ///  2. Gerekli bir bileşiği üreten tarifler bulunur. TEK bir tarif
    ///     üretiyorsa o tarif ZORUNLUDUR: işaretlenir ve girdileri de gerekli
    ///     kümesine eklenir (zincir yukarı doğru yürür).
    ///  3. Bir bileşiği birden fazla tarif üretebiliyorsa hangisinin
    ///     kullanıldığı bilinemez: HİÇBİRİ sayılmaz, o dal orada biter.
    ///  4. Temel elementler tarif gerektirmez (TileLevels.IsBasic).
    ///
    /// Kural muhafazakârdır: oyuncunun yapmamış olabileceği bir tarifi asla
    /// keşfedilmiş saymaz. Fazladan keşif vermektense eksik vermeyi seçer.
    /// </summary>
    public static List<string> ComputeBackfill(int maxLevelUnlocked)
    {
        List<string> sonuc = new List<string>();

        RecipeCatalog catalog = RecipeCatalog.Load();
        if (catalog == null) return sonuc;

        int acilan = Mathf.Clamp(maxLevelUnlocked, 0, catalog.levels.Count);

        HashSet<string> gerekli = new HashSet<string>();
        for (int i = 0; i < acilan; i++)
        {
            foreach (string hedef in catalog.levels[i].goals) gerekli.Add(hedef);
        }

        HashSet<string> islenen = new HashSet<string>();
        Queue<string> kuyruk = new Queue<string>(gerekli);

        while (kuyruk.Count > 0)
        {
            string bilesik = kuyruk.Dequeue();
            if (!islenen.Add(bilesik)) continue;

            // Temel element: tek kaynak TileLevels (tarif grafiginden turetilir)
            if (TileLevels.IsBasic(bilesik)) continue;

            List<RecipeCatalog.Entry> ureticiler = new List<RecipeCatalog.Entry>();
            foreach (RecipeCatalog.Entry e in catalog.entries)
            {
                if (e.productName == bilesik) ureticiler.Add(e);
            }

            // Belirsiz: birden fazla uretici varsa hangisi yapildi bilinemez
            if (ureticiler.Count != 1) continue;

            RecipeCatalog.Entry zorunlu = ureticiler[0];
            if (!sonuc.Contains(zorunlu.key)) sonuc.Add(zorunlu.key);

            kuyruk.Enqueue(zorunlu.inputAName);
            kuyruk.Enqueue(zorunlu.inputBName);
        }

        return sonuc;
    }

    /// <summary>
    /// Göç adımı: defter hiç doldurulmadıysa geriye dönük doldurmayı uygular.
    /// Yalnızca bir kez çalışır (<c>recipeBackfillDone</c>).
    /// </summary>
    public static int RunBackfillOnce()
    {
        if (SaveService.Data.recipeBackfillDone == 1) return 0;

        List<string> zorunlu = ComputeBackfill(SaveService.Data.maxLevelUnlocked);
        int eklenen = 0;

        foreach (string key in zorunlu)
        {
            if (SaveService.Data.discoveredRecipes.Contains(key)) continue;
            SaveService.Data.discoveredRecipes.Add(key);
            eklenen++;
        }

        SaveService.Data.recipeBackfillDone = 1;
        SaveService.SaveNow();

        Debug.Log($"{Tag}: geriye donuk doldurma -> acilan bolum " +
                  $"{SaveService.Data.maxLevelUnlocked}, zorunlu tarif {zorunlu.Count}, " +
                  $"eklenen {eklenen}, toplam defter {DiscoveredCount}");
        return eklenen;
    }
}
