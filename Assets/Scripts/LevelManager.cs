using System.Collections.Generic;
using UnityEngine;

// 1. Yeni Eklediğimiz: Her bir görevi tanımlayan yapı
[System.Serializable]
public class LevelGoal
{
    public GameObject targetPrefab; // İstenen bileşik (Örn: Tile_H2O)
    public int targetAmount;        // Kaç tane isteniyor? (Örn: 3)
    
    [HideInInspector] 
    public int currentAmount = 0;   // Oyun içinde oyuncunun o an kaç tane ürettiğini tutar
}

// 2. Yanlışlıkla Silinen: Havuza düşme olasılıklarını belirleyen yapı
[System.Serializable]
public class SpawnElement
{
    public GameObject elementPrefab;
    [Range(1, 100)] public int spawnWeight; 
}

// 3. Bölüm (Level) Genel Kuralları
[System.Serializable]
public class LevelData
{
    public string levelName;
    public int startingTileCount = 3; 
    public List<SpawnElement> spawnPool; 

    [Header("Bölüm Hedefleri")]
    public List<LevelGoal> levelGoals; 

    // --- YENİ EKLENEN: DENEY FÖYÜ (TUTORIAL) VERİLERİ ---
    [Header("Deney Föyü (Öğretici) Ayarları")]
    public string tutorialTitle; 
    
    [TextArea(5, 12)] 
    public string tutorialContent; 
    
    // Yan yana duracak 2 adet generic büyük yuva (Örn: H ve O, veya H2O ve Isı Şeması)
    public Sprite tutorialLargeSprite1; // Sol Büyük Resim
    public Sprite tutorialLargeSprite2; // Sağ Büyük Resim
}
// 4. Ana Yönetici Sınıfımız
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Bölüm Ayarları")]
    public List<LevelData> levels;
    public int currentLevelIndex = 0;

    void Awake()
    {
        Instance = this;
        currentLevelIndex = PlayerPrefs.GetInt("SelectedLevel", 0); 
    }

    void Start()
    {
        // Oyun başladığında bu bölümün tüm hedeflerinin üretilen miktarını 0'a sıfırla
        foreach (var goal in levels[currentLevelIndex].levelGoals)
        {
            goal.currentAmount = 0;
        }
    }

    // Ağırlıklı Olasılık (Weighted Probability) Algoritması
    public GameObject GetRandomElementForCurrentLevel()
    {
        LevelData currentLevel = levels[currentLevelIndex];
        
        int totalWeight = 0;
        foreach (var item in currentLevel.spawnPool)
        {
            totalWeight += item.spawnWeight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (var item in currentLevel.spawnPool)
        {
            cumulativeWeight += item.spawnWeight;
            if (randomValue < cumulativeWeight)
            {
                return item.elementPrefab;
            }
        }

        return currentLevel.spawnPool[0].elementPrefab;
    }
}