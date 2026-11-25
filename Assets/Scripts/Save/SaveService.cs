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

    // --- Tarif defteri ----------------------------------------------------
    [Tooltip("Keşfedilen tarifler (RecipeCatalog anahtarı: \"Tile_H_Tile_O\")")]
    public List<string> discoveredRecipes = new List<string>();
    public int recipeBackfillDone;   // geriye dönük doldurma bir kez çalışır

    // --- Quiz öğrenme (1.5) ----------------------------------------------
    [Tooltip("Yanlış cevaplanan soruların asset adları (\"Soru_7\"). Sonraki " +
             "quizlerde önceliklidir; doğru cevaplanınca havuzdan çıkar.")]
    public List<string> quizWrongPool = new List<string>();

    [Tooltip("En son sorulan sorunun asset adı; aynı soru üst üste sorulmaz.")]
    public string quizLastAsked = "";

    // --- İlerleme: XP, rütbe, rozet (Faz 2 / 2.1) ------------------------
    [Tooltip("Toplam deneyim puanı. KAZANILAN puandan beslenir, kalan skordan değil.")]
    public int xp;

    [Tooltip("Bölüm başına bugüne kadar ÖDENEN XP (çiftlik koruması). " +
             "İndeks = bölüm indeksi.")]
    public List<int> levelXpGranted = new List<int>();

    [Tooltip("XP ödemiş quiz sorularının asset adları. Bir soru ömründe bir kez öder.")]
    public List<string> quizXpAwarded = new List<string>();

    [Tooltip("Ödenmiş quiz ödülü sayısı. Soru adı kaydedilmemiş eski kayıtların " +
             "geriye dönük hesabı bu sayacı doldurur; toplam ödül banka boyutunu aşamaz.")]
    public int quizXpPaid;

    [Tooltip("Kazanılan rozet anahtarları (Badges tablosu).")]
    public List<string> badges = new List<string>();

    [Tooltip("Kutlaması GÖSTERİLMİŞ en yüksek rütbe indeksi. Kutlama bir kez çıkar.")]
    public int rankCelebrated;

    [Tooltip("XP'nin geriye dönük hesabı bir kez çalışır.")]
    public int xpBackfillDone;

    // --- Yıldızlar (Faz 2 / 2.2) -----------------------------------------
    [Tooltip("Bölüm başına KAZANILMIŞ yıldızların bit maskesi (StarRules). " +
             "İndeks = bölüm indeksi. Birikir: kazanılan yıldız geri alınmaz.")]
    public List<int> levelStars = new List<int>();

    [Tooltip("Bölüm başına EN İYİ hamle sayısı; 0 = hiç bitirilmedi.")]
    public List<int> levelBestMoves = new List<int>();

    [Tooltip("Yıldızların geriye dönük hesabı bir kez çalışır.")]
    public int starBackfillDone;

    // --- İstatistik -------------------------------------------------------
    public int totalScore;
    public int totalSynthesis;
    public int totalAccidents;
    public int quizCorrect;
    public int quizAttempts;

    // --- Ayarlar ----------------------------------------------------------
    public int language = -1;         // -1 = henüz seçilmedi (cihaz dilinden türetilir)
    public int showReactionEquations = 1;   // her birleşmede tepkime denklemi (varsayılan AÇIK)
    public int musicOn = 1;
    public int sfxOn = 1;
    public int mentorHintsOn = 1;
    public int flashcardsOn = 1;

    // ======================================================================
    // İLERİDE EKLENECEK ALANLAR (Faz 2+). Şema hazır olsun diye burada
    // listeleniyor; eklendiklerinde CurrentVersion artırılıp Upgrade'e
    // karşılık gelen adım yazılacak.
    //
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
    public const int CurrentVersion = 5;
    private const string Tag = "NEXUM_SAVE";

    /// <summary>Ad listelerinde kabul edilen en fazla girdi (bozuk veri koruması).</summary>
    private const int NameListMax = 256;

    /// <summary>Sayı listelerinde kabul edilen en fazla girdi (bozuk veri koruması).</summary>
    private const int IntListMax = 64;

    /// <summary>
    /// <c>rankCelebrated</c> için akıl dışı değer sınırı. Gerçek sınır rütbe
    /// tablosunun boyu, ama o tablo başka bir derlemede; burada yalnızca
    /// bozuk veri kırpılır.
    /// </summary>
    private const int RankCelebratedMax = 31;

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

    // --------------------------------------------------------- ham erişim (test)
    // Kalıcı depolama TEK KAPIDAN geçer: PlayerPrefs yalnızca bu dosyada
    // kullanılır (NexumValidation bunu zorunlu kılar). Göç testlerinin eski
    // sürüm kaydı yazması ve testin kendi yedeğini alması gerektiği için ham
    // okuma/yazma da buradan sunulur; oyun kodu bunları ÇAĞIRMAZ.

    // YAYINA GİRMEZ: bu iki üye yalnızca EDİTÖRDE derlenir. Oyuncunun
    // elindeki pakette kayıt dosyasını ham hâliyle okuyup yazan bir kapı
    // BULUNMAZ — testlerin kolaylığı, yayındaki saldırı yüzeyine
    // dönüşmemeli.
    //
    // Neden yalnızca UNITY_EDITOR: UNITY_INCLUDE_TESTS bu projenin PLAYER
    // derlemelerinde de TANIMLI (ölçüldü, NexumTestGates). Onu kapı
    // sembolü saymak koruma görüntüsü verip hiçbir şeyi kapatmazdı.
    // PlayMode testleri editörde koştuğu için UNITY_EDITOR testlere yetiyor.
#if UNITY_EDITOR

    /// <summary>Diskteki ham JSON (yedekleme için). Kayıt yoksa boş dizi.</summary>
    public static string ExportRaw()
    {
        return PlayerPrefs.GetString(SaveKey, "");
    }

    /// <summary>
    /// Ham JSON'u diske yazar ve bellekteki kaydı boşaltır; boş/null verilirse
    /// kayıt anahtarı silinir. Bir sonraki <see cref="Data"/> erişimi göçü
    /// yeniden çalıştırır.
    /// </summary>
    public static void ImportRaw(string json)
    {
        if (string.IsNullOrEmpty(json)) PlayerPrefs.DeleteKey(SaveKey);
        else PlayerPrefs.SetString(SaveKey, json);

        PlayerPrefs.Save();
        Unload();
    }

#endif   // UNITY_EDITOR

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
        if (d.discoveredRecipes == null) { d.discoveredRecipes = new List<string>(); Log("discoveredRecipes"); repaired++; }
        if (d.quizLastAsked == null) { d.quizLastAsked = ""; Log("quizLastAsked"); repaired++; }
        repaired += RepairNameList(ref d.quizWrongPool, "quizWrongPool");
        repaired += RepairNameList(ref d.quizXpAwarded, "quizXpAwarded");
        repaired += RepairNameList(ref d.badges, "badges");
        repaired += RepairIntList(ref d.levelXpGranted, "levelXpGranted");
        repaired += RepairIntList(ref d.levelStars, "levelStars");
        repaired += RepairIntList(ref d.levelBestMoves, "levelBestMoves");

        repaired += ClampMin(ref d.avatarIndex, 0, "avatarIndex");
        repaired += ClampMin(ref d.selectedLevel, 0, "selectedLevel");
        repaired += ClampRange(ref d.gameMode, 0, 2, "gameMode");
        repaired += ClampMin(ref d.unlockedLevel, 0, "unlockedLevel");
        repaired += ClampMin(ref d.maxLevelUnlocked, 0, "maxLevelUnlocked");
        repaired += ClampMin(ref d.xp, 0, "xp");
        repaired += ClampMin(ref d.quizXpPaid, 0, "quizXpPaid");
        // Rütbe tablosu Nexum.Progression içinde ve bu derleme onu GÖRMEZ
        // (kayıt katmanı hiçbir şeye bağlı değil). Burada yalnızca akıl dışı
        // değer kırpılır; gerçek tabloya kırpma ProgressionService.CelebratedRank
        // içinde yapılır.
        repaired += ClampRange(ref d.rankCelebrated, 0, RankCelebratedMax, "rankCelebrated");
        repaired += ClampRange(ref d.xpBackfillDone, 0, 1, "xpBackfillDone");
        repaired += ClampRange(ref d.starBackfillDone, 0, 1, "starBackfillDone");
        repaired += RepairStarMasks(d);
        repaired += ClampMin(ref d.totalScore, 0, "totalScore");
        repaired += ClampMin(ref d.totalSynthesis, 0, "totalSynthesis");
        repaired += ClampMin(ref d.totalAccidents, 0, "totalAccidents");
        repaired += ClampMin(ref d.quizCorrect, 0, "quizCorrect");
        repaired += ClampMin(ref d.quizAttempts, 0, "quizAttempts");
        repaired += ClampRange(ref d.language, -1, 1, "language");
        repaired += ClampRange(ref d.showReactionEquations, 0, 1, "showReactionEquations");
        repaired += ClampRange(ref d.recipeBackfillDone, 0, 1, "recipeBackfillDone");
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

    /// <summary>
    /// Ad listesi onarımı: null, boş girdi, yineleme ve akıl dışı uzunluk.
    /// Havuz bir KÜME gibi davranır (aynı soru iki kez durmaz); üst sınır
    /// yalnızca bozuk veriye karşı korumadır — gerçek sınır banka boyutudur
    /// ve bankada olmayan adlar <c>QuizSelector</c> ilk kullanımda ayıklar.
    /// </summary>
    private static int RepairNameList(ref List<string> list, string field)
    {
        if (list == null)
        {
            list = new List<string>();
            Log(field);
            return 1;
        }

        int before = list.Count;
        HashSet<string> seen = new HashSet<string>();
        List<string> temiz = new List<string>(before);

        foreach (string s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (!seen.Add(s)) continue;
            temiz.Add(s);
            if (temiz.Count >= NameListMax) break;
        }

        if (temiz.Count == before) return 0;

        Log($"{field} ({before} -> {temiz.Count})");
        list = temiz;
        return 1;
    }

    /// <summary>
    /// Sayı listesi onarımı: null, negatif girdi ve akıl dışı uzunluk.
    ///
    /// <c>levelXpGranted</c> indeksle anlam taşıyor (indeks = bölüm), bu
    /// yüzden girdi ATILMAZ — negatif değer 0'a çekilir, liste yalnızca
    /// uzunluk sınırından kesilir. Girdi atmak bütün bölümleri kaydırırdı.
    /// </summary>
    private static int RepairIntList(ref List<int> list, string field)
    {
        if (list == null)
        {
            list = new List<int>();
            Log(field);
            return 1;
        }

        bool degisti = false;

        if (list.Count > IntListMax)
        {
            Log($"{field} ({list.Count} -> {IntListMax})");
            list.RemoveRange(IntListMax, list.Count - IntListMax);
            degisti = true;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] >= 0) continue;
            Log($"{field}[{i}] = {list[i]}");
            list[i] = 0;
            degisti = true;
        }

        return degisti ? 1 : 0;
    }

    /// <summary>
    /// Yıldız maskelerinde tanımsız bit bırakmaz. Maske üç bitlik
    /// (<c>StarRules.All</c> = 7) ama o tablo başka bir derlemede;
    /// burada sayı olarak kırpılır, anlamı <c>StarRules.Sanitize</c>
    /// okuma tarafında bir kez daha uygular.
    /// </summary>
    private static int RepairStarMasks(SaveData d)
    {
        if (d.levelStars == null) return 0;

        const int tumBitler = 7;      // StarRules.All
        int onarilan = 0;

        for (int i = 0; i < d.levelStars.Count; i++)
        {
            int temiz = d.levelStars[i] & tumBitler;
            if (temiz == d.levelStars[i]) continue;

            Log($"levelStars[{i}] = {d.levelStars[i]}");
            d.levelStars[i] = temiz;
            onarilan++;
        }

        return onarilan > 0 ? 1 : 0;
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

        // Sürüm 2: tarif defteri eklendi. Eski kayıtta defter yok; geriye dönük
        // doldurma RecipeBook.RunBackfillOnce ile yapılır (tarif tablosu bu
        // assembly'den görünmediği için burada yalnızca bayrak kurulur).
        if (d.saveVersion < 2)
        {
            if (d.discoveredRecipes == null) d.discoveredRecipes = new List<string>();
            d.recipeBackfillDone = 0;     // ilk fırsatta doldurulsun
            d.saveVersion = 2;
            steps++;
        }

        // Sürüm 3: quiz öğrenme (yanlış cevap havuzu). Eski kayıtta havuz yok;
        // GERİYE DÖNÜK DOLDURMA YAPILMAZ — hangi soruların yanlış cevaplandığı
        // kaydedilmemişti, uydurmak oyuncuya yanlış soru önceliği verirdi.
        // Boş havuz "henüz yanlış yok" ile aynı davranır: düz rastgele seçim.
        if (d.saveVersion < 3)
        {
            if (d.quizWrongPool == null) d.quizWrongPool = new List<string>();
            if (d.quizLastAsked == null) d.quizLastAsked = "";
            d.saveVersion = 3;
            steps++;
        }

        // Sürüm 4: XP, rütbe ve rozet (Faz 2 / 2.1). Eski kayıtta bu alanlar
        // yok. GERİYE DÖNÜK HESAP BURADA YAPILMAZ: XP tablosu ve rütbe
        // eşikleri Nexum.Progression içinde ve kayıt katmanı hiçbir derlemeye
        // bağlı değil (tarif defterinde de aynı desen kullanıldı). Burada
        // yalnızca bayrak kurulur; hesabı ProgressionService.RunBackfillOnce
        // yapar ve ProgressionBootstrap ilk sahne yüklenmeden önce çağırır.
        if (d.saveVersion < 4)
        {
            if (d.levelXpGranted == null) d.levelXpGranted = new List<int>();
            if (d.quizXpAwarded == null) d.quizXpAwarded = new List<string>();
            if (d.badges == null) d.badges = new List<string>();
            d.xpBackfillDone = 0;     // ilk fırsatta hesaplansın
            d.saveVersion = 4;
            steps++;
        }

        // Sürüm 5: üç yıldız (Faz 2 / 2.2). Eski kayıtta yıldız yok.
        // GERİYE DÖNÜK HESAP BURADA YAPILMAZ: yıldız tablosu ve XP
        // miktarı Nexum.Progression içinde, kayıt katmanı hiçbir
        // derlemeye bağlı değil (tarif defteri ve XP ile aynı desen).
        // Hesabı StarService.RunBackfillOnce yapar; ProgressionBootstrap
        // sırayı kurar.
        if (d.saveVersion < 5)
        {
            if (d.levelStars == null) d.levelStars = new List<int>();
            if (d.levelBestMoves == null) d.levelBestMoves = new List<int>();
            d.starBackfillDone = 0;     // ilk fırsatta hesaplansın
            d.saveVersion = 5;
            steps++;
        }

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
