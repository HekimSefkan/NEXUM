using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Yeni bileşiklerin ansiklopedi metinlerini (ElementData.description) ve
// tariflerin bilimsel ipuçlarını (MergeRecipe.scientificHint) yazar.
//
// Metinler kimyasal onay bekliyordu; bu yüzden yapısal kurulumdan
// (NexumNewCompounds) ayrı tutuldu. Metin değişirse aşağıdaki tablolar
// düzenlenip script yeniden çalıştırılır.
//
// IDEMPOTENT: aynı metin zaten yazılıysa hiçbir şeyi değiştirmez.
//
// Menüden: NEXUM -> Yeni Bileşik Metinlerini Yaz
// Batch mode: -executeMethod NexumCompoundTexts.Run
public static class NexumCompoundTexts
{
    private const string Tag = "NEXUM_COMPOUND_TEXTS";
    private const string DataDir = "Assets/Resources/Elements";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        ["N2_Data"] = "Soluduğumuz havanın beşte dördü. Üçlü bağı o kadar sağlamdır ki azot, " +
                      "gübreye dönüşmek için yüksek sıcaklık ve basınç ister.",
        ["Cl2_Data"] = "Sarı-yeşil, keskin kokulu ve zehirli bir gaz. Suyu dezenfekte eder; " +
                       "sodyumla buluşunca uslanıp sofra tuzuna dönüşür.",
        ["C2_Data"] = "İki karbonun kısa ömürlü buluşması. Mum alevinin mavi bölgesinde ve " +
                      "kuyruklu yıldızlarda görülür; kararsızdır, ilk fırsatta oksijene koşar.",
    };

    // anahtar: "Tile_A+Tile_B" (alfabetik sırada)
    private static readonly Dictionary<string, string> Hints = new Dictionary<string, string>
    {
        ["Tile_N+Tile_N"] =
            "İki azot atomu üçlü bağ kurar; doğadaki en sağlam moleküler bağlardan biri böyle oluşur.",
        ["Tile_Cl+Tile_Cl"] =
            "İki klor atomu birer elektron paylaşarak kararlı, sarı-yeşil klor molekülünü oluşturur.",
        ["Tile_C+Tile_C"] =
            "İki karbon atomu kısa ömürlü dikarbonu kurar; alevin mavi bölgesini bu tür yapılar boyar.",
        ["Tile_H2+Tile_N2"] =
            "Haber-Bosch yöntemi: azotun üçlü bağı demir katalizör eşliğinde yüksek basınç ve " +
            "sıcaklıkta kırılır, hidrojenle birleşerek amonyak oluşur.",
        ["Tile_Cl2+Tile_Na"] =
            "Klor molekülü ayrışır ve her klor atomu sodyumun tek değerlik elektronunu alır; " +
            "zıt yüklü iyonlar kenetlenince sofra tuzu oluşur.",
        ["Tile_C2+Tile_O2"] =
            "Kararsız dikarbon, oksijen molekülüyle karşılaşınca bağı kopar ve iki karbonmonoksite dönüşür.",
    };

    [MenuItem("NEXUM/Yeni Bileşik Metinlerini Yaz")]
    public static void Run()
    {
        int dataWritten = 0;

        foreach (KeyValuePair<string, string> pair in Descriptions)
        {
            string path = $"{DataDir}/{pair.Key}.asset";
            ElementData data = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            if (data == null)
            {
                Debug.LogError($"{Tag}: asset yok: {path}");
                continue;
            }
            if (data.description == pair.Value) continue;

            data.description = pair.Value;
            EditorUtility.SetDirty(data);
            dataWritten++;
        }

        if (dataWritten > 0) AssetDatabase.SaveAssets();

        int hintsWritten = WriteHints();

        Debug.Log($"{Tag}: description +{dataWritten}, scientificHint +{hintsWritten}");
        Debug.Log($"{Tag}: OK");
    }

    private static string Key(string a, string b)
    {
        return string.CompareOrdinal(a, b) <= 0 ? a + "+" + b : b + "+" + a;
    }

    private static int WriteHints()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        GridManager gridManager = Object.FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError($"{Tag}: Game sahnesinde GridManager yok");
            return 0;
        }

        List<MergeRecipe> list = new List<MergeRecipe>(gridManager.recipes);
        int written = 0;

        for (int i = 0; i < list.Count; i++)
        {
            MergeRecipe recipe = list[i];
            if (recipe.element1 == null || recipe.element2 == null) continue;

            string key = Key(recipe.element1.name, recipe.element2.name);
            if (!Hints.TryGetValue(key, out string text)) continue;
            if (recipe.scientificHint == text) continue;

            recipe.scientificHint = text;
            list[i] = recipe;
            written++;
        }

        if (written > 0)
        {
            gridManager.recipes = list;
            EditorUtility.SetDirty(gridManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return written;
    }
}
