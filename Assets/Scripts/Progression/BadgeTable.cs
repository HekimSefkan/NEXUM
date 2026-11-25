using System.Collections.Generic;

/// <summary>
/// ROZET TABLOSU — tek kaynak (Faz 2 / 2.1).
///
/// <b>Kural: verisi olmayan rozet TANIMLI ama KAZANILAMAZ.</b> Gün serisi
/// (Faz 3), ansiklopedi okuma (Faz 4), yıldız (2.2) ve günlük bulmaca
/// (Faz 3) sayaçları kayıtta HENÜZ YOK; bu rozetlerin
/// <see cref="Badge.available"/> alanı false, ve <c>Badges.TryEarn</c>
/// onları kazanmayı REDDEDER (sessizce geçmez, bir kez loglar).
///
/// Neden tabloda duruyorlar: rozet listesi tek kaynak olsun. Veri gelince
/// yalnızca bayrak açılır ve koşul yazılır; UI, kayıt şeması ve testler
/// değişmez. (<see cref="HubFeatures"/> ve <see cref="XpSources"/> ile aynı
/// yaklaşım.)
///
/// <b>Rozet XP vermez.</b> XP kaynakları <see cref="XpSources"/> tablosunda
/// sayılıdır; rozet bir ANIYI işaretler, ikinci bir XP musluğu değil. Rozete
/// XP bağlanırsa "Tüm Tepkimeler" rozeti tarif keşfi XP'sinin üstüne ikinci
/// kez ödeme yapardı.
///
/// <b>Liste ve görseller ONAYLANDI (25 Kasım 2026)</b> — bkz.
/// docs/DESIGN_TOKENS.md 12.4 ve docs/ui-kit/badges.png. Glif dosyaları
/// Tools/ui-kit/badges/ altında; <b>Assets'e 2.3'te alınacak</b>, çünkü
/// 2.1'de rozeti gösteren bir ekran yok. Rozetler yine de BUGÜNDEN
/// kazanılıyor ve kayda yazılıyor: 2.3'teki duvar gerçek geçmişi
/// göstersin diye.
/// </summary>
public static class Badges
{
    private const string Tag = "NEXUM_BADGE";

    public struct Badge
    {
        /// <summary>Kayıtta duran anahtar. DEĞİŞTİRİLEMEZ (eski kayıtlar).</summary>
        public readonly string key;

        /// <summary>Sözlük anahtarı (CodeStrings).</summary>
        public readonly string nameKey;

        /// <summary>Bugün kazanılabilir mi? false ise veri henüz yok.</summary>
        public readonly bool available;

        /// <summary>
        /// Rozet görselinin glif adı. Görsel = kitteki madalya çerçevesi
        /// (<c>badge_frame_normal</c>) + simge ailesinden bu glif.
        /// </summary>
        public readonly string glyph;

        /// <summary>Hangi fazda açılacağı (kapalı rozetlerde).</summary>
        public readonly string phase;

        public Badge(string key, string nameKey, bool available, string glyph, string phase)
        {
            this.key = key;
            this.nameKey = nameKey;
            this.available = available;
            this.glyph = glyph;
            this.phase = phase;
        }
    }

    public const string FirstSynthesis = "first_synthesis";
    public const string AllReactions = "all_reactions";
    public const string FlawlessLevel = "flawless_level";
    public const string Streak7 = "streak_7";
    public const string Streak30 = "streak_30";
    public const string Streak100 = "streak_100";
    public const string Encyclopedia = "encyclopedia";
    public const string ThreeStars = "three_stars";
    public const string DailyPuzzle10 = "daily_puzzle_10";

    /// <summary>Dokuz rozet. Sıra ekranda gösterim sırasıdır.</summary>
    public static readonly Badge[] All =
    {
        new Badge(FirstSynthesis, CodeStrings.BadgeFirstSynthesis, true, "flask", "2.1"),
        new Badge(AllReactions, CodeStrings.BadgeAllReactions, true, "book", "2.1"),
        new Badge(FlawlessLevel, CodeStrings.BadgeFlawlessLevel, true, "shield", "2.1"),
        new Badge(ThreeStars, CodeStrings.BadgeThreeStars, false, "star_full", "2.2"),
        new Badge(Streak7, CodeStrings.BadgeStreak7, false, "flame", "Faz 3"),
        new Badge(Streak30, CodeStrings.BadgeStreak30, false, "flame", "Faz 3"),
        new Badge(Streak100, CodeStrings.BadgeStreak100, false, "flame", "Faz 3"),
        new Badge(DailyPuzzle10, CodeStrings.BadgeDailyPuzzle10, false, "calendar", "Faz 3"),
        new Badge(Encyclopedia, CodeStrings.BadgeEncyclopedia, false, "book", "Faz 4"),
    };

    public static bool TryGet(string key, out Badge badge)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].key == key) { badge = All[i]; return true; }
        }
        badge = default;
        return false;
    }

    public static bool IsAvailable(string key)
    {
        Badge b;
        return TryGet(key, out b) && b.available;
    }

    public static bool IsEarned(string key)
    {
        List<string> list = SaveService.Data.badges;
        return list != null && list.Contains(key);
    }

    public static int EarnedCount
    {
        get { return SaveService.Data.badges != null ? SaveService.Data.badges.Count : 0; }
    }

    /// <summary>Bugün kazanılabilir rozet sayısı (ekranda "3 / 9" için).</summary>
    public static int AvailableCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (All[i].available) n++;
            return n;
        }
    }

    /// <summary>
    /// Rozeti kazanılmış olarak kaydeder. YENİ bir rozetse true döner.
    ///
    /// Tabloda olmayan ya da <c>available = false</c> olan anahtar
    /// REDDEDİLİR ve loglanır: kapalı bir rozet "bir yerden" kazanılırsa
    /// sessizce geçmesin.
    /// </summary>
    public static bool TryEarn(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        Badge badge;
        if (!TryGet(key, out badge))
        {
            UnityEngine.Debug.LogError($"{Tag}: tabloda olmayan rozet reddedildi -> {key}");
            return false;
        }

        if (!badge.available)
        {
            UnityEngine.Debug.LogError($"{Tag}: verisi olmayan rozet reddedildi -> {key} " +
                                       $"({badge.phase})");
            return false;
        }

        List<string> list = SaveService.Data.badges;
        if (list == null)
        {
            list = new List<string>();
            SaveService.Data.badges = list;
        }

        if (list.Contains(key)) return false;

        list.Add(key);
        SaveService.MarkDirty();
        UnityEngine.Debug.Log($"{Tag}: kazanildi -> {key}");
        return true;
    }

    /// <summary>
    /// Bugün kazanılabilir rozetlerin koşullarını değerlendirir; YENİ
    /// kazanılanların anahtarlarını döndürür.
    ///
    /// Koşullar yalnızca KAYITTAKİ KESİNLEŞMİŞ veriden okunur:
    ///  - <b>İlk Sentez:</b> <c>totalSynthesis &gt;= 1</c>. Bu sayaç geri
    ///    alınabiliyor (<c>GridManager.ExecuteUndo</c> geri yazıyor), ama
    ///    rozet geri ALINMAZ: oyuncu o birleşmeyi gerçekten yaptı. Tarif
    ///    keşfiyle aynı istisna (CLAUDE.md "Ömür boyu istatistikler").
    ///  - <b>Tüm Tepkimeler:</b> keşfedilen tarif sayısı tablo boyutuna
    ///    eşit. <paramref name="recipeTotal"/> 0 ise (katalog okunamadı)
    ///    DEĞERLENDİRİLMEZ — bilinmeyen bir sınıra göre rozet verilmez.
    ///  - <b>Kazasız Bölüm:</b> yalnızca bölüm bitişinde, o oynanışta
    ///    laboratuvar kazası olmadıysa.
    /// </summary>
    public static List<string> Evaluate(int recipeTotal, bool flawlessLevelFinished)
    {
        List<string> yeni = new List<string>();
        SaveData d = SaveService.Data;

        if (d.totalSynthesis >= 1 && TryEarn(FirstSynthesis)) yeni.Add(FirstSynthesis);

        int kesfedilen = d.discoveredRecipes != null ? d.discoveredRecipes.Count : 0;
        if (recipeTotal > 0 && kesfedilen >= recipeTotal && TryEarn(AllReactions))
        {
            yeni.Add(AllReactions);
        }

        if (flawlessLevelFinished && TryEarn(FlawlessLevel)) yeni.Add(FlawlessLevel);

        return yeni;
    }
}
