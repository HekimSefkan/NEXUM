using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Text.RegularExpressions; // YENİ: E-posta doğrulama (Regex) için gerekli kütüphane

[System.Serializable]
public struct MentorData
{
    public string mentorName;
    [TextArea(2, 3)] public string description; 
    public string quote; 
}

public class RegistrationManager : MonoBehaviour
{
    [Header("Kayıt (Onboarding) Ekranı")]
    public GameObject registrationPanel;
    public TMP_InputField nameInputField;
    public TMP_InputField emailInputField; 
    public TextMeshProUGUI emailWarningText; // YENİ: Hata durumunda kızaracak olan küçük açıklama metni
    
    [Header("Mentor Seçim Sistemi (Grid)")]
    public MentorData[] mentors; 
    public Sprite[] avatarSprites; 
    public GameObject[] highlightRings; 
    public TextMeshProUGUI selectedMentorText; 
    public Button detailsButton; 
    private int selectedAvatarIndex = -1;

    [Header("Mentor Pop-up (Ayrıntılar Kartı)")]
    public GameObject mentorDetailPanel;
    public Image detailAvatar;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDesc;

    [Header("NEXUM Kimlik Kartı (Giriş Animasyonu)")]
    public GameObject idCardPanel;
    public TextMeshProUGUI idCardNameText;
    public TextMeshProUGUI idCardEmailText; 
    public TextMeshProUGUI idCardMentorText;
    public TextMeshProUGUI idCardQuoteText;
    public Image idCardAvatarImage;
    public CanvasGroup idCardCanvasGroup; 

    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            if(registrationPanel != null) registrationPanel.SetActive(false);
            if(idCardPanel != null) idCardPanel.SetActive(false);
            if(mentorDetailPanel != null) mentorDetailPanel.SetActive(false);
        }
        else
        {
            if(registrationPanel != null) registrationPanel.SetActive(true);
            if(idCardPanel != null) idCardPanel.SetActive(false);
            if(mentorDetailPanel != null) mentorDetailPanel.SetActive(false);
            
            ResetHighlightRings();
            if(selectedMentorText != null) selectedMentorText.text = "<color=#ADB5BD>Mentor seçilmedi...</color>";
            if(detailsButton != null) detailsButton.interactable = false; 
        }
    }

    private void ResetHighlightRings()
    {
        for (int i = 0; i < highlightRings.Length; i++)
        {
            if (highlightRings[i] != null) highlightRings[i].SetActive(false);
        }
    }

    public void SelectAvatar(int index)
    {
        selectedAvatarIndex = index;
        
        for (int i = 0; i < highlightRings.Length; i++)
        {
            if (highlightRings[i] != null) highlightRings[i].SetActive(i == index);
        }

        if (index >= 0 && index < mentors.Length)
        {
            selectedMentorText.text = $"Mentor: <b>{mentors[index].mentorName}</b>";
            if(detailsButton != null)
            {
                detailsButton.interactable = true;
                detailsButton.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.3f);
            }
        }
    }

    public void OpenMentorDetails()
    {
        if (selectedAvatarIndex == -1) return;

        MentorData mData = mentors[selectedAvatarIndex];
        
        detailName.text = mData.mentorName;
        detailDesc.text = mData.description;
        
        if (detailAvatar != null && avatarSprites.Length > selectedAvatarIndex)
        {
            detailAvatar.sprite = avatarSprites[selectedAvatarIndex];
        }

        mentorDetailPanel.SetActive(true);
        mentorDetailPanel.transform.localScale = Vector3.zero;
        mentorDetailPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
    }

    public void CloseMentorDetails()
    {
        mentorDetailPanel.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            mentorDetailPanel.SetActive(false);
        });
    }

    // YENİ EKLENEN FONKSİYON: E-Posta formatını denetler
    private bool IsValidEmail(string email)
    {
        // Standart Regex (Düzenli İfade) e-posta filtresi: x@y.z formatını arar
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    public void SaveProfileAndLogin()
    {
        string enteredName = nameInputField.text;
        string enteredEmail = emailInputField != null ? emailInputField.text : "";

        // İsim veya Avatar eksikse zaten girilmiyor
        if (string.IsNullOrWhiteSpace(enteredName) || selectedAvatarIndex == -1) return;

        // --- E-POSTA GÜVENLİK DUVARI ---
        // Eğer e-posta kutusu boş DEĞİLSE (yani bir şeyler yazılmışsa) kontrol et
        if (!string.IsNullOrWhiteSpace(enteredEmail))
        {
            // Eğer yazılan metin gerçek bir e-posta değilse
            if (!IsValidEmail(enteredEmail))
            {
                if (emailWarningText != null)
                {
                    // Uyarı metnini kırmızıya çevir ve hatayı söyle
                    emailWarningText.text = "<color=red>Kabul edilmeyen E-Posta formatı!</color>";
                    
                    // Giriş kutusunu oyuncuyu uyarmak için hafifçe titret
                    emailInputField.transform.DOShakePosition(0.4f, new Vector3(15f, 0, 0), 20);
                }
                
                // Oyuna girişi DURDUR (İşlemi iptal et)
                return; 
            }
        }

        // Eğer e-posta doğruysa (veya hiç girilmediyse) uyarı metnini eski gri haline döndür
        if (emailWarningText != null)
        {
            emailWarningText.text = "<color=#ADB5BD>Laboratuvar Kaydı (İsteğe Bağlı) - İlerlemeni farklı cihazlarda taşı</color>";
        }
        // -------------------------------

        PlayerPrefs.SetString("PlayerName", enteredName);
        PlayerPrefs.SetInt("PlayerAvatarIndex", selectedAvatarIndex);
        if (!string.IsNullOrWhiteSpace(enteredEmail)) PlayerPrefs.SetString("PlayerEmail", enteredEmail);

        if (!PlayerPrefs.HasKey("TotalScore")) PlayerPrefs.SetInt("TotalScore", 0);
        if (!PlayerPrefs.HasKey("MaxLevelUnlocked")) PlayerPrefs.SetInt("MaxLevelUnlocked", 1);
        if (!PlayerPrefs.HasKey("TotalSynthesis")) PlayerPrefs.SetInt("TotalSynthesis", 0);
        if (!PlayerPrefs.HasKey("TotalAccidents")) PlayerPrefs.SetInt("TotalAccidents", 0);
        if (!PlayerPrefs.HasKey("QuizAttempts")) PlayerPrefs.SetInt("QuizAttempts", 0);
        if (!PlayerPrefs.HasKey("QuizCorrect")) PlayerPrefs.SetInt("QuizCorrect", 0);

        PlayerPrefs.Save();

        StartCoroutine(ShowIDCardRoutine(enteredName, enteredEmail, selectedAvatarIndex));
    }

    private IEnumerator ShowIDCardRoutine(string playerName, string playerEmail, int mentorIndex)
    {
        idCardNameText.text = playerName;
        idCardMentorText.text = "Mentor: " + mentors[mentorIndex].mentorName;
        idCardQuoteText.text = $"<i>\"{mentors[mentorIndex].quote}\"</i>";

        if (idCardEmailText != null)
        {
            if (!string.IsNullOrWhiteSpace(playerEmail))
                idCardEmailText.text = playerEmail;
            else
                idCardEmailText.text = "Gözlemci Kaydı (İsimsiz Ağ)";
        }
        
        if (idCardAvatarImage != null && avatarSprites.Length > mentorIndex)
        {
            idCardAvatarImage.sprite = avatarSprites[mentorIndex];
        }

        idCardPanel.SetActive(true);
        if (idCardCanvasGroup != null) idCardCanvasGroup.alpha = 0;
        idCardPanel.transform.localPosition = new Vector3(0, -50f, 0);
        
        if (idCardCanvasGroup != null) idCardCanvasGroup.DOFade(1, 0.5f);
        idCardPanel.transform.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(3f);

        if (idCardCanvasGroup != null) 
        {
            idCardCanvasGroup.DOFade(0, 0.5f).OnComplete(FinishOnboarding);
        }
        else 
        {
            FinishOnboarding();
        }
    }

    private void FinishOnboarding()
    {
        if(idCardPanel != null) idCardPanel.SetActive(false);
        if(registrationPanel != null) registrationPanel.SetActive(false);
        
        ProfileManager profileManager = FindObjectOfType<ProfileManager>();
        if(profileManager != null) profileManager.LoadProfileData();

        MainMenuManager menuManager = FindObjectOfType<MainMenuManager>();
        if(menuManager != null) menuManager.UpdateProfileAvatar();
    }
}