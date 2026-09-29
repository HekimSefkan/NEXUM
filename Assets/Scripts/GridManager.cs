using System.Collections.Generic;
using UnityEngine;
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
    public int savedEmptyShiftCount;    // entropi (basınç) sayacı
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
    private int emptyShiftCount = 0; 
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
        List<Transform> emptyCells = new List<Transform>();
        foreach (Transform cell in cells) if (cell.childCount == 0) emptyCells.Add(cell);
        
        if (emptyCells.Count == 0) return;

        int randomIndex = Random.Range(0, emptyCells.Count);
        GameObject selectedElement = LevelManager.Instance.GetRandomElementForCurrentLevel();
        GameObject newTile = Instantiate(selectedElement, emptyCells[randomIndex]);
        newTile.transform.localScale = Vector3.zero;
        newTile.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void SaveCurrentState()
    {
        GridSnapshot snapshot = new GridSnapshot();
        GameManager gm = FindObjectOfType<GameManager>();
        snapshot.savedScore = gm != null ? gm.currentScore : 0; 
        snapshot.savedEmptyShiftCount = emptyShiftCount;
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

            SpawnTile();
            emptyShiftCount = 0;
            UIManager.Instance.UpdatePressureMeter(emptyShiftCount);
            
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

            if (currentGameMode != 2) 
            {
                emptyShiftCount++;
                UIManager.Instance.UpdatePressureMeter(emptyShiftCount);
                
                if (emptyShiftCount >= 5)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.errorClip);
                    }

                    if (Camera.main != null)
                    {
                        Camera.main.transform.DOShakePosition(0.3f, 0.4f, 15, 90f);
                    }

                    Handheld.Vibrate();

                    SpawnTile(); 
                    emptyShiftCount = 0; 
                    DOVirtual.DelayedCall(0.3f, () => { UIManager.Instance.UpdatePressureMeter(emptyShiftCount); });
                    Debug.Log("Laboratuvarda entropi patlaması! Ceza elementi eklendi.");
                }
            }
        }
        else
        {
            // Hiçbir şey değişmedi: bu hamle için alınan anlık görüntü geri alınır,
            // yoksa geçersiz hamleler geri alma yığınını şişiriyor.
            if (historyStack.Count > 0) historyStack.Pop();
        }

        CheckGameOver();
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

        emptyShiftCount = lastState.savedEmptyShiftCount;
        UIManager.Instance.UpdatePressureMeter(emptyShiftCount);

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