using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    // Sahne başında bir kez bulunur; her panel açılışında aranmaz
    private GameManager gameManager;

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
    private Vector2 quizQuestionPos;      // Soru metninin tasarımdaki yeri
    private Vector2 quizQuestionSize;
    private bool quizRectCaptured;

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
    public TextMeshProUGUI catalystLabelText;   // göstergenin ne işe yaradığını söyleyen başlık
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

    // Girdiyi engelleyen (modal) paneller: açıkken swipe ve klavye hamlesi işlenmez.
    // AssistantPanel ve FocusPanel bilerek dışarıda: ipucu mesajı oyunu durdurmaz,
    // oyuncu mesaj ekrandayken hamle yapabilmeli.
    // Her karede çağrıldığı için arama yapmaz; sadece activeInHierarchy okur.
    public bool IsModalPanelOpen
    {
        get
        {
            return IsPanelOpen(tutorialPanel)
                || IsPanelOpen(pausePanel)
                || IsPanelOpen(quizPanel)
                || IsPanelOpen(gameOverPanel)
                || IsPanelOpen(winPanel)
                || IsPanelOpen(hypothesisPanel)
                || IsPanelOpen(undoPanel);
        }
    }

    private static bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    void Awake()
    {
        Instance = this;
        gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        InitGoalsUI(); 

        int currentLevel = LevelManager.Instance.currentLevelIndex;
        int isTutorialRead = SaveService.IsTutorialRead(currentLevel) ? 1 : 0;

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

        // Serbest modda bölüm hedefi yok; beher paneli hiç gösterilmez.
        // Ayar doğrudan kayıttan okunur: GridManager.Start ile UIManager.Start
        // arasındaki sıra garanti değil.
        bool freeMode = SaveService.Data.gameMode == 2;
        goalsContainer.gameObject.SetActive(!freeMode);
        if (freeMode) return;

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
        if (goalFills.Count == 0) return;   // serbest modda beher yok

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
        GameManager gm = gameManager != null ? gameManager : FindObjectOfType<GameManager>();
        int currentScore = (gm != null) ? gm.currentScore : 0;

        string costColor = (currentScore >= cost) ? "green" : "red";
        string limitColor = (remaining > 0) ? "green" : "red";

        undoInfoText.text = Loc.Format(CodeStrings.UndoExplain, costColor, cost, limitColor, remaining);

        undoPanel.SetActive(true); 
    }
    public void HideUndoPanel() { undoPanel.SetActive(false); }

    public void ShowQuizPanel()
    {
        // İkinci şans hakkı quiz açıldığı anda tüketilir: yanlış cevap verilse de panel kapatılıp
        // tekrar açılsa da aynı oyun oturumunda ikinci bir hak doğmaz.
        if (GridManager.Instance != null) GridManager.Instance.hasUsedRevive = true;

        // Soru düzeni: metin üstte. Geri bildirimde ortaya alınıyor, tekrar açılışta eski yerine döner.
        if (questionTextUI != null)
        {
            if (!quizRectCaptured)
            {
                quizQuestionPos = questionTextUI.rectTransform.anchoredPosition;
                quizQuestionSize = questionTextUI.rectTransform.sizeDelta;
                quizRectCaptured = true;
            }
            else
            {
                questionTextUI.rectTransform.anchoredPosition = quizQuestionPos;
                questionTextUI.rectTransform.sizeDelta = quizQuestionSize;
            }
        }

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

        // Şıklar gizlendi; geri bildirim metni panelin ortasında dursun (üstte sıkışmasın)
        if (questionTextUI != null && quizRectCaptured)
        {
            questionTextUI.rectTransform.anchoredPosition = new Vector2(quizQuestionPos.x, 0f);
            questionTextUI.rectTransform.sizeDelta = new Vector2(quizQuestionSize.x, 700f);
        }

        int totalAttempts = SaveService.Data.quizAttempts + 1;
        SaveService.Data.quizAttempts = totalAttempts;

        if (selectedIndex == currentCorrectIndex)
        {
            if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.quizCorrectClip);
            
            int totalCorrect = SaveService.Data.quizCorrect + 1;
            SaveService.Data.quizCorrect = totalCorrect;
            SaveService.SaveNow();

            questionTextUI.text = Loc.Get(CodeStrings.QuizCorrect);
            yield return new WaitForSeconds(2f); 
            quizPanel.SetActive(false); 
            GridManager.Instance.ApplyReviveBonus(); 
        }
        else
        {
            if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.quizWrongClip);
            SaveService.SaveNow(); 
            
            questionTextUI.text = Loc.Get(CodeStrings.QuizWrong);
            yield return new WaitForSeconds(2f); 
            quizPanel.SetActive(false);
            ShowGameOver(); 
        }
        
        foreach (var btn in optionButtonObjects) btn.SetActive(true);
    }

    public void TogglePause(bool isPaused)
    {
        PlayButtonSound();   // PausePanel'deki "Devam Et" butonunun OnClick'inde ses yok

        if (isPaused)
        {
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            if (allFacts != null && allFacts.Count > 0) factTextUI.text = allFacts[Random.Range(0, allFacts.Count)].factText;
        }
        else { Time.timeScale = 1f; pausePanel.SetActive(false); }
    }

    // Kaza sayacı oyun başına bir kez artar. Oyun sonu ekranı ikinci şans quizi
    // yanlış cevaplanınca tekrar açıldığı için aynı oyun iki kez sayılmamalı.
    // Bayrak sahne örneğinde durur; yeniden başlatınca (sahne yüklenince) sıfırlanır.
    private bool accidentCounted = false;

    public void ShowGameOver()
    {
        // Serbest modda kaybetme yok: modal açılmaz, matris kendiliğinden
        // yeniden düzenlenir ve oyuncuya kısa bir bildirim gösterilir.
        if (GridManager.Instance != null && GridManager.Instance.IsFreeMode)
        {
            GridManager.Instance.ReshuffleMatrix();
            return;
        }

        if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverClip);

        if (!accidentCounted)
        {
            accidentCounted = true;
            SaveService.Data.totalAccidents++;
            SaveService.SaveNow();
        }

        gameOverPanel.SetActive(true);
        reviveButton.SetActive(!GridManager.Instance.hasUsedRevive);
    }

    /// <summary>
    /// Kombo yazısıyla aynı bantta, ekranın ortasında kısa bir sistem bildirimi.
    /// Kombo prefab'ı yeniden kullanılır; yeni sahne/prefab objesi gerekmez.
    /// </summary>
    public void ShowSystemMessage(string message, float duration = 1.5f)
    {
        if (comboTextPrefab == null || canvasTransform == null) return;

        float scale = 1f;
        Canvas parentCanvas = canvasTransform.GetComponentInParent<Canvas>();
        if (parentCanvas != null) scale = parentCanvas.scaleFactor;

        Vector3 position = new Vector3(Screen.width * 0.5f,
                                       Screen.height * 0.5f + ComboAnchorY * scale, 0f);

        GameObject floatingObj = Instantiate(comboTextPrefab, position, Quaternion.identity, canvasTransform);
        TextMeshProUGUI tmpText = floatingObj.GetComponent<TextMeshProUGUI>();
        if (tmpText == null) { Destroy(floatingObj); return; }

        tmpText.text = message;
        tmpText.color = new Color(0.56f, 0.85f, 1f);   // #8FD8FF, HUD vurgu rengi

        floatingObj.transform.localScale = Vector3.zero;
        floatingObj.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(duration * 0.55f);
        seq.Append(tmpText.DOFade(0f, duration * 0.45f).SetEase(Ease.InQuad));
        seq.SetUpdate(true).OnComplete(() => Destroy(floatingObj));
    }

    public void ShowWinScreen()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.winClip);
        winPanel.SetActive(true);
        winPanel.transform.localScale = Vector3.zero;
        winPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        int currentLevelIndex = LevelManager.Instance.currentLevelIndex;
        int highestUnlocked = SaveService.Data.unlockedLevel; 
        if (currentLevelIndex >= highestUnlocked)
        {
            SaveService.Data.unlockedLevel = currentLevelIndex + 1;
            SaveService.Data.maxLevelUnlocked = currentLevelIndex + 2;
            SaveService.SaveNow(); 
        }
    }

    // Sahne yükleyen üç metotta da ses koddan çalınır: PausePanel'deki "Ana Menü" ve
    // GameOverPanel'deki "Yeniden Başlat" butonlarının OnClick'inde ses bağlı değil.
    public void RestartGame()
    {
        PlayButtonSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        PlayButtonSound();
        SceneManager.LoadScene("MainMenu");
    }

    public void NextLevel()
    {
        PlayButtonSound();
        int nextLevel = SaveService.Data.selectedLevel + 1;
        if(nextLevel >= LevelManager.Instance.levels.Count) SceneManager.LoadScene("MainMenu");
        else { SaveService.Data.selectedLevel = nextLevel; SaveService.SaveNow(); SceneManager.LoadScene("Game"); }
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
            int avatarIndex = SaveService.Data.avatarIndex;
            if (avatarIndex < avatarSprites.Length) assistantAvatarImage.sprite = avatarSprites[avatarIndex];
        }
    }
    public void HideHintMessage()
    {
        if (focusPanel != null) focusPanel.SetActive(false);
        if (assistantPanel != null) assistantPanel.SetActive(false);
        if (GridManager.Instance != null) GridManager.Instance.StopHintHighlight();
    }

    // Katalizör şarjı: her sentezde dolar, dolunca bedava Joker hakkı verir.
    // (Eskiden entropi cezasını gösteriyordu; ceza sistemi kaldırıldı.)
    public void UpdateCatalystMeter(int charge, int freeJokers)
    {
        int limit = GridManager.CatalystChargeLimit;
        int step = Mathf.Clamp(charge, 0, pressureSteps.Length - 1);

        if (pressureLiquidFill != null)
        {
            pressureLiquidFill.DOFillAmount(pressureSteps[step], 0.25f);
        }

        if (catalystLabelText != null)
        {
            catalystLabelText.text = freeJokers > 0
                ? Loc.Format(CodeStrings.CatalystReadyLabel, freeJokers)
                : Loc.Format(CodeStrings.CatalystChargeLabel, charge, limit);
        }

        // Bedava hak varken gösterge yeşil parlar, yoksa söner
        if (dangerGlowImage != null)
        {
            DOTween.Kill("CatalystReady");
            if (freeJokers > 0)
            {
                dangerGlowImage.color = new Color(0.18f, 0.8f, 0.44f, dangerGlowImage.color.a);
                dangerGlowImage.DOFade(0.35f, 0.8f).SetLoops(-1, LoopType.Yoyo)
                    .SetId("CatalystReady").SetLink(gameObject);
            }
            else
            {
                dangerGlowImage.DOFade(0f, 0.3f);
            }
        }
    }

    // Şarj dolduğunda: görsel + sesli geri bildirim
    public void ShowCatalystReady(int freeJokers)
    {
        if (pressureLiquidFill != null && pressureLiquidFill.transform.parent != null)
        {
            Transform meter = pressureLiquidFill.transform.parent;
            meter.DOKill(true);
            meter.DOPunchScale(Vector3.one * 0.12f, 0.45f, 8, 0.6f);
        }

        if (factTextUI != null)
        {
            factTextUI.text = Loc.Get(CodeStrings.CatalystReadyMessage);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.comboClip);
        }

        Handheld.Vibrate();
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
        SaveService.SetTutorialRead(currentLevel);
        SaveService.Flush();
        tutorialPanel.SetActive(false);
        GridManager.Instance.enabled = true;
    }

    // Kombo yazısı birleşmenin olduğu yerde doğunca grid'in üst sırasının üstüne biniyordu.
    // Artık grid ile skor göstergesi arasındaki boş banda sabitleniyor (referans birimi,
    // kanvas merkezine göre): grid üstü 290, ScoreButton altı 411 -> bant 121 birim.
    // Yazı 45 birim yüksekliğinde ve 45 birim yükseldiği için 328 merkezde iki yana
    // 15'er birim pay kalır. Yatayda birleşmenin sütunu korunur, ekran dışına taşmaz.
    private const float ComboAnchorY = 328f;
    private const float ComboFloatY = 45f;
    // Kombo/sistem yazısının kutusu 700 birim geniş (ComboTextPrefab);
    // ekran kenarına taşmaması için yarı genişliği kadar içeride tutulur.
    private const float ComboHalfWidth = 350f;

    public void ShowComboText(int comboCount, Vector3 spawnPosition)
    {
        if (comboTextPrefab == null || canvasTransform == null) return;

        float scale = 1f;
        Canvas parentCanvas = canvasTransform.GetComponentInParent<Canvas>();
        if (parentCanvas != null) scale = parentCanvas.scaleFactor;

        // Overlay canvas'ta dünya koordinatı = ekran pikseli
        float posY = Screen.height * 0.5f + ComboAnchorY * scale;
        float margin = ComboHalfWidth * scale;
        float posX = Mathf.Clamp(spawnPosition.x, margin, Screen.width - margin);
        spawnPosition = new Vector3(posX, posY, 0f);

        GameObject floatingObj = Instantiate(comboTextPrefab, spawnPosition, Quaternion.identity, canvasTransform);
        TextMeshProUGUI tmpText = floatingObj.GetComponent<TextMeshProUGUI>();
        
        if (tmpText == null) return;

        string comboMessage = "";
        Color comboColor = Color.white;

        if (comboCount == 1)
        {
            comboMessage = Loc.Get(CodeStrings.Combo1);
            comboColor = new Color(0.2f, 0.8f, 0.2f); 
        }
        else if (comboCount == 2)
        {
            comboMessage = Loc.Get(CodeStrings.Combo2);
            comboColor = new Color(1f, 0.6f, 0f); 
        }
        else if (comboCount >= 3)
        {
            comboMessage = Loc.Get(CodeStrings.Combo3);
            comboColor = new Color(1f, 0.2f, 0.2f); 
        }

        tmpText.text = comboMessage;
        tmpText.color = comboColor;

        floatingObj.transform.localScale = Vector3.zero;
        floatingObj.transform.DOScale(Vector3.one * 1.2f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(floatingObj.transform.DOMoveY(ComboFloatY * scale, 1.5f).SetRelative().SetEase(Ease.OutQuad));
        seq.Join(tmpText.DOFade(0, 1.5f).SetEase(Ease.InExpo)); 
        seq.SetUpdate(true).OnComplete(() => Destroy(floatingObj)); 
    }

    // Buton sesi tek kaynaktan çalar. Bazı butonlarda ses OnClick listesinde bağlı,
    // bazılarında yok; sesi ilgili metotların içine de koyduğumuz için aynı karede
    // iki kez çalma riski doğuyor. Kare koruması bunu engeller (çift ses olmaz),
    // sahne OnClick listelerine dokunmak gerekmez.

    // Kare koruması AudioManager.PlayButtonSound içinde; burada yalnızca iletim var.
    // AudioManager.PlaySFX zaten "SfxOn" ayarına bakıyor; kapalıyken ses çıkmaz.
    public void PlayButtonSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound();
    }
}