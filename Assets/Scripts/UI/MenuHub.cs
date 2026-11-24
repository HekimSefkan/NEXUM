using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ana menü hub'ını görünürlük bayraklarına ve GERÇEK KAYDA göre kurar (1.7).
///
/// İki iş yapar, üçüncüsünü yapmaz:
///  1. <b>Görünürlük.</b> Her giriş <see cref="HubFeatures"/>'a bakar.
///     Uygulanmamış giriş kapatılır; bütün çocukları kapalı olan satır da
///     kapatılır, böylece düzende boşluk kalmaz ve satırlar yeniden akar.
///  2. <b>Durum satırları.</b> "Bölüm 5 · kaldığın yerden", "5 / 7 açık",
///     "10 / 21 tepkime" — hepsi kayıttan okunur. <b>Sahte değer yok:</b>
///     verisi olmayan alan (rütbe, XP, seri) gösterilmez, uydurulmaz.
///  3. Panel AÇMAZ. Butonların OnClick bağları sahnede duruyor ve
///     DEĞİŞMEDİ; hub yalnızca neyin görüneceğine ve ne yazacağına karar
///     verir. Böylece "her giriş eskisiyle aynı hedefi çağırıyor" sözü
///     kodla değil, sahnedeki bağın kendisiyle korunuyor.
/// </summary>
public class MenuHub : MonoBehaviour
{
    [Header("Profil şeridi")]
    public TextMeshProUGUI playerNameText;
    [Tooltip("Rütbe + XP (Faz 2): HubFeatures.RankAndXp ile yönetilir.")]
    public GameObject rankXpGroup;
    [Tooltip("Gün serisi rozeti (Faz 3): HubFeatures.Streak ile yönetilir.")]
    public GameObject streakBadge;

    [Header("Kahraman kart")]
    public TextMeshProUGUI heroStatusText;
    [Tooltip("3 yıldız (Faz 2): HubFeatures.Stars ile yönetilir.")]
    public GameObject heroStars;

    [Header("İkili kart")]
    public GameObject levelSelectCard;
    public TextMeshProUGUI levelSelectStatusText;
    [Tooltip("Günlük bulmaca (Faz 3): HubFeatures.DailyPuzzle ile yönetilir.")]
    public GameObject dailyPuzzleCard;

    [Header("Mod satırı (Faz 5)")]
    public GameObject modeRow;
    public GameObject boilingPointCard;
    public GameObject endlessLabCard;
    public GameObject weeklyExperimentCard;

    [Header("Bilgi satırı")]
    public GameObject recipeBookCard;
    public TextMeshProUGUI recipeBookStatusText;

    [Header("İkon şeridi")]
    public GameObject mentorButton;
    public Image soundIconImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private void OnEnable()
    {
        Apply();
    }

    /// <summary>Görünürlük + durum satırları. Panel açıldığında çalışır.</summary>
    public void Apply()
    {
        ApplyVisibility();
        ApplyStatus();
        RefreshSoundIcon();
    }

    // ------------------------------------------------------------ görünürlük

    private void ApplyVisibility()
    {
        Set(heroStars, HubFeatures.Stars);
        Set(rankXpGroup, HubFeatures.RankAndXp);
        Set(streakBadge, HubFeatures.Streak);
        Set(dailyPuzzleCard, HubFeatures.DailyPuzzle);
        Set(boilingPointCard, HubFeatures.BoilingPoint);
        Set(endlessLabCard, HubFeatures.EndlessLab);
        Set(weeklyExperimentCard, HubFeatures.WeeklyExperiment);
        Set(mentorButton, HubFeatures.Mentor);

        // Bütün çocukları kapalı olan satır HİÇ yer kaplamaz. Layout grubu
        // kapalı objeyi hesaba katmıyor, ama satırın kendisi açık kalırsa
        // kendi dolgusu ve aralığı boşluk bırakırdı.
        Set(modeRow, HubFeatures.BoilingPoint || HubFeatures.EndlessLab
                     || HubFeatures.WeeklyExperiment);

        // Günlük bulmaca gizliyken Bölüm Seçimi satırın TAMAMINI alır.
        LayoutElement levelElement = levelSelectCard != null
            ? levelSelectCard.GetComponent<LayoutElement>() : null;
        if (levelElement != null)
        {
            levelElement.flexibleWidth = 1f;
            levelElement.minWidth = 0f;
        }
    }

    private static void Set(GameObject go, bool visible)
    {
        if (go != null && go.activeSelf != visible) go.SetActive(visible);
    }

    // --------------------------------------------------------- durum satırları

    private void ApplyStatus()
    {
        SaveData data = SaveService.Data;
        int bolumSayisi = LevelCount();

        if (playerNameText != null)
        {
            string ad = data.playerName;
            playerNameText.text = string.IsNullOrWhiteSpace(ad)
                ? Loc.Get(CodeStrings.ProfileDefaultName)
                : ad.Trim();
        }

        if (heroStatusText != null)
        {
            // "kaldığın yer" = oynanabilir en ileri bölüm.
            int bolum = bolumSayisi > 0
                ? Mathf.Clamp(data.unlockedLevel, 0, bolumSayisi - 1) + 1
                : 1;
            heroStatusText.text = Loc.Format(CodeStrings.HubHeroStatus, bolum);
        }

        if (levelSelectStatusText != null)
        {
            int acik = bolumSayisi > 0 ? Mathf.Clamp(data.maxLevelUnlocked, 1, bolumSayisi) : 0;
            levelSelectStatusText.text = Loc.Format(CodeStrings.HubLevelStatus, acik, bolumSayisi);
        }

        if (recipeBookStatusText != null)
        {
            int kesfedilen = data.discoveredRecipes != null ? data.discoveredRecipes.Count : 0;
            int toplam = RecipeCount();
            recipeBookStatusText.text = Loc.Format(CodeStrings.HubRecipeStatus, kesfedilen, toplam);
        }
    }

    /// <summary>Bölüm sayısı katalogdan (ana menüde LevelManager yok).</summary>
    private static int LevelCount()
    {
        RecipeCatalog catalog = RecipeCatalog.Load();
        return catalog != null && catalog.levels != null ? catalog.levels.Count : 0;
    }

    private static int RecipeCount()
    {
        RecipeCatalog catalog = RecipeCatalog.Load();
        return catalog != null && catalog.entries != null ? catalog.entries.Count : 0;
    }

    // ------------------------------------------------------------------- ses

    /// <summary>
    /// Hızlı sessize alma. <b>TEK KAYNAK:</b> Ayarlar'daki müzik ve efekt
    /// anahtarlarıyla aynı <c>SaveService</c> alanlarını yazar — ikinci bir
    /// "sessiz" kaydı YOK. Ayarlar paneli açıldığında anahtarlar bu değeri
    /// gösterir, tersi de doğru.
    /// </summary>
    public void ToggleMute()
    {
        SaveData data = SaveService.Data;
        bool acik = data.musicOn == 1 || data.sfxOn == 1;
        int yeni = acik ? 0 : 1;

        data.musicOn = yeni;
        data.sfxOn = yeni;
        SaveService.SaveNow();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateMusicState();
            AudioManager.Instance.PlayButtonSound();
        }

        RefreshSoundIcon();
    }

    /// <summary>Simge kayıttaki duruma göre; ayrı bir bayrak tutulmaz.</summary>
    public void RefreshSoundIcon()
    {
        if (soundIconImage == null) return;

        SaveData data = SaveService.Data;
        bool acik = data.musicOn == 1 || data.sfxOn == 1;

        Sprite hedef = acik ? soundOnSprite : soundOffSprite;
        if (hedef != null) soundIconImage.sprite = hedef;
    }
}
