using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Oyun Modu Butonları (Image Bileşenleri)")]
    public Image normalModeImg;
    public Image examModeImg;
    public Image freeModeImg;
    
    [Header("Görsel Ayarlar")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f); 
    public Sprite toggleOnSprite;
    public Sprite toggleOffSprite;

    [Header("Toggle Buton Görselleri (Image Bileşenleri)")]
    public Image musicToggleImg;
    public Image sfxToggleImg;
    public Image flashcardsToggleImg;
    public Image mentorHintsToggleImg;

    void Start()
    {
        int currentMode = PlayerPrefs.GetInt("SelectedGameMode", 0);
        UpdateModeUI(currentMode);

        if(!PlayerPrefs.HasKey("MusicOn")) PlayerPrefs.SetInt("MusicOn", 1);
        if(!PlayerPrefs.HasKey("SfxOn")) PlayerPrefs.SetInt("SfxOn", 1);
        if(!PlayerPrefs.HasKey("FlashcardsOn")) PlayerPrefs.SetInt("FlashcardsOn", 1);
        if(!PlayerPrefs.HasKey("MentorHintsOn")) PlayerPrefs.SetInt("MentorHintsOn", 1);

        UpdateAllTogglesUI();
    }

    public void SelectGameMode(int modeIndex)
    {
        PlayerPrefs.SetInt("SelectedGameMode", modeIndex);
        PlayerPrefs.Save();
        UpdateModeUI(modeIndex);
    }

    private void UpdateModeUI(int index)
    {
        normalModeImg.color = (index == 0) ? selectedColor : unselectedColor;
        examModeImg.color = (index == 1) ? selectedColor : unselectedColor;
        freeModeImg.color = (index == 2) ? selectedColor : unselectedColor;
    }

    public void ToggleMusic()
    {
        int state = PlayerPrefs.GetInt("MusicOn") == 1 ? 0 : 1;
        PlayerPrefs.SetInt("MusicOn", state);
        PlayerPrefs.Save();
        UpdateAllTogglesUI();
        
        // YENİ: Düğmeye basıldığı saniye AudioManager'a müziği susturması/açması için haber ver
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateMusicState();
        }
    }

    public void ToggleSfx()
    {
        int state = PlayerPrefs.GetInt("SfxOn") == 1 ? 0 : 1;
        PlayerPrefs.SetInt("SfxOn", state);
        PlayerPrefs.Save();
        UpdateAllTogglesUI();
    }

    public void ToggleFlashcards()
    {
        int state = PlayerPrefs.GetInt("FlashcardsOn") == 1 ? 0 : 1;
        PlayerPrefs.SetInt("FlashcardsOn", state);
        PlayerPrefs.Save();
        UpdateAllTogglesUI();
    }

    public void ToggleMentorHints()
    {
        int state = PlayerPrefs.GetInt("MentorHintsOn") == 1 ? 0 : 1;
        PlayerPrefs.SetInt("MentorHintsOn", state);
        PlayerPrefs.Save();
        UpdateAllTogglesUI();
    }

    private void UpdateAllTogglesUI()
    {
        musicToggleImg.sprite = PlayerPrefs.GetInt("MusicOn") == 1 ? toggleOnSprite : toggleOffSprite;
        sfxToggleImg.sprite = PlayerPrefs.GetInt("SfxOn") == 1 ? toggleOnSprite : toggleOffSprite;
        flashcardsToggleImg.sprite = PlayerPrefs.GetInt("FlashcardsOn") == 1 ? toggleOnSprite : toggleOffSprite;
        mentorHintsToggleImg.sprite = PlayerPrefs.GetInt("MentorHintsOn") == 1 ? toggleOnSprite : toggleOffSprite;
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        Start();
        
        // YENİ: Sıfırlamadan sonra seslerin durumunu da fabrika ayarlarına çek
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateMusicState();
        }
        
        Debug.Log("Laboratuvar verileri sıfırlandı.");
    }
}