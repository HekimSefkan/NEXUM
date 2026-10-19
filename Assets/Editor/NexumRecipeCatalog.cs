using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tarif kataloğunu Game sahnesinden üretir.
///
/// Tarifler <c>GridManager.recipes</c> içinde, yani yalnızca Game sahnesinde
/// var. Tarif defteri ana menüde de açılıyor ve geriye dönük doldurma oyun
/// sahnesine girilmeden çalışabilmeli; bu yüzden tablo bir Resources asset'ine
/// kopyalanıyor.
///
/// IDEMPOTENT: içerik aynıysa asset'e dokunmaz.
///
/// Menüden: NEXUM → Tarif Kataloğunu Kur
/// Batch mode: -executeMethod NexumRecipeCatalog.Run
/// </summary>
public static class NexumRecipeCatalog
{
    private const string Tag = "NEXUM_CATALOG";
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string AssetDir = "Assets/Resources";
    private const string AssetPath = AssetDir + "/" + RecipeCatalog.ResourcePath + ".asset";

    [MenuItem("NEXUM/Tarif Kataloğunu Kur")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GridManager grid = Object.FindObjectOfType<GridManager>(true);
        LevelManager levels = Object.FindObjectOfType<LevelManager>(true);
        if (grid == null || levels == null)
        {
            Debug.LogError($"{Tag}: Game sahnesinde GridManager ya da LevelManager yok");
            return;
        }

        Directory.CreateDirectory(AssetDir);

        RecipeCatalog catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalog>(AssetPath);
        bool created = false;
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<RecipeCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
            created = true;
        }

        List<RecipeCatalog.Entry> entries = new List<RecipeCatalog.Entry>();
        HashSet<string> gorulen = new HashSet<string>();

        foreach (MergeRecipe r in grid.recipes)
        {
            if (r.element1 == null || r.element2 == null || r.resultPrefab == null) continue;

            string key = RecipeCatalog.MakeKey(r.element1.name, r.element2.name);
            if (!gorulen.Add(key))
            {
                Debug.LogWarning($"{Tag}: yinelenen tarif anahtarı atlandı -> {key}");
                continue;
            }

            entries.Add(new RecipeCatalog.Entry
            {
                key = key,
                inputAName = r.element1.name,
                inputBName = r.element2.name,
                productName = r.resultPrefab.name,
                inputASymbol = Symbol(r.element1),
                inputBSymbol = Symbol(r.element2),
                productSymbol = Symbol(r.resultPrefab),
                productData = Data(r.resultPrefab),
            });
        }

        // Oyundaki TUM taslar: temel elementler + tariflerin urunleri.
        // TileLevels seviyeyi bu listeden tureteceği icin eksiksiz olmali.
        List<RecipeCatalog.TileInfo> tiles = new List<RecipeCatalog.TileInfo>();
        HashSet<string> tasGorulen = new HashSet<string>();

        void TasEkle(GameObject prefab)
        {
            if (prefab == null || !tasGorulen.Add(prefab.name)) return;
            tiles.Add(new RecipeCatalog.TileInfo
            {
                name = prefab.name,
                symbol = Symbol(prefab),
                data = Data(prefab),
            });
        }

        foreach (GameObject el in grid.basicElements) TasEkle(el);
        foreach (MergeRecipe r in grid.recipes)
        {
            TasEkle(r.element1);
            TasEkle(r.element2);
            TasEkle(r.resultPrefab);
        }

        List<RecipeCatalog.LevelGoalInfo> levelInfos = new List<RecipeCatalog.LevelGoalInfo>();
        foreach (LevelData level in levels.levels)
        {
            RecipeCatalog.LevelGoalInfo info = new RecipeCatalog.LevelGoalInfo
            {
                levelName = level.levelName,
            };

            foreach (SpawnElement s in level.spawnPool)
            {
                if (s.elementPrefab != null && s.spawnWeight > 0) info.spawnPool.Add(s.elementPrefab.name);
            }
            foreach (LevelGoal g in level.levelGoals)
            {
                if (g.targetPrefab != null) info.goals.Add(g.targetPrefab.name);
            }

            levelInfos.Add(info);
        }

        bool changed = created || Differs(catalog, entries, levelInfos) || TilesDiffers(catalog, tiles);
        if (changed)
        {
            catalog.entries = entries;
            catalog.levels = levelInfos;
            catalog.tiles = tiles;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"{Tag}: tarif {entries.Count}, bolum {levelInfos.Count}, tas {tiles.Count} -> " +
                  $"{(changed ? "yazildi" : "zaten guncel")}");
        Debug.Log($"{Tag}: OK");
    }

    private static bool TilesDiffers(RecipeCatalog catalog, List<RecipeCatalog.TileInfo> tiles)
    {
        if (catalog.tiles.Count != tiles.Count) return true;
        for (int i = 0; i < tiles.Count; i++)
        {
            if (catalog.tiles[i].name != tiles[i].name) return true;
            if (catalog.tiles[i].symbol != tiles[i].symbol) return true;
            if (catalog.tiles[i].data != tiles[i].data) return true;
        }
        return false;
    }

    private static bool Differs(RecipeCatalog catalog,
                                List<RecipeCatalog.Entry> entries,
                                List<RecipeCatalog.LevelGoalInfo> levels)
    {
        if (catalog.entries.Count != entries.Count) return true;
        if (catalog.levels.Count != levels.Count) return true;

        for (int i = 0; i < entries.Count; i++)
        {
            RecipeCatalog.Entry a = catalog.entries[i];
            RecipeCatalog.Entry b = entries[i];
            if (a.key != b.key || a.productName != b.productName ||
                a.inputASymbol != b.inputASymbol || a.inputBSymbol != b.inputBSymbol ||
                a.productSymbol != b.productSymbol || a.productData != b.productData) return true;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (string.Join(",", catalog.levels[i].goals) != string.Join(",", levels[i].goals)) return true;
            if (string.Join(",", catalog.levels[i].spawnPool) != string.Join(",", levels[i].spawnPool)) return true;
        }

        return false;
    }

    private static string Symbol(GameObject tilePrefab)
    {
        ElementData data = Data(tilePrefab);
        if (data != null && !string.IsNullOrEmpty(data.symbol)) return data.symbol;
        return tilePrefab.name.Replace("Tile_", "");
    }

    private static ElementData Data(GameObject tilePrefab)
    {
        TileInteraction tile = tilePrefab.GetComponent<TileInteraction>();
        return tile != null ? tile.myElementData : null;
    }
}
