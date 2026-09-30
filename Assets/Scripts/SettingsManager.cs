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
        int currentMode = SaveService.Data.gameMode;
        UpdateModeUI(currentMode);

        // Varsayilanlar SaveData alan tanimlarinda (hepsi 1); ayrica kurmaya gerek yok.

        UpdateAllTogglesUI();
    }

    public void SelectGameMode(int modeIndex)
    {
        SaveService.Data.gameMode = modeIndex;
        SaveService.SaveNow();
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
        int state = SaveService.Data.musicOn == 1 ? 0 : 1;
        SaveService.Data.musicOn = state;
        SaveService.SaveNow();
        UpdateAllTogglesUI();
        
        // YENİ: Düğmeye basıldığı saniye AudioManager'a müziği susturması/açması için haber ver
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateMusicState();
        }
    }

    public void ToggleSfx()
    {
        int state = SaveService.Data.sfxOn == 1 ? 0 : 1;
        SaveService.Data.sfxOn = state;
        SaveService.SaveNow();
        UpdateAllTogglesUI();
    }

    public void ToggleFlashcards()
    {
        int state = SaveService.Data.flashcardsOn == 1 ? 0 : 1;
        SaveService.Data.flashcardsOn = state;
        SaveService.SaveNow();
        UpdateAllTogglesUI();
    }

    public void ToggleMentorHints()
    {
        int state = SaveService.Data.mentorHintsOn == 1 ? 0 : 1;
        SaveService.Data.mentorHintsOn = state;
        SaveService.SaveNow();
        UpdateAllTogglesUI();
    }

    private void UpdateAllTogglesUI()
    {
        musicToggleImg.sprite = SaveService.Data.musicOn == 1 ? toggleOnSprite : toggleOffSprite;
        sfxToggleImg.sprite = SaveService.Data.sfxOn == 1 ? toggleOnSprite : toggleOffSprite;
        flashcardsToggleImg.sprite = SaveService.Data.flashcardsOn == 1 ? toggleOnSprite : toggleOffSprite;
        mentorHintsToggleImg.sprite = SaveService.Data.mentorHintsOn == 1 ? toggleOnSprite : toggleOffSprite;
    }

    public void ResetProgress()
    {
        SaveService.ResetAll();
        Start();
        
        // YENİ: Sıfırlamadan sonra seslerin durumunu da fabrika ayarlarına çek
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateMusicState();
        }
        
        Debug.Log("Laboratuvar verileri sıfırlandı.");
    }
}