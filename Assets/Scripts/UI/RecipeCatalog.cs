using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 21 tarifin ve bölüm hedeflerinin sahne dışından okunabilen kopyası.
///
/// Neden gerekli: tarifler <c>GridManager.recipes</c> içinde, yani YALNIZCA
/// Game sahnesinde. Tarif defteri ana menüde de açılıyor ve geriye dönük
/// doldurma oyun sahnesine girilmeden çalışabilmeli. Bu asset Editor tarafında
/// Game sahnesinden üretilir (<c>NexumRecipeCatalog</c>) ve çalışma zamanında
/// Resources'tan okunur.
/// </summary>
public class RecipeCatalog : ScriptableObject
{
    public const string ResourcePath = "RecipeCatalog";

    [System.Serializable]
    public class Entry
    {
        [Tooltip("Kalıcı kimlik: iki girdinin alfabetik sırayla birleşimi (GridManager.GetMergeKey ile aynı)")]
        public string key;

        public string inputAName;      // Tile_H
        public string inputBName;      // Tile_O
        public string productName;     // Tile_H2O

        public string inputASymbol;    // H
        public string inputBSymbol;    // O
        public string productSymbol;   // H<sub>2</sub>O

        [Tooltip("Keşfedilen tarife dokununca açılacak ansiklopedi kartı")]
        public ElementData productData;
    }

    [System.Serializable]
    public class LevelGoalInfo
    {
        public string levelName;
        public List<string> spawnPool = new List<string>();   // Tile_* adları (ağırlık > 0)
        public List<string> goals = new List<string>();       // Tile_* adları
    }

    public List<Entry> entries = new List<Entry>();
    public List<LevelGoalInfo> levels = new List<LevelGoalInfo>();

    private static RecipeCatalog cached;

    public static RecipeCatalog Load()
    {
        if (cached == null) cached = Resources.Load<RecipeCatalog>(ResourcePath);
        if (cached == null)
        {
            Debug.LogError($"RecipeCatalog: Resources/{ResourcePath} bulunamadı " +
                           "(NEXUM -> Tarif Kataloğunu Kur)");
        }
        return cached;
    }

    /// <summary>Test için önbelleği boşaltır.</summary>
    public static void ClearCache()
    {
        cached = null;
    }

    public Entry Find(string key)
    {
        foreach (Entry e in entries)
        {
            if (e.key == key) return e;
        }
        return null;
    }

    /// <summary>GridManager.GetMergeKey ile AYNI kural: alfabetik sırayla "A_B".</summary>
    public static string MakeKey(string nameA, string nameB)
    {
        string a = nameA.Replace("(Clone)", "");
        string b = nameB.Replace("(Clone)", "");
        return string.Compare(a, b) < 0 ? a + "_" + b : b + "_" + a;
    }
}
