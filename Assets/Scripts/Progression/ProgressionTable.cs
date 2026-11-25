/// <summary>
/// XP ve RÜTBE TABLOSU — tek kaynak (Faz 2 / 2.1).
///
/// Buradaki sayılar UYDURULMADI: <c>Tools/sim_xp.py</c> bu dosyadan okuyup
/// ölçümü yapıyor, yani belgedeki tahmini ulaşma süreleri ile koddaki
/// değerler AYRIŞAMAZ. Bir sabiti değiştirmek, aracı tekrar koşturup
/// <c>docs/DESIGN_TOKENS.md</c> 11.2 tablosunu güncellemek demektir
/// (<c>RankTableTests</c> tutarlılığı ölçer).
///
/// <b>XP'nin beslendiği değer KAZANILAN puandır</b> (<c>LevelRun.EarnedScore</c>),
/// kalan skor değil. Kalan skor geri alma, ipucu ve Joker harcamasından
/// sonra kalan tutardır; ondan XP vermek yardım kullanan oyuncuyu iki kez
/// cezalandırırdı (Faz 1'den devir kuralı, docs/STATUS.md).
/// </summary>
public static class ProgressionTable
{
    // ===================================================== XP MİKTARLARI

    /// <summary>Bir bölümün İLK tamamlanmasında verilen sabit XP.</summary>
    public const int LevelClearXp = 60;

    /// <summary>
    /// Kazanılan puandan XP'ye çevirme bölücüsü: <c>xp = earnedScore / 5</c>
    /// (tam sayı bölmesi). Ölçüm (Tools/sim_xp.py, 80 oyun, derinlik-2):
    /// kazanan oyunda bölüm başına kazanılan puan 57..430, yani bu bölücüyle
    /// bölüm başına 11..86 XP. Puan payı bilerek küçük tutuldu — tekrar
    /// oynamanın değeri "daha iyi oyna" olmalı, "aynı bölümü tekrar bitir"
    /// değil.
    /// </summary>
    public const int ScoreXpDivisor = 5;

    /// <summary>Defterde İLK KEZ görülen tarif. 21 tarif x 30 = 630 XP.</summary>
    public const int RecipeXp = 30;

    /// <summary>Bir sorunun İLK doğru cevabı. 36 soru x 12 = 432 XP.</summary>
    public const int QuizXp = 12;

    // --- Kapalı kaynaklar: tablo hazır, bayrak kapalı (XpSources) ---------

    /// <summary>Bölüm yıldızı (2.2). Yıldız başına.</summary>
    public const int StarXp = 20;

    /// <summary>Günlük bulmaca (Faz 3). Gün başına.</summary>
    public const int DailyPuzzleXp = 40;

    /// <summary>Ansiklopedi kartı okuma (Faz 4). Kart başına, bir kez.</summary>
    public const int EncyclopediaXp = 10;

    // ===================================================== RÜTBELER

    public struct Rank
    {
        /// <summary>Sözlük anahtarı (CodeStrings). Metin burada DURMAZ.</summary>
        public readonly string nameKey;

        /// <summary>Bu rütbeye geçiş için gereken toplam XP.</summary>
        public readonly int threshold;

        public Rank(string nameKey, int threshold)
        {
            this.nameKey = nameKey;
            this.threshold = threshold;
        }
    }

    /// <summary>
    /// Yedi rütbe, artan eşikle. ARALIKLAR BİLEREK BÜYÜYOR
    /// (150 / 250 / 400 / 600 / 1000 / 1600, oran ~1,6):
    ///
    ///  - İlk atlama İLK OTURUMDA gelir: L1'i bitirmek 60 + 11 puan XP'si +
    ///    en az iki tarif keşfi (2x30) = 131 XP; L1 + L2 = 232 > 150.
    ///  - Bugünkü içeriğin tavanı ÖLÇÜLDÜ (RankTableTests): 7 bölüm 677
    ///    (420 sabit + 257 puan payı) + 21 tarif 630 + 36 soru 432 =
    ///    1739 XP, yani 5. rütbe (Kimyager). Son iki rütbe Faz 3-5
    ///    içeriği için ayrıldı.
    ///
    /// ADLAR VE EŞİKLER ONAYLANDI (25 Kasım 2026, bkz. docs/DESIGN_TOKENS.md 12.2).
    /// Değiştirilecekse Tools/sim_xp.py yeniden koşturulur ve o tablo
    /// güncellenir; RankTableTests aralıkların büyümesini ve ilk atlamanın
    /// ilk oturumda gelmesini zorunlu kılıyor.
    /// </summary>
    public static readonly Rank[] Ranks =
    {
        new Rank(CodeStrings.Rank0, 0),
        new Rank(CodeStrings.Rank1, 150),
        new Rank(CodeStrings.Rank2, 400),
        new Rank(CodeStrings.Rank3, 800),
        new Rank(CodeStrings.Rank4, 1400),
        new Rank(CodeStrings.Rank5, 2400),
        new Rank(CodeStrings.Rank6, 4000),
    };

    /// <summary>XP'ye karşılık gelen rütbe indeksi (0 = Çırak).</summary>
    public static int RankForXp(int xp)
    {
        int index = 0;
        for (int i = 0; i < Ranks.Length; i++)
        {
            if (xp >= Ranks[i].threshold) index = i;
        }
        return index;
    }

    /// <summary>Bir sonraki rütbenin eşiği; son rütbedeyse -1.</summary>
    public static int NextThreshold(int rankIndex)
    {
        int next = rankIndex + 1;
        return next < Ranks.Length ? Ranks[next].threshold : -1;
    }
}

/// <summary>
/// XP KAYNAKLARININ "UYGULANDI MI" BAYRAKLARI — tek kaynak.
///
/// <see cref="HubFeatures"/> ile aynı yaklaşım: kaynağın miktarı
/// <see cref="ProgressionTable"/>'da TANIMLI, ama bayrağı kapalıysa
/// <c>ProgressionService</c> o kaynaktan XP VERMEZ. Faz 2.2 / 3 / 4 bir
/// kaynağı bitirdiğinde burada tek satır değişir.
///
/// Bayraklar kod sabiti, kayıt verisi değil: "kaynak var mı" sorusunun
/// cevabı oyuncuya göre değişmez.
/// </summary>
public static class XpSources
{
    /// <summary>Bölüm tamamlama (sabit XP + kazanılan puan payı).</summary>
    public static bool LevelComplete { get; set; }

    /// <summary>Defterde ilk kez görülen tarif.</summary>
    public static bool RecipeDiscovery { get; set; }

    /// <summary>Quiz sorusunun ilk doğru cevabı.</summary>
    public static bool QuizCorrect { get; set; }

    /// <summary>Bölüm yıldızları (2.2).</summary>
    public static bool Stars { get; set; }

    /// <summary>Günlük bulmaca (Faz 3).</summary>
    public static bool DailyPuzzle { get; set; }

    /// <summary>Ansiklopedi kartı okuma (Faz 4).</summary>
    public static bool EncyclopediaRead { get; set; }

    static XpSources()
    {
        Varsayilana();
    }

    /// <summary>Bugün yayına giren hâl.</summary>
    public static void Varsayilana()
    {
        LevelComplete = true;       // 2.1
        RecipeDiscovery = true;     // 2.1
        QuizCorrect = true;         // 2.1
        Stars = true;               // 2.2 UYGULANDI
        DailyPuzzle = false;        // Faz 3
        EncyclopediaRead = false;   // Faz 4
    }

    /// <summary>TEST İÇİN: kapalı kaynakların da ölçülebilmesi için hepsini açar.</summary>
    public static void TumunuAc()
    {
        LevelComplete = true;
        RecipeDiscovery = true;
        QuizCorrect = true;
        Stars = true;
        DailyPuzzle = true;
        EncyclopediaRead = true;
    }
}
