using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("Arayüz Bağlantıları")]
    public GameObject gameOverPanel; 
    public GameObject winPanel; 

    [Header("Beherglas Hedef (Titrasyon) Sistemi")]
    public Transform goalsContainer; 
    public GameObject beakerGoalPrefab; 
    private List<UnityEngine.UI.Image> goalFills = new List<UnityEngine.UI.Image>(); 
    private List<TextMeshProUGUI> goalTexts = new List<TextMeshProUGUI>(); 

    [Header("Eğitim ve Duraklatma Paneli")]
    public GameObject pausePanel;
    public TextMeshProUGUI factTextUI; 
    public List<ChemistryFact> allFacts; 

    [Header("Quiz (Kurtarma) Sistemi")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionTextUI; 
    public TextMeshProUGUI[] optionTexts;  
    public List<ChemistryQuiz> allQuizzes; 
    private int currentCorrectIndex; 

    [Header("Game Over & Revive Butonu")]
    public GameObject reviveButton; 
    public GameObject[] optionButtonObjects; 

    [Header("Hipotez (Joker) Paneli")]
    public GameObject hypothesisPanel;

    [Header("Tersinir Tepkime Paneli")]
    public GameObject undoPanel;
    public TextMeshProUGUI undoInfoText; 

    [Header("Asistan (İpucu) Sistemi")]
    public GameObject focusPanel;
    public GameObject assistantPanel;
    public TextMeshProUGUI hintMessageText;

    [Header("Asistan Avatar Ayarları")]
    public UnityEngine.UI.Image assistantAvatarImage; 
    public Sprite[] avatarSprites; 

    [Header("Sistem Basıncı (Entropi) UI")]
    public UnityEngine.UI.Image pressureLiquidFill; 
    private float[] pressureSteps = { 0f, 0.340f, 0.500f, 0.618f, 0.745f, 1f };
    
    [Header("Deney Föyü (Tutorial) Sistemi")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialTitleText;
    public TextMeshProUGUI tutorialContentText;
    public UnityEngine.UI.Image largeImage1; 
    public UnityEngine.UI.Image largeImage2; 
    
    [Header("Kombo Bildirim (Yüzen Yazı) Sistemi")]
    public GameObject comboTextPrefab; 
    public Transform canvasTransform; 

    [Header("Görsel Efektler")]
    public UnityEngine.UI.Image dangerGlowImage; // YENİ: Kırmızı alarm ışığı

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        InitGoalsUI(); 

        int currentLevel = LevelManager.Instance.currentLevelIndex;
        int isTutorialRead = PlayerPrefs.GetInt("TutorialRead_Level_" + currentLevel, 0);

        if (isTutorialRead == 0)
        {
            ShowTutorialPanel(currentLevel);
        }
        else
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void InitGoalsUI()
    {
        foreach(Transform child in goalsContainer) Destroy(child.gameObject);
        goalFills.Clear();
        goalTexts.Clear();

        var currentGoals = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex].levelGoals;

        foreach(var goal in currentGoals)
        {
            GameObject newBeaker = Instantiate(beakerGoalPrefab, goalsContainer);
            UnityEngine.UI.Image icon = newBeaker.transform.Find("ElementIcon").GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image targetImage = goal.targetPrefab.GetComponent<UnityEngine.UI.Image>();
            
            if (targetImage != null) 
            {
                icon.sprite = targetImage.sprite; 
                icon.color = targetImage.color; 
            }

            UnityEngine.UI.Image fill = newBeaker.transform.Find("LiquidFill").GetComponent<UnityEngine.UI.Image>();
            fill.fillAmount = 0;
            goalFills.Add(fill);

            string cleanName = goal.targetPrefab.name.Replace("Tile_", "").Replace("(Clone)", "");
            string chemName = cleanName.Replace("2", "<sub>2</sub>").Replace("3", "<sub>3</sub>").Replace("4", "<sub>4</sub>");

            TextMeshProUGUI text = newBeaker.transform.Find("AmountText").GetComponent<TextMeshProUGUI>();
            text.text = $"<b>{chemName}</b>\n0 / {goal.targetAmount}";
            goalTexts.Add(text);
        }
    }

    public void UpdateGoalUI()
    {
        var currentGoals = LevelManager.Instance.levels[LevelManager.Instance.currentLevelIndex].levelGoals;

        for(int i = 0; i < currentGoals.Count; i++)
        {
            var goal = currentGoals[i];
            float fillRatio = (float)goal.currentAmount / goal.targetAmount;
            
            goalFills[i].DOFillAmount(fillRatio, 0.5f).SetEase(Ease.OutBounce);
            
            string cleanName = goal.targetPrefab.name.Replace("Tile_", "").Replace("(Clone)", "");
            string chemName = cleanName.Replace("2", "<sub>2</sub>").Replace("3", "<sub>3</sub>").Replace("4", "<sub>4</sub>");

            string color = (goal.currentAmount >= goal.targetAmount) ? "#2ECC71" : "#FFFFFF";
            goalTexts[i].text = $"<b>{chemName}</b>\n<color={color}>{goal.currentAmount} / {goal.targetAmount}</color>";
        }
    }

    public void ShowUndoPanel() 
    { 
        int cost = GridManager.Instance.undoCost;
        int remaining = GridManager.Instance.currentUndoLimit - GridManager.Instance.usedUndos;
        GameManager gm = FindObjectOfType<GameManager>();
        int currentScore = (gm != null) ? gm.currentScore : 0;

        string costColor = (currentScore >= cost) ? "green" : "red";
        string limitColor = (remaining > 0) ? "green" : "red";

        undoInfoText.text = $"Kimyada bazı reaksiyonlar geri döndürülebilir. Bu işlem matrisi bir önceki hamleye geri alır.\n\n" +
                            $"Bedeli: <color={costColor}>{cost} Puan</color>\n" +
                            $"Kalan Hakkın: <color={limitColor}>{remaining}</color>";

        undoPanel.SetActive(true); 
    }
    public void HideUndoPanel() { undoPanel.SetActive(false); }

    public void ShowQuizPanel()
    {
        quizPanel.SetActive(true);
        int randomIndex = Random.Range(0, allQuizzes.Count);
        ChemistryQuiz selectedQuiz = allQuizzes[randomIndex];
        questionTextUI.text = selectedQuiz.questionText;
        currentCorrectIndex = selectedQuiz.correctAnswerIndex;
        for (int i = 0; i < optionTexts.Length; i++) optionTexts[i].text = selectedQuiz.options[i];
    }

    public void CheckAnswer(int selectedIndex) { StartCoroutine(ProvideFeedbackRoutine(selectedIndex)); }

    private System.Collections.IEnumerator ProvideFeedbackRoutine(int selectedIndex)
    {
        foreach (var btn in optionButtonObjects) btn.SetActive(false);

        int totalAttempts = PlayerPrefs.GetInt("QuizAttempts", 0) + 1;
        PlayerPrefs.SetInt("QuizAttempts", totalAttempts);

        if (selectedIndex == currentCorrectIndex)
        {
            if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.quizCorrectClip);
            
            int totalCorrect = PlayerPrefs.GetInt("QuizCorrect", 0) + 1;
            PlayerPrefs.SetInt("QuizCorrect", totalCorrect);
            PlayerPrefs.Save();

            questionTextUI.text = "<color=green>TEBRİKLER! DOĞRU CEVAP.</color>\nMatris temizleniyor, laboratuvara geri dönüyorsun...";
            yield return new WaitForSeconds(2f); 
            quizPanel.SetActive(false); 
            GridManager.Instance.ApplyReviveBonus(); 
        }
        else
        {
            if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.quizWrongClip);
            PlayerPrefs.Save(); 
            
            questionTextUI.text = "<color=red>MAALESEF YANLIŞ CEVAP!</color>\nLaboratuvar tamamen kilitlendi.";
            yield return new WaitForSeconds(2f); 
            quizPanel.SetActive(false);
            ShowGameOver(); 
        }
        
        foreach (var btn in optionButtonObjects) btn.SetActive(true);
    }

    public void TogglePause(bool isPaused)
    {
        if (isPaused)
        {
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            if (allFacts != null && allFacts.Count > 0) factTextUI.text = allFacts[Random.Range(0, allFacts.Count)].factText;
        }
        else { Time.timeScale = 1f; pausePanel.SetActive(false); }
    }

    public void ShowGameOver()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverClip);
        gameOverPanel.SetActive(true);
        reviveButton.SetActive(!GridManager.Instance.hasUsedRevive);
    }

    public void ShowWinScreen()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.winClip);
        winPanel.SetActive(true);
        winPanel.transform.localScale = Vector3.zero;
        winPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        int currentLevelIndex = LevelManager.Instance.currentLevelIndex;
        int highestUnlocked = PlayerPrefs.GetInt("UnlockedLevel", 0); 
        if (currentLevelIndex >= highestUnlocked)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentLevelIndex + 1);
            PlayerPrefs.SetInt("MaxLevelUnlocked", currentLevelIndex + 2); 
            PlayerPrefs.Save(); 
        }
    }

    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { SceneManager.LoadScene("MainMenu"); }
    public void NextLevel()
    {
        int nextLevel = PlayerPrefs.GetInt("SelectedLevel", 0) + 1;
        if(nextLevel >= LevelManager.Instance.levels.Count) SceneManager.LoadScene("MainMenu");
        else { PlayerPrefs.SetInt("SelectedLevel", nextLevel); SceneManager.LoadScene("Game"); }
    }

    public void ShowHypothesisPanel() { hypothesisPanel.SetActive(true); }
    public void HideHypothesisPanel() { hypothesisPanel.SetActive(false); }

    public void ShowHintMessage(string message)
    {
        hintMessageText.text = message;
        focusPanel.SetActive(true);
        assistantPanel.SetActive(true);

        if (assistantAvatarImage != null && avatarSprites != null && avatarSprites.Length > 0)
        {
            int avatarIndex = PlayerPrefs.GetInt("PlayerAvatarIndex", 0);
            if (avatarIndex < avatarSprites.Length) assistantAvatarImage.sprite = avatarSprites[avatarIndex];
        }
    }
    public void HideHintMessage()
    {
        if (focusPanel != null) focusPanel.SetActive(false);
        if (assistantPanel != null) assistantPanel.SetActive(false);
        if (GridManager.Instance != null) GridManager.Instance.StopHintHighlight();
    }

    // YENİ GÜNCELLENEN: Basınç artınca kırmızı tehlike ışığı yanar
    public void UpdatePressureMeter(int currentStep)
    {
        if (pressureLiquidFill == null) return;
        if (currentStep >= 0 && currentStep < pressureSteps.Length)
        {
            float targetFill = pressureSteps[currentStep];
            pressureLiquidFill.DOFillAmount(targetFill, 0.25f);
            
            // Eğer sayaç 4'teyse (Kritik sınır), kırmızı ışığı yanıp söndür
            if (currentStep == 4)
            {
                pressureLiquidFill.transform.parent.DOShakePosition(0.4f, 12f);
                factTextUI.text = "<color=red>UYARI: Laboratuvar entropisi kritik seviyede! Bir sentez yapmalısın!</color>";
                
                if(dangerGlowImage != null)
                {
                    dangerGlowImage.DOFade(0.35f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetId("DangerAlarm");
                }
            }
            else 
            {
                // Sayaç düştüyse alarmı kapat
                if(dangerGlowImage != null)
                {
                    DOTween.Kill("DangerAlarm"); 
                    dangerGlowImage.DOFade(0f, 0.3f); 
                }
            }
        }
    }

    private void ShowTutorialPanel(int levelIndex)
    {
        LevelData currentLevelData = LevelManager.Instance.levels[levelIndex];

        if (!string.IsNullOrEmpty(currentLevelData.tutorialTitle)) tutorialTitleText.text = currentLevelData.tutorialTitle;
        if (!string.IsNullOrEmpty(currentLevelData.tutorialContent)) tutorialContentText.text = currentLevelData.tutorialContent;

        if (currentLevelData.tutorialLargeSprite1 != null)
        {
            largeImage1.sprite = currentLevelData.tutorialLargeSprite1;
            largeImage1.color = Color.white; 
            largeImage1.gameObject.SetActive(true);
        }
        else largeImage1.gameObject.SetActive(false); 

        if (currentLevelData.tutorialLargeSprite2 != null)
        {
            largeImage2.sprite = currentLevelData.tutorialLargeSprite2;
            largeImage2.color = Color.white;
            largeImage2.gameObject.SetActive(true);
        }
        else largeImage2.gameObject.SetActive(false); 

        GridManager.Instance.enabled = false;
        tutorialPanel.SetActive(true);
    }

    public void StartExperiment()
    {
        int currentLevel = LevelManager.Instance.currentLevelIndex;
        PlayerPrefs.SetInt("TutorialRead_Level_" + currentLevel, 1);
        PlayerPrefs.Save();
        tutorialPanel.SetActive(false);
        GridManager.Instance.enabled = true;
    }

    public void ShowComboText(int comboCount, Vector3 spawnPosition)
    {
        if (comboTextPrefab == null || canvasTransform == null) return;

        GameObject floatingObj = Instantiate(comboTextPrefab, spawnPosition, Quaternion.identity, canvasTransform);
        TextMeshProUGUI tmpText = floatingObj.GetComponent<TextMeshProUGUI>();
        
        if (tmpText == null) return;

        string comboMessage = "";
        Color comboColor = Color.white;

        if (comboCount == 1)
        {
            comboMessage = "BAŞARILI SENTEZ!";
            comboColor = new Color(0.2f, 0.8f, 0.2f); 
        }
        else if (comboCount == 2)
        {
            comboMessage = "ÇİFTE BAĞ!";
            comboColor = new Color(1f, 0.6f, 0f); 
        }
        else if (comboCount >= 3)
        {
            comboMessage = "ZİNCİRLEME REAKSİYON!";
            comboColor = new Color(1f, 0.2f, 0.2f); 
        }

        tmpText.text = comboMessage;
        tmpText.color = comboColor;

        floatingObj.transform.localScale = Vector3.zero;
        floatingObj.transform.DOScale(Vector3.one * 1.2f, 0.3f).SetEase(Ease.OutBack);

        Sequence seq = DOTween.Sequence();
        seq.Append(floatingObj.transform.DOMoveY(150f, 1.5f).SetRelative().SetEase(Ease.OutQuad)); 
        seq.Join(tmpText.DOFade(0, 1.5f).SetEase(Ease.InExpo)); 
        seq.OnComplete(() => Destroy(floatingObj)); 
    }

    public void PlayButtonSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound();
    }
}