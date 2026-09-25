using UnityEngine;
using DG.Tweening; 
using UnityEngine.SceneManagement; 

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectionPanel;
    public GameObject encyclopediaPanel;
    public GameObject howToPlayPanel;
    public GameObject profilePanel;
    public GameObject settingsPanel; 

    private GameObject currentPanel; 

    [Header("Profil Butonu Avatarı")]
    public UnityEngine.UI.Image profileButtonAvatarImage; 
    public Sprite[] avatarSprites; 

    void Start()
    {
        levelSelectionPanel.SetActive(false);
        encyclopediaPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        profilePanel.SetActive(false);
        if(settingsPanel != null) settingsPanel.SetActive(false); 

        mainMenuPanel.SetActive(true);
        currentPanel = mainMenuPanel;

        // Oyun başladığında avatarı güncelle
        UpdateProfileAvatar();
    }

    // YENİ EKLENEN: Avatarı anında güncellemek için dışarıdan çağrılabilir fonksiyon
    public void UpdateProfileAvatar()
    {
        if (profileButtonAvatarImage != null && avatarSprites != null && avatarSprites.Length > 0)
        {
            int avatarIndex = PlayerPrefs.GetInt("PlayerAvatarIndex", -1); 
            if (avatarIndex >= 0 && avatarIndex < avatarSprites.Length)
            {
                profileButtonAvatarImage.sprite = avatarSprites[avatarIndex];
            }
        }
    }

    private void SwitchPanel(GameObject targetPanel)
    {
        if (currentPanel == targetPanel) return;

        currentPanel.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
        {
            currentPanel.SetActive(false);
            
            targetPanel.SetActive(true);
            ResetScrollPositions(targetPanel);
            targetPanel.transform.localScale = Vector3.zero;
            targetPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            
            currentPanel = targetPanel;
        });
    }

    // Panel her açıldığında kaydırma en üstten başlasın. İçerik boyutu (ContentSizeFitter / LayoutGroup)
    // hesaplanmadan konum yazılırsa etkisiz kalır; bu yüzden önce düzen yeniden hesaplanır.
    private void ResetScrollPositions(GameObject panel)
    {
        Canvas.ForceUpdateCanvases();

        foreach (UnityEngine.UI.ScrollRect scroll in panel.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true))
        {
            if (scroll.content != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);

            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;   // 1 = en üst
            scroll.horizontalNormalizedPosition = 0f;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void OpenLevelSelection() { SwitchPanel(levelSelectionPanel); }
    public void OpenEncyclopedia() { SwitchPanel(encyclopediaPanel); }
    public void OpenHowToPlay() { SwitchPanel(howToPlayPanel); }
    public void OpenProfile() { SwitchPanel(profilePanel); }
    public void OpenSettingsPanel() { SwitchPanel(settingsPanel); }
    
    public void BackToMainMenu() { SwitchPanel(mainMenuPanel); }

    public void LoadLevel(int levelIndex)
    {
        PlayerPrefs.SetInt("SelectedLevel", levelIndex);
        SceneManager.LoadScene("Game"); 
    }
}