using UnityEngine;
using DG.Tweening; 
using UnityEngine.SceneManagement; 
using System.Collections;

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
            StartCoroutine(ResetScrollPositionsNextFrame(targetPanel));
            targetPanel.transform.localScale = Vector3.zero;
            targetPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            
            currentPanel = targetPanel;
        });
    }

    // Kaydırmayı en üste al. ScrollRect'in viewport/content sınırları panel aktif edildiği karede
    // henüz güncel olmadığı için normalizedPosition ataması yanlış sonuç veriyor (içerik dibe kayıyor);
    // bu yüzden bir kare beklenip içerik doğrudan konumlandırılıyor. ContentSizeFitter ile boyutu
    // sürülen RectTransform'larda ForceRebuildLayoutImmediate kullanılmıyor.
    private IEnumerator ResetScrollPositionsNextFrame(GameObject panel)
    {
        UnityEngine.UI.ScrollRect[] scrolls = panel.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true);
        if (scrolls.Length == 0) yield break;

        // İki kare: ilki düzenin kurulması, ikincisi geç hesaplanan içerik boyutları için
        for (int pass = 0; pass < 2; pass++)
        {
            yield return null;

            foreach (UnityEngine.UI.ScrollRect scroll in scrolls)
            {
                if (scroll == null || scroll.content == null) continue;

                scroll.StopMovement();
                // İçerik pivotu üstte (y=1) olduğu için y=0 her zaman en üst demek
                scroll.content.anchoredPosition = new Vector2(0f, 0f);
            }
        }
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