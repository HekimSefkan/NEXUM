using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelMenuManager : MonoBehaviour
{
    [Header("Bölüm Butonları")]
    public Button[] levelButtons;

    [Header("Görsel Tasarımlar (Kapsüller)")]
    public Sprite unlockedSprite; // Mavi cam kapsül (level_selection_button)
    public Sprite lockedSprite;   // Gri metal kapsül (level_selection_kilitlibutton)

    [Header("Renk Paleti")]
    public Color completedColor = new Color(0.18f, 0.8f, 0.44f); // Yeşil (Tamamlanmış) #2ECC71
    public Color activeColor = new Color(0f, 0.7f, 0.84f);       // Camgöbeği (Aktif) #00B4D8
    public Color lockedColor = Color.white;                      // Kilitli (Görsel zaten gri olduğu için dokunmuyoruz)

    void Start()
    {
        UpdateLevelButtons();
    }

    // Bu fonksiyon menü her açıldığında cihazdaki kaydı okuyup butonları görsel olarak tasarlar
    public void UpdateLevelButtons()
    {
        // Cihazdan açık olan en yüksek bölümün indeksini çek
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 0);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Image btnImage = levelButtons[i].GetComponent<Image>();

            if (i < unlockedLevel)
            {
                // 1. DURUM: TAMAMLANMIŞ SEVİYELER (GEÇMİŞ)
                levelButtons[i].interactable = true;
                btnImage.sprite = unlockedSprite; // Cam kapsül
                btnImage.color = completedColor;  // Yeşil renk
            }
            else if (i == unlockedLevel)
            {
                // 2. DURUM: ŞU AN OYNANMASI GEREKEN AKTİF SEVİYE (ŞİMDİ)
                levelButtons[i].interactable = true;
                btnImage.sprite = unlockedSprite; // Cam kapsül
                btnImage.color = activeColor;     // Parlak mavi
            }
            else
            {
                // 3. DURUM: KİLİTLİ SEVİYELER (GELECEK)
                levelButtons[i].interactable = false;
                btnImage.sprite = lockedSprite;   // Gri metal kilitli kapsül
                btnImage.color = lockedColor;     // Rengi bozmamak için normal bırakıyoruz
            }
        }
    }

    public void SelectLevelAndPlay(int levelIndex)
    {
        // Tıklanan bölümdeki kilit açılmamışsa ekstra güvenlik önlemi
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 0);
        if (levelIndex > unlockedLevel) 
        {
            // İstersen buraya AudioManager ile "Hata/Kilitli" sesi ekleyebilirsin
            Debug.Log("Bu seviye henüz kilitli!");
            return;
        }

        PlayerPrefs.SetInt("SelectedLevel", levelIndex);
        SceneManager.LoadScene("Game");
    }
}