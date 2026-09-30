using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Yeni bileşiklerin taş prefab'larını, ElementData asset'lerini ve
// GridManager.recipes kayıtlarını kurar.
//
// IDEMPOTENT: iki kez çalıştırıldığında aynı sonucu verir. Var olan prefab,
// asset ve tarifleri yeniden oluşturmaz, üzerine yazmaz.
//
// Menüden: NEXUM -> Yeni Bileşikleri Kur
// Batch mode: -executeMethod NexumNewCompounds.Run
//
// Metinler (ElementData.description ve MergeRecipe.scientificHint) bilerek
// burada tutulmaz; kimyasal onaydan sonra ayrı bir adımda yazılır.
public static class NexumNewCompounds
{
    private const string Tag = "NEXUM_COMPOUNDS";
    private const string PrefabDir = "Assets/Prefabs";
    private const string DataDir = "Assets/Resources/Elements";
    private const string TemplateTile = "Assets/Prefabs/Tile_O2.prefab";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    private class TileSpec
    {
        public string tile;         // prefab adı, ör. Tile_N2
        public string data;         // ElementData asset adı, ör. N2_Data
        public string display;      // taşın üzerindeki metin, ör. N<sub>2</sub>
        public string elementName;  // ansiklopedideki ad
        public string recipeText;   // "Sentez:" satırı
        public int score;           // synthesisScore
        public int jokerCost;
    }

    private class RecipeSpec
    {
        public string a, b, result;
        public int score;
    }

    // İki atomlu bileşikler H2 / O2 / Fe2 ile aynı ekonomiyi kullanır (30 / 60).
    private static readonly TileSpec[] NewTiles =
    {
        new TileSpec { tile = "Tile_N2",  data = "N2_Data",  display = "N<sub>2</sub>",
                       elementName = "Azot Gazı",  recipeText = "N + N",   score = 30, jokerCost = 60 },
        new TileSpec { tile = "Tile_Cl2", data = "Cl2_Data", display = "Cl<sub>2</sub>",
                       elementName = "Klor Gazı",  recipeText = "Cl + Cl", score = 30, jokerCost = 60 },
        new TileSpec { tile = "Tile_C2",  data = "C2_Data",  display = "C<sub>2</sub>",
                       elementName = "Dikarbon",   recipeText = "C + C",   score = 30, jokerCost = 60 },
    };

    // Her yeni bileşiğin en az bir tüketici tarifi vardır; çıkmaz sokak yok.
    private static readonly RecipeSpec[] NewRecipes =
    {
        // Oluşum (temel + temel, H+H / O+O ile aynı puan)
        new RecipeSpec { a = "Tile_N",  b = "Tile_N",  result = "Tile_N2",  score = 10 },
        new RecipeSpec { a = "Tile_Cl", b = "Tile_Cl", result = "Tile_Cl2", score = 10 },
        new RecipeSpec { a = "Tile_C",  b = "Tile_C",  result = "Tile_C2",  score = 10 },

        // Tüketim (bileşik içeren tarifler 30 puan)
        new RecipeSpec { a = "Tile_N2",  b = "Tile_H2", result = "Tile_NH3",  score = 30 }, // Haber-Bosch
        new RecipeSpec { a = "Tile_Cl2", b = "Tile_Na", result = "Tile_NaCl", score = 30 },
        new RecipeSpec { a = "Tile_C2",  b = "Tile_O2", result = "Tile_CO",   score = 30 },
    };

    [MenuItem("NEXUM/Yeni Bileşikleri Kur")]
    public static void Run()
    {
        int createdData = 0, createdPrefab = 0;

        foreach (TileSpec spec in NewTiles)
        {
            if (EnsureElementData(spec)) createdData++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (TileSpec spec in NewTiles)
        {
            if (EnsureTilePrefab(spec)) createdPrefab++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int addedRecipes = EnsureRecipes();

        Debug.Log($"{Tag}: ElementData +{createdData}, prefab +{createdPrefab}, tarif +{addedRecipes}");
        Debug.Log($"{Tag}: OK");
    }

    // ------------------------------------------------------------- ElementData

    private static string DataPath(TileSpec spec)
    {
        return $"{DataDir}/{spec.data}.asset";
    }

    private static bool EnsureElementData(TileSpec spec)
    {
        string path = DataPath(spec);
        if (AssetDatabase.LoadAssetAtPath<ElementData>(path) != null) return false;

        ElementData asset = ScriptableObject.CreateInstance<ElementData>();
        asset.elementName = spec.elementName;
        asset.symbol = spec.display;
        asset.synthesisScore = spec.score;
        asset.jokerCost = spec.jokerCost;
        asset.recipe = spec.recipeText;
        asset.description = string.Empty;   // kimyasal onaydan sonra yazılır
        AssetDatabase.CreateAsset(asset, path);
        return true;
    }

    // ----------------------------------------------------------------- prefab

    private static string PrefabPath(string tileName)
    {
        return $"{PrefabDir}/{tileName}.prefab";
    }

    private static bool EnsureTilePrefab(TileSpec spec)
    {
        string path = PrefabPath(spec.tile);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return false;

        if (!AssetDatabase.CopyAsset(TemplateTile, path))
        {
            Debug.LogError($"{Tag}: prefab kopyalanamadı: {path}");
            return false;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        contents.name = spec.tile;

        TMP_Text label = contents.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = spec.display;

        TileInteraction interaction = contents.GetComponentInChildren<TileInteraction>(true);
        if (interaction != null)
        {
            interaction.myElementData = AssetDatabase.LoadAssetAtPath<ElementData>(DataPath(spec));
        }

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        return true;
    }

    // ---------------------------------------------------------------- tarifler

    private static string Key(string a, string b)
    {
        return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
    }

    private static int EnsureRecipes()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        GridManager gridManager = Object.FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError($"{Tag}: Game sahnesinde GridManager yok");
            return 0;
        }

        HashSet<string> existing = new HashSet<string>();
        foreach (MergeRecipe r in gridManager.recipes)
        {
            if (r.element1 == null || r.element2 == null) continue;
            existing.Add(Key(r.element1.name, r.element2.name));
        }

        List<MergeRecipe> list = new List<MergeRecipe>(gridManager.recipes);
        int added = 0;

        foreach (RecipeSpec spec in NewRecipes)
        {
            if (existing.Contains(Key(spec.a, spec.b))) continue;

            GameObject e1 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(spec.a));
            GameObject e2 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(spec.b));
            GameObject res = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(spec.result));
            if (e1 == null || e2 == null || res == null)
            {
                Debug.LogError($"{Tag}: tarif için prefab bulunamadı: {spec.a} + {spec.b} -> {spec.result}");
                continue;
            }

            MergeRecipe recipe = new MergeRecipe
            {
                element1 = e1,
                element2 = e2,
                resultPrefab = res,
                scoreReward = spec.score,
                scientificHint = string.Empty   // kimyasal onaydan sonra yazılır
            };
            list.Add(recipe);
            existing.Add(Key(spec.a, spec.b));
            added++;
        }

        if (added > 0)
        {
            gridManager.recipes = list;
            EditorUtility.SetDirty(gridManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return added;
    }
}
