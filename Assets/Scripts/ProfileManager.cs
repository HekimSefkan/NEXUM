using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using DG.Tweening;

public class ProfileManager : MonoBehaviour
{
    [Header("Hero Section (Kimlik Kartı)")]
    public TextMeshProUGUI profileNameText;
    public TextMeshProUGUI profileIDText; 
    public Image profileAvatarImage;
    
    [Header("E-Posta Sistemi")]
    public TextMeshProUGUI profileEmailText;
    public GameObject addEmailButtonObj; // Ekle butonu
    
    [Header("E-Posta Ekleme Pop-up")]
    public GameObject emailUpdatePanel;
    public TMP_InputField emailInputField;
    public TextMeshProUGUI emailWarningText; 

    [Header("Ana Menü Butonu")]
    public Image mainMenuAvatarImage;

    [Header("İstatistikler (Mini Kartlar)")]
    public TextMeshProUGUI totalSynthesisText;
    public TextMeshProUGUI totalDiscoveredText; 
    public TextMeshProUGUI quizSuccessText;
    public TextMeshProUGUI accidentsText;

    [Header("Veritabanı")]
    public Sprite[] avatarSprites;

    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            LoadProfileData();
        }
    }

    public void LoadProfileData()
    {
        // 1. Temel Verileri Çek
        string playerName = PlayerPrefs.GetString("PlayerName", "Baş Kimyager");
        string playerEmail = PlayerPrefs.GetString("PlayerEmail", ""); // E-postayı çek
        int maxLevel = PlayerPrefs.GetInt("MaxLevelUnlocked", 1);
        int avatarIndex = PlayerPrefs.GetInt("PlayerAvatarIndex", 0);

        int totalSynthesis = PlayerPrefs.GetInt("TotalSynthesis", 0);
        int totalAccidents = PlayerPrefs.GetInt("TotalAccidents", 0);
        int quizAttempts = PlayerPrefs.GetInt("QuizAttempts", 0);
        int quizCorrect = PlayerPrefs.GetInt("QuizCorrect", 0);

        if (!PlayerPrefs.HasKey("NexumID"))
        {
            int randomID = Random.Range(1000, 9999);
            PlayerPrefs.SetString("NexumID", "NX-" + randomID.ToString());
            PlayerPrefs.Save();
        }
        string nexumID = PlayerPrefs.GetString("NexumID");

        string quizRatioText = "%0";
        if (quizAttempts > 0)
        {
            float ratio = ((float)quizCorrect / quizAttempts) * 100f;
            quizRatioText = "%" + ratio.ToString("F0");
        }

        // --- ARAYÜZ (UI) GÜNCELLEMELERİ ---

        if (profileNameText != null) profileNameText.text = playerName;
        if (profileIDText != null) profileIDText.text = "Kimlik No: " + nexumID;
        
        // E-Posta Kontrolü ve UI Güncellemesi
        if (string.IsNullOrEmpty(playerEmail))
        {
            if (profileEmailText != null) profileEmailText.text = "E-posta bağlanmadı";
            if (addEmailButtonObj != null) addEmailButtonObj.SetActive(true); // Ekle butonunu göster
        }
        else
        {
            if (profileEmailText != null) profileEmailText.text = playerEmail;
            if (addEmailButtonObj != null) addEmailButtonObj.SetActive(false); // Ekle butonunu gizle
        }
        
        if (avatarIndex >= 0 && avatarIndex < avatarSprites.Length)
        {
            if (profileAvatarImage != null) profileAvatarImage.sprite = avatarSprites[avatarIndex];
            if (mainMenuAvatarImage != null) mainMenuAvatarImage.sprite = avatarSprites[avatarIndex];
        }

        if (totalSynthesisText != null) totalSynthesisText.text = totalSynthesis.ToString();
        if (totalDiscoveredText != null) totalDiscoveredText.text = maxLevel.ToString(); 
        if (quizSuccessText != null) quizSuccessText.text = quizRatioText;
        if (accidentsText != null) accidentsText.text = totalAccidents.ToString();
    }

    // --- E-POSTA POP-UP KONTROLLERİ ---
    
    public void OpenEmailPopup()
    {
        if (emailWarningText != null) emailWarningText.text = "<color=#ADB5BD>Yeni E-posta adresini girin:</color>";
        if (emailInputField != null) emailInputField.text = ""; // Kutuyu temizle
        
        emailUpdatePanel.SetActive(true);
        emailUpdatePanel.transform.localScale = Vector3.zero;
        emailUpdatePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void CloseEmailPopup()
    {
        emailUpdatePanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
            emailUpdatePanel.SetActive(false);
        });
    }

    private bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    public void SaveNewEmail()
    {
        string newEmail = emailInputField.text;

        // Güvenlik: Geçersiz format engellemesi
        if (!IsValidEmail(newEmail))
        {
            if (emailWarningText != null) emailWarningText.text = "<color=red>Kabul edilmeyen E-Posta formatı!</color>";
            if (emailInputField != null) emailInputField.transform.DOShakePosition(0.4f, new Vector3(15f, 0, 0), 20);
            return;
        }

        // Başarılı kayıt işlemi
        PlayerPrefs.SetString("PlayerEmail", newEmail);
        PlayerPrefs.Save();
        
        LoadProfileData(); // Ekrandaki yazıyı anında yenile
        CloseEmailPopup(); // Pop-up'ı kapat
    }
}