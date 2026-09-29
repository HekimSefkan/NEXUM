using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public struct MergeRecipe
{
    public GameObject element1;
    public GameObject element2;
    public GameObject resultPrefab;
    public int scoreReward; 
    [TextArea(2, 3)] 
    public string scientificHint; 
}

public class GridSnapshot
{
    public GameObject[] savedTiles = new GameObject[16]; 
    public int savedScore; 

    // Geri alma yalnızca taşları değil, hamleyle birlikte değişen sayaçları da
    // eski hâline döndürmeli; aksi hâlde hedef üret -> geri al -> yeniden üret
    // döngüsüyle sayaçlar şişiyordu.
    public int[] savedGoalAmounts;      // bölümün hedef sayaçları
    public int savedCatalystCharge;     // katalizör şarjı
    public int savedFreeJokerCharges;   // bedava Joker hakkı
    public int savedMovesSinceSpawn;    // melez spawn sayacı
    public int savedTotalSynthesis;     // profildeki toplam sentez
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Arayüz Bağlantıları")]
    public Transform gridBoard; 

    [Header("Doğacak Temel Elementler")]
    public GameObject[] basicElements; 

    [Header("Kimya Reaksiyon Tarifleri")]
    public List<MergeRecipe> recipes; 
    
    private Dictionary<string, MergeRecipe> mergeDictionary = new Dictionary<string, MergeRecipe>();
    private List<Transform> cells = new List<Transform>();
    // --- Katalizör şarjı --------------------------------------------------
    // Gösterge artık ceza değil ÖDÜL biriktirir: her sentez şarjı doldurur,
    // dolunca oyuncuya bedava bir Joker (parçalama) hakkı verilir.
    // Melez spawn kuralı geldikten sonra entropi ceza taşına gerek kalmadı.
    public const int CatalystChargeLimit = 5;
    private int catalystCharge = 0;

    /// <summary>Bedava (puan harcamayan) Joker hakkı.</summary>
    public int freeJokerCharges = 0;

    // Melez spawn: birleşme olmayan her MergelessMovesPerSpawn hamlede bir yeni
    // taş gelir. Entropi cezasından bağımsızdır ve her modda çalışır.
    private const int MergelessMovesPerSpawn = 2;
    private int movesSinceSpawn = 0;

    // --- Yeni taşın doğuş zamanlaması -----------------------------------
    // Kayma tween'i 0,2 sn; birleşme ürününün büyümesi 0,3 sn. Yeni taş
    // bunların ikisi de bitmeden doğarsa oyuncu onu göremeden birleşebiliyor.
    public const float MoveAnimDuration = 0.2f;
    public const float MergeAnimDuration = 0.3f;
    public const float SpawnDelay = MergeAnimDuration;

    private int pendingSpawns = 0;
    private Vector2 pendingSpawnDirection = Vector2.zero;
    private Coroutine spawnRoutine;

    /// <summary>Animasyonların bitmesi beklenen, henüz doğmamış taş var mı?</summary>
    public bool HasPendingSpawn { get { return pendingSpawns > 0; } }
    public bool hasUsedRevive = false; 

    // Bölüm kazanıldı mı? Kazanma ekranının tekrar tetiklenmesini engeller.
    public bool hasWon = false;

    [Header("Tersinir Tepkime (Undo) Ayarları")]
    public int undoCost = 50; 
    public int currentUndoLimit = 0; 
    public int usedUndos = 0; 
    
    [Header("Asistan Ayarları")]
    public int baseHintCost = 25; 
    public int maxHints = 3; 
    public int usedHints = 0; 
    private Transform activeHintTile1; 
    private Transform activeHintTile2; 
    
    [Header("Oyun Modu")]
    public int currentGameMode = 0; // 0 = Normal, 1 = Sınav, 2 = Serbest

    [Header("Görsel Efektler")]
    public GameObject mergeParticlePrefab; 

    public int GetCurrentHintCost()
    {
        return (usedHints + 1) * baseHintCost; 
    }
    
    private Stack<GridSnapshot> historyStack = new Stack<GridSnapshot>();

    void Awake() 
    { 
        Instance = this; 
        BuildDictionary(); 
    }

    void Start()
    {
        foreach (Transform child in gridBoard) 
        {
            cells.Add(child);
        }
        
        int startCount = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex].startingTileCount;

        currentUndoLimit = 2 + (LevelManager.Instance.currentLevelIndex / 2); 
        usedUndos = 0;
        historyStack.Clear(); 
        usedHints = 0; 
        
        currentGameMode = PlayerPrefs.GetInt("SelectedGameMode", 0);

        for (int i = 0; i < startCount; i++)
        {
            SpawnTile();
        }

        catalystCharge = 0;
        freeJokerCharges = 0;
        if (UIManager.Instance != null) UIManager.Instance.UpdateCatalystMeter(0, 0);
    }

    private void BuildDictionary()
    {
        foreach (var recipe in recipes)
        {
            if (recipe.element1 != null && recipe.element2 != null && recipe.resultPrefab != null)
            {
                string key = GetMergeKey(recipe.element1.name, recipe.element2.name);
                if (!mergeDictionary.ContainsKey(key)) mergeDictionary.Add(key, recipe);
            }
        }
    }

    private string GetMergeKey(string name1, string name2)
    {
        string cleanName1 = name1.Replace("(Clone)", "");
        string cleanName2 = name2.Replace("(Clone)", "");

        if (string.Compare(cleanName1, cleanName2) < 0) return cleanName1 + "_" + cleanName2;
        else return cleanName2 + "_" + cleanName1;
    }

    public void SpawnTile()
    {
        SpawnTile(Vector2.zero);
    }

    /// <summary>
    /// Yeni taşı doğurur. Konum kuralı (sırayla):
    /// 1) Kaydırma yönünün TERSİNDEKİ kenardaki boş hücreler.
    /// 2) O kenar doluysa, kaydırma hedefinden en uzak boş hücreler.
    /// 3) Bu aday küme içinde, komşusuyla anında birleşmeyecek hücreler
    ///    (hiçbiri yoksa kural esnetilir; tahta dolu olabilir).
    /// </summary>
    public void SpawnTile(Vector2 direction)
    {
        List<int> emptyIndices = new List<int>();
        for (int i = 0; i < cells.Count; i++) if (cells[i].childCount == 0) emptyIndices.Add(i);

        if (emptyIndices.Count == 0) return;

        GameObject selectedElement = LevelManager.Instance.GetRandomElementForCurrentLevel();

        List<int> candidates = FarthestCells(direction, emptyIndices);
        List<int> safe = new List<int>();
        foreach (int index in candidates)
        {
            if (!WouldMergeImmediately(index, selectedElement.name)) safe.Add(index);
        }
        if (safe.Count > 0) candidates = safe;

        int chosen = candidates[Random.Range(0, candidates.Count)];
        GameObject newTile = Instantiate(selectedElement, cells[chosen]);
        newTile.transform.localScale = Vector3.zero;

        // Belirme animasyonu: birleşme ürününden ayırt edilebilsin diye
        // hafif taşmalı büyüme + kısa parlama.
        Sequence appear = DOTween.Sequence();
        appear.Append(newTile.transform.DOScale(Vector3.one * 1.18f, 0.18f).SetEase(Ease.OutBack));
        appear.Append(newTile.transform.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutQuad));
        PlaySpawnFlash(newTile);
    }

    /// <summary>Kaydırma hedefinden en uzak sıradaki boş hücreler.</summary>
    private List<int> FarthestCells(Vector2 direction, List<int> emptyIndices)
    {
        if (direction == Vector2.zero) return new List<int>(emptyIndices);

        int best = -1;
        List<int> result = new List<int>();
        foreach (int index in emptyIndices)
        {
            int row = index / 4;
            int column = index % 4;
            int distance;
            if (direction == Vector2.up) distance = row;            // yukarı kaydırıldıysa en alt sıra
            else if (direction == Vector2.down) distance = 3 - row;  // aşağı kaydırıldıysa en üst sıra
            else if (direction == Vector2.left) distance = column;   // sola kaydırıldıysa en sağ sütun
            else distance = 3 - column;                              // sağa kaydırıldıysa en sol sütun

            if (distance > best) { best = distance; result.Clear(); result.Add(index); }
            else if (distance == best) result.Add(index);
        }
        return result;
    }

    /// <summary>Bu hücreye konacak taş, komşularından biriyle hemen birleşir mi?</summary>
    private bool WouldMergeImmediately(int index, string tileName)
    {
        int row = index / 4;
        int column = index % 4;

        if (row > 0 && MergesWith(index - 4, tileName)) return true;
        if (row < 3 && MergesWith(index + 4, tileName)) return true;
        if (column > 0 && MergesWith(index - 1, tileName)) return true;
        if (column < 3 && MergesWith(index + 1, tileName)) return true;
        return false;
    }

    private bool MergesWith(int neighbourIndex, string tileName)
    {
        if (cells[neighbourIndex].childCount == 0) return false;
        string neighbourName = cells[neighbourIndex].GetChild(0).name;
        return mergeDictionary.ContainsKey(GetMergeKey(tileName, neighbourName));
    }

    /// <summary>Taşın üstünde bir kez parlayıp sönen beyaz kopya.</summary>
    private void PlaySpawnFlash(GameObject tile)
    {
        Image source = tile.GetComponent<Image>();
        if (source == null) return;

        GameObject glow = new GameObject("SpawnGlow", typeof(RectTransform), typeof(Image));
        RectTransform rect = glow.GetComponent<RectTransform>();
        rect.SetParent(tile.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();   // sembolün altında kalsın

        Image glowImage = glow.GetComponent<Image>();
        glowImage.sprite = source.sprite;
        glowImage.raycastTarget = false;
        glowImage.color = new Color(1f, 1f, 1f, 0.85f);

        rect.DOScale(1.35f, 0.35f).SetEase(Ease.OutQuad);
        glowImage.DOFade(0f, 0.35f).SetEase(Ease.OutQuad)
                 .OnComplete(() => { if (glow != null) Destroy(glow); });
    }

    // --- Bekleyen doğuşlar ------------------------------------------------

    /// <summary>Sentez başına şarjı doldurur; dolunca bedava Joker hakkı verir.</summary>
    private void AddCatalystCharge(int syntheses)
    {
        if (syntheses <= 0) return;

        catalystCharge += syntheses;
        bool rewarded = false;

        while (catalystCharge >= CatalystChargeLimit)
        {
            catalystCharge -= CatalystChargeLimit;
            freeJokerCharges++;
            rewarded = true;
        }

        UIManager.Instance.UpdateCatalystMeter(catalystCharge, freeJokerCharges);
        if (rewarded) UIManager.Instance.ShowCatalystReady(freeJokerCharges);
    }

    /// <summary>Bedava Joker hakkı varsa birini harcar.</summary>
    public bool TryConsumeFreeJoker()
    {
        if (freeJokerCharges <= 0) return false;

        freeJokerCharges--;
        UIManager.Instance.UpdateCatalystMeter(catalystCharge, freeJokerCharges);
        return true;
    }

    private void RequestSpawn(Vector2 direction)
    {
        pendingSpawns++;
        pendingSpawnDirection = direction;
    }

    private IEnumerator ResolveSpawnsAfterAnimations()
    {
        yield return new WaitForSeconds(SpawnDelay);
        spawnRoutine = null;
        FlushPendingSpawns(true);
    }

    /// <summary>Bekleyen taşları hemen doğurur (oyuncu beklemeden hamle yaparsa).</summary>
    private void FlushPendingSpawns(bool checkGameOver)
    {
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }

        while (pendingSpawns > 0)
        {
            pendingSpawns--;
            SpawnTile(pendingSpawnDirection);
        }

        if (checkGameOver) CheckGameOver();
    }

    /// <summary>Geri alma, henüz doğmamış taşı da iptal eder.</summary>
    private void CancelPendingSpawns()
    {
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }
        pendingSpawns = 0;
    }

    private void SaveCurrentState()
    {
        GridSnapshot snapshot = new GridSnapshot();
        GameManager gm = FindObjectOfType<GameManager>();
        snapshot.savedScore = gm != null ? gm.currentScore : 0; 
        snapshot.savedCatalystCharge = catalystCharge;
        snapshot.savedFreeJokerCharges = freeJokerCharges;
        snapshot.savedMovesSinceSpawn = movesSinceSpawn;
        snapshot.savedTotalSynthesis = PlayerPrefs.GetInt("TotalSynthesis", 0);

        var savedGoals = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex].levelGoals;
        snapshot.savedGoalAmounts = new int[savedGoals.Count];
        for (int g = 0; g < savedGoals.Count; g++) snapshot.savedGoalAmounts[g] = savedGoals[g].currentAmount;

        for (int i = 0; i < 16; i++)
        {
            if (cells[i].childCount > 0)
            {
                string tileName = cells[i].GetChild(0).name.Replace("(Clone)", "");
                snapshot.savedTiles[i] = FindPrefabByName(tileName);
            }
            else snapshot.savedTiles[i] = null;
        }
        historyStack.Push(snapshot); 
    }

    private GameObject FindPrefabByName(string name)
    {
        foreach (var el in basicElements) if (el.name == name) return el;
        foreach (var rec in recipes) if (rec.resultPrefab.name == name) return rec.resultPrefab;
        return null;
    }

    // Kaydırma yönüne göre hücrelerin taranma sırası: hedef kenara en yakın
    // hücreden başlanır, böylece taşlar birbirinin üstüne binmez.
    // (Kenardaki hücreler zaten ilerleyemeyeceği için listede yok.)
    private static readonly int[] ScanUp    = { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
    private static readonly int[] ScanDown  = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
    private static readonly int[] ScanLeft  = { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15 };
    private static readonly int[] ScanRight = { 14, 13, 12, 10, 9, 8, 6, 5, 4, 2, 1, 0 };

    private static int[] GetScanOrder(Vector2 direction)
    {
        if (direction == Vector2.up) return ScanUp;
        if (direction == Vector2.down) return ScanDown;
        if (direction == Vector2.left) return ScanLeft;
        if (direction == Vector2.right) return ScanRight;
        return null;
    }

    private static int GetStepOffset(Vector2 direction)
    {
        if (direction == Vector2.up) return -4;
        if (direction == Vector2.down) return 4;
        if (direction == Vector2.left) return -1;
        return 1;
    }

    public void Shift(Vector2 direction)
    {
        // ==========================================
        // YENİ EKLENEN: MASTER RESET (SENKRONİZASYON)
        // Yeni hamle hesaplanmadan önce matristeki tüm taşları düzelt
        // ==========================================
        foreach (Transform cell in cells)
        {
            if (cell.childCount > 0)
            {
                Transform tile = cell.GetChild(0);
                tile.DOKill();                     // Varsa yarım kalan animasyonu durdur
                tile.localScale = Vector3.one;     // Boyutu orijinale döndür (kaybolmayı önler)
                tile.localPosition = Vector3.zero; // Kutunun tam merkezine oturt (sünmeyi/kaymayı önler)
            }
        }

        // Oyuncu animasyonu beklemeden hamle yaptiysa bekleyen tas once doğar;
        // böylece hamle her zaman güncel tahta üzerinde hesaplanır.
        FlushPendingSpawns(false);

        SaveCurrentState(); 

        bool actionHappened = false;
        bool moveHappened = false; 
        
        int currentCombo = 0; 
        Vector3 lastMergePosition = Vector3.zero; 
        
        GameManager gameManager = FindObjectOfType<GameManager>();

        // Dört yön de aynı işi yapıyor; yalnızca hücrelerin taranma sırası, hedef
        // hücrenin ofseti ve "itilme" animasyonunun ekseni değişiyor.
        int[] scanOrder = GetScanOrder(direction);
        int step = GetStepOffset(direction);
        Vector3 punch = (direction == Vector2.up || direction == Vector2.down)
            ? new Vector3(0.15f, -0.15f, 0f)
            : new Vector3(-0.15f, 0.15f, 0f);

        if (scanOrder != null)
        {
            // Taşlar duvara (ya da önlerindeki engele) kadar kayar: tek hücrelik
            // tarama, hiçbir taş ilerlemeyene kadar tekrarlanır.
            // Bir hamlede üretilen bileşik aynı hamlede yeniden birleşmez.
            HashSet<Transform> mergedThisShift = new HashSet<Transform>();
            HashSet<Transform> movedTiles = new HashSet<Transform>();
            bool passMovedSomething;
            int safety = 0;

            do
            {
                passMovedSomething = false;

                foreach (int i in scanOrder)
                {
                    if (cells[i].childCount == 0) continue;

                    Transform currentTile = cells[i].GetChild(0);
                    int targetIndex = i + step;

                    if (cells[targetIndex].childCount == 0)
                    {
                        currentTile.SetParent(cells[targetIndex]);
                        movedTiles.Add(currentTile);
                        moveHappened = true;
                        passMovedSomething = true;
                        continue;
                    }

                    Transform targetTile = cells[targetIndex].GetChild(0);

                    // Bu hamlede üretilmiş bir bileşik ikinci kez birleşemez
                    if (mergedThisShift.Contains(currentTile) || mergedThisShift.Contains(targetTile)) continue;

                    string mergeKey = GetMergeKey(currentTile.name, targetTile.name);
                    if (!mergeDictionary.ContainsKey(mergeKey)) continue;

                    MergeRecipe recipe = mergeDictionary[mergeKey];

                    // Çakışmaları önlemek için eski objeleri hücreden kopar
                    currentTile.SetParent(null);
                    targetTile.SetParent(null);
                    movedTiles.Remove(currentTile);
                    movedTiles.Remove(targetTile);

                    Destroy(currentTile.gameObject);
                    Destroy(targetTile.gameObject);

                    GameObject mergedTile = Instantiate(recipe.resultPrefab, cells[targetIndex]);
                    mergedTile.transform.localScale = Vector3.zero;
                    mergedTile.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                    mergedThisShift.Add(mergedTile.transform);

                    if (gameManager != null)
                    {
                        int finalScore = (currentGameMode == 1) ? (recipe.scoreReward * 2) : recipe.scoreReward;
                        gameManager.AddScore(finalScore);
                    }

                    CheckWinCondition(recipe);

                    actionHappened = true;
                    currentCombo++;
                    PlayerPrefs.SetInt("TotalSynthesis", PlayerPrefs.GetInt("TotalSynthesis", 0) + 1);
                    lastMergePosition = cells[targetIndex].position;
                    passMovedSomething = true;
                }

                safety++;
            }
            while (passMovedSomething && safety < 24);

            // Kayan taşların animasyonu bir kez oynatılır: taş kaç hücre
            // ilerlediyse o mesafeyi tek seferde kat eder.
            foreach (Transform moved in movedTiles)
            {
                if (moved == null) continue;
                Transform tile = moved;
                tile.DOLocalMove(Vector3.zero, 0.2f).OnComplete(() => {
                    tile.DOPunchScale(punch, 0.15f, 1, 0.5f);
                });
            }
        }

        if (actionHappened)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.mergeClip);
            }

            if (mergeParticlePrefab != null)
            {
                GameObject fx = Instantiate(mergeParticlePrefab, lastMergePosition, Quaternion.identity);
                Destroy(fx, 1.5f); 
            }

            RequestSpawn(direction);
            movesSinceSpawn = 0;
            AddCatalystCharge(currentCombo);
            
            if (currentCombo > 0)
            {
                UIManager.Instance.ShowComboText(currentCombo, lastMergePosition);
                
                if (currentCombo >= 2) 
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.comboClip);
                    }

                    Handheld.Vibrate(); 
                }
            }
        }
        else if (moveHappened)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.shiftClip, 0.5f);
            }

            // Melez spawn: birleşme olmasa da her 2 hamlede bir yeni taş gelir.
            // Her modda geçerlidir; tahtanın hiç dolmadan sonsuza kadar
            // kaydırılabilmesini engeller.
            movesSinceSpawn++;
            if (movesSinceSpawn >= MergelessMovesPerSpawn)
            {
                RequestSpawn(direction);
                movesSinceSpawn = 0;
            }

            // Entropi ceza taşı kaldırıldı: melez spawn kuralı zaten birleşmesiz
            // hamlelerde tahtayı dolduruyor, ikinci bir ceza katmanına gerek yok.
            // Şarj yalnızca sentezle dolar; birleşmesiz hamle onu sıfırlamaz.
        }
        else
        {
            // Hiçbir şey değişmedi: bu hamle için alınan anlık görüntü geri alınır,
            // yoksa geçersiz hamleler geri alma yığınını şişiriyor.
            if (historyStack.Count > 0) historyStack.Pop();
        }

        if (pendingSpawns > 0)
        {
            // Yeni taş, kayma ve birleşme animasyonları bittikten sonra doğar.
            // Oyun sonu kontrolü de o zaman yapılır (yeni taş tahtayı doldurabilir).
            spawnRoutine = StartCoroutine(ResolveSpawnsAfterAnimations());
        }
        else
        {
            CheckGameOver();
        }
    }

    public void CheckGameOver()
    {
        foreach (Transform cell in cells) if (cell.childCount == 0) return; 

        for (int i = 0; i < 16; i++)
        {
            Transform currentTile = cells[i].GetChild(0);
            if ((i + 1) % 4 != 0 && cells[i + 1].childCount > 0)
            {
                Transform rightTile = cells[i + 1].GetChild(0);
                string mergeKey = GetMergeKey(currentTile.name, rightTile.name);
                if (mergeDictionary.ContainsKey(mergeKey)) return; 
            }
            if (i + 4 < 16 && cells[i + 4].childCount > 0)
            {
                Transform downTile = cells[i + 4].GetChild(0);
                string mergeKey = GetMergeKey(currentTile.name, downTile.name);
                if (mergeDictionary.ContainsKey(mergeKey)) return; 
            }
        }
        UIManager.Instance.ShowGameOver(); 
        PlayerPrefs.Save();
    }

    private void CheckWinCondition(MergeRecipe recipe)
    {
        // Kazanma ekranı bir kez açılır. Tek bir kaydırmada birden fazla birleşme
        // olabildiği için (kombo) bu kontrol olmadan aynı hamlede tekrar tetikleniyordu.
        // Bayrak sahne örneğinde durur; sahne yeniden yüklenince kendiliğinden sıfırlanır.
        if (hasWon) return;

        var currentLevel = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex];
        
        foreach (var goal in currentLevel.levelGoals)
        {
            if (recipe.resultPrefab == goal.targetPrefab)
            {
                goal.currentAmount++;
                UIManager.Instance.UpdateGoalUI(); 
            }
        }

        bool isWin = true;
        foreach (var goal in currentLevel.levelGoals)
        {
            if (goal.currentAmount < goal.targetAmount) isWin = false; 
        }

        if (isWin)
        {
            hasWon = true;
            TotalScoreService.AddLevelScore(FindObjectOfType<GameManager>());
            UIManager.Instance.ShowWinScreen();
        }
    }

    public void ApplyReviveBonus()
    {
        List<Transform> filledCells = new List<Transform>();
        hasUsedRevive = true;
        
        foreach(var cell in cells) if(cell.childCount > 0) filledCells.Add(cell); 

        int tilesToRemove = Mathf.Min(3, filledCells.Count); 
        
        for(int i = 0; i < tilesToRemove; i++)
        {
            int randomIndex = Random.Range(0, filledCells.Count);
            Transform tileToDestroy = filledCells[randomIndex].GetChild(0);
            tileToDestroy.DOKill();
            
            tileToDestroy.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                Destroy(tileToDestroy.gameObject);
            });
            
            filledCells.RemoveAt(randomIndex); 
        }
        Debug.Log("Matris temizlendi, oyuna devam edilebilir!");
    }

    public void ExecuteUndo()
    {
        GameManager gm = FindObjectOfType<GameManager>();

        if (historyStack.Count == 0)
        {
            Debug.LogWarning("Geri alınacak bir hamle yok!");
            return;
        }
        if (usedUndos >= currentUndoLimit)
        {
            Debug.LogWarning("Bu seviyedeki Tersinir Tepkime hakların bitti!");
            return;
        }
        if (gm != null && gm.currentScore < undoCost)
        {
            Debug.LogWarning("Tersinir Tepkime için yeterli puanın yok! Gereken: " + undoCost);
            return;
        }

        usedUndos++;

        // Henüz doğmamış taş varsa iptal edilir: anlık görüntü hamle öncesine ait.
        CancelPendingSpawns();

        GridSnapshot lastState = historyStack.Pop();

        // Önce hamle öncesi skora dönülür, sonra geri alma bedeli düşülür.
        // Bedel, hamle öncesi skoru eksiye düşürmez (buton kontrolü ekrandaki skora bakar).
        if (gm != null)
        {
            gm.currentScore = lastState.savedScore;
            gm.SubtractScore(Mathf.Min(undoCost, Mathf.Max(0, gm.currentScore)));
        }

        // Hedef sayaçları, entropi ve toplam sentez de hamle öncesine döner
        var goals = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex].levelGoals;
        if (lastState.savedGoalAmounts != null)
        {
            for (int g = 0; g < goals.Count && g < lastState.savedGoalAmounts.Length; g++)
            {
                goals[g].currentAmount = lastState.savedGoalAmounts[g];
            }
            UIManager.Instance.UpdateGoalUI();
        }

        catalystCharge = lastState.savedCatalystCharge;
        freeJokerCharges = lastState.savedFreeJokerCharges;
        movesSinceSpawn = lastState.savedMovesSinceSpawn;
        UIManager.Instance.UpdateCatalystMeter(catalystCharge, freeJokerCharges);

        PlayerPrefs.SetInt("TotalSynthesis", lastState.savedTotalSynthesis);

        foreach (Transform cell in cells)
        {
            if (cell.childCount > 0) Destroy(cell.GetChild(0).gameObject);
        }

        for (int i = 0; i < 16; i++)
        {
            if (lastState.savedTiles[i] != null)
            {
                GameObject restoredTile = Instantiate(lastState.savedTiles[i], cells[i]);
                restoredTile.transform.localScale = Vector3.zero;
                restoredTile.transform.DOScale(Vector3.one, 0.3f);
            }
        }

        Debug.Log($"Tersinir Tepkime Başarılı! Kalan Hak: {currentUndoLimit - usedUndos}");
    }

    public void RequestHint()
    {
        if (currentGameMode == 1)
        {
            UIManager.Instance.ShowHintMessage("<color=red>SINAV MODU AKTİF!</color>\nSınav modunda laboratuvar asistanından yardım alamazsın. Kendi bilgine güvenmelisin Baş Kimyager!");
            return; 
        }
        
        if (usedHints >= maxHints)
        {
            UIManager.Instance.ShowHintMessage("Bu laboratuvar seansındaki tüm asistan haklarını (3/3) tükettin Baş Kimyager! Artık kendi kimya bilgine güvenmelisin.");
            return;
        }

        GameManager gm = FindObjectOfType<GameManager>();
        int currentCost = GetCurrentHintCost(); 
        
        if (gm != null && gm.currentScore < currentCost)
        {
            UIManager.Instance.ShowHintMessage($"Laboratuvar bütçemiz yetersiz! Asistanın {usedHints + 1}. ipucunu verebilmesi için <color=red>{currentCost} puana</color> ihtiyacın var.");
            return;
        }

        var currentGoals = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex].levelGoals;
        Transform fallbackT1 = null; Transform fallbackT2 = null;
        string fallbackHint = ""; bool foundNormalMatch = false;

        for (int i = 0; i < 16; i++)
        {
            if (cells[i].childCount == 0) continue; 
            Transform currentTile = cells[i].GetChild(0);

            if ((i + 1) % 4 != 0 && cells[i + 1].childCount > 0)
            {
                Transform rightTile = cells[i + 1].GetChild(0);
                string key = GetMergeKey(currentTile.name, rightTile.name);
                
                if (mergeDictionary.ContainsKey(key)) 
                {
                    MergeRecipe recipe = mergeDictionary[key];
                    bool isGoal = false;
                    foreach(var goal in currentGoals) if(goal.targetPrefab == recipe.resultPrefab) isGoal = true;

                    if (isGoal) { ActivateHint(currentTile, rightTile, recipe.scientificHint, gm); return; }
                    else if (!foundNormalMatch) { fallbackT1 = currentTile; fallbackT2 = rightTile; fallbackHint = recipe.scientificHint; foundNormalMatch = true; }
                }
            }
            
            if (i + 4 < 16 && cells[i + 4].childCount > 0)
            {
                Transform downTile = cells[i + 4].GetChild(0);
                string key = GetMergeKey(currentTile.name, downTile.name);
                
                if (mergeDictionary.ContainsKey(key)) 
                {
                    MergeRecipe recipe = mergeDictionary[key];
                    bool isGoal = false;
                    foreach(var goal in currentGoals) if(goal.targetPrefab == recipe.resultPrefab) isGoal = true;

                    if (isGoal) { ActivateHint(currentTile, downTile, recipe.scientificHint, gm); return; }
                    else if (!foundNormalMatch) { fallbackT1 = currentTile; fallbackT2 = downTile; fallbackHint = recipe.scientificHint; foundNormalMatch = true; }
                }
            }
        }

        if (foundNormalMatch) ActivateHint(fallbackT1, fallbackT2, fallbackHint, gm);
        else UIManager.Instance.ShowHintMessage("Şu an matriste yapılabilecek hiçbir kimyasal sentez göremiyorum! Parçalamayı veya Geri Almayı denemelisin.");
    }

    private void ActivateHint(Transform t1, Transform t2, string hintMsg, GameManager gm)
    {
        int currentCost = GetCurrentHintCost(); 
        if (gm != null) gm.SubtractScore(currentCost); 

        usedHints++; 

        activeHintTile1 = t1;
        activeHintTile2 = t2;

        t1.DOScale(Vector3.one * 1.15f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        t2.DOScale(Vector3.one * 1.15f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        string coreMsg = string.IsNullOrEmpty(hintMsg) ? "Bu iki elementi birleştirmek harika bir fikir olabilir!" : hintMsg;
        
        int remainingHints = maxHints - usedHints;
        string nextCostText = (remainingHints > 0) ? GetCurrentHintCost().ToString() : "-";
        
        string infoFooter = $"\n\n<size=80%><color=#F1C40F>Kalan İpucu Hakkın: {remainingHints} | Sonraki Bedel: {nextCostText} Puan</color></size>";
        
        UIManager.Instance.ShowHintMessage(coreMsg + infoFooter);
    }

    public void StopHintHighlight()
    {
        if (activeHintTile1 != null) { activeHintTile1.DOKill(); activeHintTile1.localScale = Vector3.one; }
        if (activeHintTile2 != null) { activeHintTile2.DOKill(); activeHintTile2.localScale = Vector3.one; }
    }
}