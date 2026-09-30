using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Oyun içi kimya metinlerinin tek kaynağı: yeni bileşiklerin ansiklopedi
// metinleri (ElementData.description) ve TÜM tariflerin bilimsel ipuçları
// (MergeRecipe.scientificHint).
//
// UZUNLUK KURALI: ipucu metni ipucu kutusunun %85'ini geçemez (kutu 807×350;
// pratikte ana metin en fazla 2 satır). Sınır ölçümle korunuyor:
// Assets/Tests/PlayMode/HintFitTests.cs. Metin değiştirince testi çalıştır.
//
// IDEMPOTENT: aynı metin zaten yazılıysa hiçbir şeyi değiştirmez.
//
// Menüden: NEXUM -> Kimya Metinlerini Yaz
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

    // anahtar: "Tile_A+Tile_B" (ordinal sıraya göre; Key() ile aynı kural)
    private static readonly Dictionary<string, string> Hints = new Dictionary<string, string>
    {
        // --- temel tarifler
        ["Tile_H+Tile_H"] =
            "İki hidrojen atomu elektron paylaşıp kovalent bağ kurar: evrenin en hafif molekülü.",
        ["Tile_O+Tile_O"] =
            "İki oksijen atomu çift bağ kurar; soluduğumuz kararlı oksijen molekülü oluşur.",
        ["Tile_C+Tile_O"] =
            "Oksijen yetersizse karbon tek oksijene bağlanır; zehirli karbonmonoksit oluşur.",
        ["Tile_H+Tile_N"] =
            "Azot ilk hidrojenini bağlayınca kısa ömürlü imidogen oluşur; amonyağın ilk adımı.",
        ["Tile_H2+Tile_O"] =
            "Bir oksijen iki hidrojenle 104,5 derecelik açı yaparak su molekülünü kurar.",
        ["Tile_Cl+Tile_Na"] =
            "Sodyum değerlik elektronunu klora verir; zıt yüklü iyonlar sofra tuzunu kurar.",
        ["Tile_CO+Tile_O"] =
            "Karbonmonoksit bir oksijen daha bağlayarak kararlı ve doğrusal karbondioksite dönüşür.",
        ["Tile_O+Tile_O2"] =
            "Serbest oksijen atomu oksijen molekülüne katılır; morötesi kalkanımız ozon oluşur.",
        ["Tile_Fe+Tile_Fe"] =
            "İki demir atomu metalik bağla birleşir; pas tepkimesi için gereken demir çiftini hazırlar.",
        ["Tile_Fe2+Tile_O3"] =
            "Demir oksijenle yükseltgenince kırmızı-kahverengi pas, yani demir(III) oksit oluşur.",
        ["Tile_H2+Tile_NH"] =
            "İmidogen iki hidrojen daha bağlayınca üçgen piramit yapılı amonyak molekülü tamamlanır.",
        ["Tile_Ca+Tile_O"] =
            "Kalsiyum oksijenle birleşince inşaatın temel malzemesi sönmemiş kireç elde edilir.",
        ["Tile_CO2+Tile_H2O"] =
            "Karbondioksit suda çözününce maden suyuna ekşiliğini veren karbonik asit oluşur.",
        ["Tile_CO2+Tile_CaO"] =
            "Sönmemiş kireç karbondioksiti bağlar; mermerin maddesi kalsiyum karbonat oluşur.",
        ["Tile_NH3+Tile_O2"] =
            "Amonyak katalizörle yükseltgenince nitrik asidin ilk adımı azot monoksit oluşur.",

        // --- 13. turda eklenen bileşikler
        ["Tile_N+Tile_N"] =
            "İki azot atomu üçlü bağ kurar; doğanın en sağlam bağlarından biri böyle oluşur.",
        ["Tile_Cl+Tile_Cl"] =
            "İki klor atomu birer elektron paylaşarak kararlı, sarı-yeşil klor molekülünü oluşturur.",
        ["Tile_C+Tile_C"] =
            "İki karbon atomu kısa ömürlü dikarbonu kurar; alevin mavisini bu yapılar boyar.",
        ["Tile_H2+Tile_N2"] =
            "Haber-Bosch: azotun üçlü bağını demir katalizör ve basınç kırar; amonyak oluşur.",
        ["Tile_Cl2+Tile_Na"] =
            "Klor molekülü ayrışır; her klor atomu bir sodyumun elektronunu alıp tuz kurar.",
        ["Tile_C2+Tile_O2"] =
            "Kararsız dikarbon oksijen molekülüyle karşılaşınca iki karbonmonoksite dönüşür.",
    };

    [MenuItem("NEXUM/Kimya Metinlerini Yaz")]
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
        HashSet<string> eslesen = new HashSet<string>();
        int written = 0;

        for (int i = 0; i < list.Count; i++)
        {
            MergeRecipe recipe = list[i];
            if (recipe.element1 == null || recipe.element2 == null) continue;

            string key = Key(recipe.element1.name, recipe.element2.name);
            if (!Hints.TryGetValue(key, out string text))
            {
                Debug.LogWarning($"{Tag}: tabloda karşılığı olmayan tarif: {key}");
                continue;
            }

            eslesen.Add(key);
            if (recipe.scientificHint == text) continue;

            recipe.scientificHint = text;
            list[i] = recipe;
            written++;
        }

        // Tabloda olup sahnede karşılığı olmayan anahtar = yazım hatası
        foreach (string key in Hints.Keys)
        {
            if (!eslesen.Contains(key)) Debug.LogError($"{Tag}: tarifi bulunamayan anahtar: {key}");
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
