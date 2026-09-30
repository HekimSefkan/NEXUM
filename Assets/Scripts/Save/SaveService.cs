using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyunun TÜM kalıcı verisi. Şemalı ve sürümlü tek kayıt.
///
/// Yeni alan eklerken: alanı buraya ekle, varsayılanını yaz, gerekiyorsa
/// <see cref="SaveService.CurrentVersion"/> değerini artırıp göç adımını
/// <see cref="SaveService.Upgrade"/> içine koy.
/// </summary>
[Serializable]
public class SaveData
{
    public int saveVersion;

    // --- Kimlik -----------------------------------------------------------
    public string playerName = "";
    public string playerEmail = "";
    public string nexumId = "";
    public int avatarIndex;

    // --- Oturum seçimi ----------------------------------------------------
    public int selectedLevel;
    public int gameMode;              // 0 Normal, 1 Sınav, 2 Serbest

    // --- İlerleme ---------------------------------------------------------
    public int unlockedLevel;
    public int maxLevelUnlocked;
    public List<int> tutorialReadLevels = new List<int>();   // deney föyü okunmuş bölümler

    // --- İstatistik -------------------------------------------------------
    public int totalScore;
    public int totalSynthesis;
    public int totalAccidents;
    public int quizCorrect;
    public int quizAttempts;

    // --- Ayarlar ----------------------------------------------------------
    public int musicOn = 1;
    public int sfxOn = 1;
    public int mentorHintsOn = 1;
    public int flashcardsOn = 1;

    // ======================================================================
    // İLERİDE EKLENECEK ALANLAR (Faz 2+). Şema hazır olsun diye burada
    // listeleniyor; eklendiklerinde CurrentVersion artırılıp Upgrade'e
    // karşılık gelen adım yazılacak.
    //
    // public int xp;                              // deneyim puanı
    // public int rankIndex;                       // rütbe (XP'den türetilebilir)
    // public List<string> badges;                 // kazanılan rozetler
    // public int streakDays;                      // üst üste oynanan gün sayısı
    // public string lastPlayedDate;               // seri hesabı için (ISO 8601)
    // public List<string> discoveredRecipes;      // keşfedilen tarifler
    // public List<string> collection;             // koleksiyondaki bileşikler
    // public int researchPoints;                  // araştırma puanı ekonomisi
    // public List<string> encyclopediaRead;       // okunmuş ansiklopedi kartları
    // public List<string> dailyPuzzleHistory;     // günlük bulmaca geçmişi
    // ======================================================================
}

/// <summary>
/// Kalıcı kaydın tek sahibi. Projede <c>PlayerPrefs</c> yalnızca burada kullanılır
/// (kural validator tarafından denetlenir).
///
/// - Veri tek bir JSON anahtarında (<see cref="SaveKey"/>) tutulur.
/// - Her değişiklikte diske yazılmaz; <see cref="MarkDirty"/> ile işaretlenir,
///   <see cref="Flush"/> uygulama duraklayınca / kapanınca / sahne değişince yazar.
/// - Veri bozuksa uygulama çökmez: varsayılana dönülür ve hangi alanın
///   onarıldığı loglanır.
/// </summary>
public static class SaveService
{
    public const string SaveKey = "NEXUM_SAVE";
    public const int CurrentVersion = 1;
    private const string Tag = "NEXUM_SAVE";

    private static SaveData data;
    private static bool dirty;

    /// <summary>Kayıt verisi. İlk erişimde diskten yüklenir (gerekirse göç eder).</summary>
    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    public static bool IsDirty { get { return dirty; } }

    /// <summary>Veri değişti; bir sonraki <see cref="Flush"/> diske yazacak.</summary>
    public static void MarkDirty()
    {
        dirty = true;
    }

    /// <summary>Kirliyse diske yazar. Duraklama, çıkış ve sahne geçişinde çağrılır.</summary>
    public static void Flush()
    {
        if (!dirty || data == null) return;

        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            dirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"{Tag}: kayıt yazılamadı: {e.Message}");
        }
    }

    /// <summary>Değiştir + hemen yaz (kritik anlar için).</summary>
    public static void SaveNow()
    {
        MarkDirty();
        Flush();
    }

    // ---------------------------------------------------------------- yükleme

    public static void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");

        if (string.IsNullOrEmpty(json))
        {
            // Yeni şema yok: eski anahtarlardan göç et (yoksa temiz başlangıç)
            data = MigrateFromLegacy();
            data.saveVersion = CurrentVersion;
            SaveNow();
            return;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"{Tag}: kayıt çözümlenemedi ({e.Message}); varsayılana dönülüyor");
            data = null;
        }

        if (data == null)
        {
            Debug.LogError($"{Tag}: kayıt boş çözümlendi; varsayılana dönülüyor");
            data = new SaveData { saveVersion = CurrentVersion };
            SaveNow();
            return;
        }

        int repaired = Repair(data);
        int upgraded = Upgrade(data);

        if (repaired > 0 || upgraded > 0) SaveNow();
    }

    /// <summary>Test ve "ilerlemeyi sıfırla" için: bellekteki kaydı unut.</summary>
    public static void Unload()
    {
        data = null;
        dirty = false;
    }

    /// <summary>Tüm ilerlemeyi siler (Ayarlar → İlerlemeyi Sıfırla).</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        data = new SaveData { saveVersion = CurrentVersion };
        SaveNow();
    }

    // ------------------------------------------------------------ dayanıklılık

    /// <summary>Bozuk/eksik alanları varsayılana çeker; onarılan alan sayısını döndürür.</summary>
    private static int Repair(SaveData d)
    {
        int repaired = 0;

        if (d.playerName == null) { d.playerName = ""; Log("playerName"); repaired++; }
        if (d.playerEmail == null) { d.playerEmail = ""; Log("playerEmail"); repaired++; }
        if (d.nexumId == null) { d.nexumId = ""; Log("nexumId"); repaired++; }
        if (d.tutorialReadLevels == null) { d.tutorialReadLevels = new List<int>(); Log("tutorialReadLevels"); repaired++; }

        repaired += ClampMin(ref d.avatarIndex, 0, "avatarIndex");
        repaired += ClampMin(ref d.selectedLevel, 0, "selectedLevel");
        repaired += ClampRange(ref d.gameMode, 0, 2, "gameMode");
        repaired += ClampMin(ref d.unlockedLevel, 0, "unlockedLevel");
        repaired += ClampMin(ref d.maxLevelUnlocked, 0, "maxLevelUnlocked");
        repaired += ClampMin(ref d.totalScore, 0, "totalScore");
        repaired += ClampMin(ref d.totalSynthesis, 0, "totalSynthesis");
        repaired += ClampMin(ref d.totalAccidents, 0, "totalAccidents");
        repaired += ClampMin(ref d.quizCorrect, 0, "quizCorrect");
        repaired += ClampMin(ref d.quizAttempts, 0, "quizAttempts");
        repaired += ClampRange(ref d.musicOn, 0, 1, "musicOn");
        repaired += ClampRange(ref d.sfxOn, 0, 1, "sfxOn");
        repaired += ClampRange(ref d.mentorHintsOn, 0, 1, "mentorHintsOn");
        repaired += ClampRange(ref d.flashcardsOn, 0, 1, "flashcardsOn");

        if (d.quizCorrect > d.quizAttempts)
        {
            Log($"quizCorrect ({d.quizCorrect}) > quizAttempts ({d.quizAttempts})");
            d.quizAttempts = d.quizCorrect;
            repaired++;
        }

        return repaired;
    }

    private static int ClampMin(ref int value, int min, string field)
    {
        if (value >= min) return 0;
        Log($"{field} = {value}");
        value = min;
        return 1;
    }

    private static int ClampRange(ref int value, int min, int max, string field)
    {
        if (value >= min && value <= max) return 0;
        Log($"{field} = {value}");
        value = Mathf.Clamp(value, min, max);
        return 1;
    }

    private static void Log(string field)
    {
        Debug.LogWarning($"{Tag}: bozuk alan onarıldı -> {field}");
    }

    // -------------------------------------------------------------------- göç

    /// <summary>Şema sürümü eskiyse yükseltir; yapılan adım sayısını döndürür.</summary>
    private static int Upgrade(SaveData d)
    {
        int steps = 0;

        // Sürüm 0 (damgasız): şema sürümü eklenmeden önce yazılmış kayıt
        if (d.saveVersion < 1)
        {
            d.saveVersion = 1;
            steps++;
        }

        // Yeni sürüm geldiğinde buraya bir adım eklenecek:
        // if (d.saveVersion < 2) { ...; d.saveVersion = 2; steps++; }

        return steps;
    }

    // Eski (şemasız) PlayerPrefs anahtarları. Bir sürüm boyunca SİLİNMEZ:
    // oyuncu eski sürüme dönerse verisi yerinde kalsın.
    private const string LegacyPlayerName = "PlayerName";
    private const string LegacyPlayerEmail = "PlayerEmail";
    private const string LegacyNexumId = "NexumID";
    private const string LegacyAvatarIndex = "PlayerAvatarIndex";
    private const string LegacySelectedLevel = "SelectedLevel";
    private const string LegacyGameMode = "SelectedGameMode";
    private const string LegacyUnlockedLevel = "UnlockedLevel";
    private const string LegacyMaxLevelUnlocked = "MaxLevelUnlocked";
    private const string LegacyTotalScore = "TotalScore";
    private const string LegacyTotalSynthesis = "TotalSynthesis";
    private const string LegacyTotalAccidents = "TotalAccidents";
    private const string LegacyQuizCorrect = "QuizCorrect";
    private const string LegacyQuizAttempts = "QuizAttempts";
    private const string LegacyMusicOn = "MusicOn";
    private const string LegacySfxOn = "SfxOn";
    private const string LegacyMentorHintsOn = "MentorHintsOn";
    private const string LegacyFlashcardsOn = "FlashcardsOn";
    private const string LegacyTutorialPrefix = "TutorialRead_Level_";
    private const int LegacyTutorialScan = 32;   // bölüm sayısından bol fazlası

    private static SaveData MigrateFromLegacy()
    {
        SaveData d = new SaveData();

        if (!PlayerPrefs.HasKey(LegacyPlayerName) && !PlayerPrefs.HasKey(LegacyTotalSynthesis))
        {
            Debug.Log($"{Tag}: eski kayıt yok, temiz başlangıç");
            return d;
        }

        d.playerName = PlayerPrefs.GetString(LegacyPlayerName, d.playerName);
        d.playerEmail = PlayerPrefs.GetString(LegacyPlayerEmail, d.playerEmail);
        d.nexumId = PlayerPrefs.GetString(LegacyNexumId, d.nexumId);
        d.avatarIndex = PlayerPrefs.GetInt(LegacyAvatarIndex, d.avatarIndex);

        d.selectedLevel = PlayerPrefs.GetInt(LegacySelectedLevel, d.selectedLevel);
        d.gameMode = PlayerPrefs.GetInt(LegacyGameMode, d.gameMode);

        d.unlockedLevel = PlayerPrefs.GetInt(LegacyUnlockedLevel, d.unlockedLevel);
        d.maxLevelUnlocked = PlayerPrefs.GetInt(LegacyMaxLevelUnlocked, d.maxLevelUnlocked);

        d.totalScore = PlayerPrefs.GetInt(LegacyTotalScore, d.totalScore);
        d.totalSynthesis = PlayerPrefs.GetInt(LegacyTotalSynthesis, d.totalSynthesis);
        d.totalAccidents = PlayerPrefs.GetInt(LegacyTotalAccidents, d.totalAccidents);
        d.quizCorrect = PlayerPrefs.GetInt(LegacyQuizCorrect, d.quizCorrect);
        d.quizAttempts = PlayerPrefs.GetInt(LegacyQuizAttempts, d.quizAttempts);

        d.musicOn = PlayerPrefs.GetInt(LegacyMusicOn, d.musicOn);
        d.sfxOn = PlayerPrefs.GetInt(LegacySfxOn, d.sfxOn);
        d.mentorHintsOn = PlayerPrefs.GetInt(LegacyMentorHintsOn, d.mentorHintsOn);
        d.flashcardsOn = PlayerPrefs.GetInt(LegacyFlashcardsOn, d.flashcardsOn);

        for (int i = 0; i < LegacyTutorialScan; i++)
        {
            if (PlayerPrefs.GetInt(LegacyTutorialPrefix + i, 0) == 1) d.tutorialReadLevels.Add(i);
        }

        Repair(d);
        Debug.Log($"{Tag}: eski kayıt göç ettirildi (oyuncu=\"{d.playerName}\", " +
                  $"sentez={d.totalSynthesis}, açılan bölüm={d.unlockedLevel}, " +
                  $"okunmuş föy={d.tutorialReadLevels.Count})");
        return d;
    }

    // --------------------------------------------------------------- kolaylık

    public static bool IsTutorialRead(int levelIndex)
    {
        return Data.tutorialReadLevels.Contains(levelIndex);
    }

    public static void SetTutorialRead(int levelIndex)
    {
        if (Data.tutorialReadLevels.Contains(levelIndex)) return;
        Data.tutorialReadLevels.Add(levelIndex);
        MarkDirty();
    }

    /// <summary>Bölüm kazanılınca o bölümün skoru toplama eklenir.</summary>
    public static void AddLevelScore(int score)
    {
        if (score <= 0) return;
        Data.totalScore += score;
        MarkDirty();
    }

    public static bool HasProfile
    {
        get { return !string.IsNullOrEmpty(Data.playerName); }
    }
}
