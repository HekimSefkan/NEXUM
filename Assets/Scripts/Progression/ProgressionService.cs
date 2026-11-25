using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// XP ve RÜTBE MUHASEBESİ — XP'nin yazıldığı tek yer (Faz 2 / 2.1).
///
/// ================================================================
/// 1. XP KAZANILAN PUANDAN BESLENİR
/// ================================================================
/// <c>LevelRun.EarnedScore</c> yalnızca ARTAR; geri alma, ipucu ve Joker
/// harcaması ondan düşmez. XP'nin kalan skordan (<c>GameManager.currentScore</c>,
/// <c>SaveData.totalScore</c>) beslenmesi, yardım kullanan oyuncuyu iki kez
/// cezalandırırdı.
///
/// ================================================================
/// 2. GERİ ALINAN HAMLE XP BIRAKMAZ — anlık görüntüyle DEĞİL, YAPIYLA
/// ================================================================
/// Ömür boyu sayaç kuralı (CLAUDE.md) bugüne kadar "anlık görüntü al, geri
/// almada geri yaz" ile uygulanıyordu. XP'de buna GEREK YOK, çünkü XP'nin
/// hiçbir yazıcısı geri alınabilir bir ana bağlı değil:
///
///   | kaynak            | yazıldığı an                  | geri alınabilir mi |
///   |-------------------|-------------------------------|--------------------|
///   | bölüm tamamlama   | kazanma ekranı (bölüm bitti)  | hayır              |
///   | tarif keşfi       | ilk birleşme                  | hayır (bkz. aşağı) |
///   | quiz doğru cevabı | cevap değerlendirildiğinde    | hayır              |
///
/// Bölüm XP'si <c>LevelRun.EarnedScore</c>'dan okunur ve o değer geri
/// almada hamle öncesine döner, yani kazanma anındaki tutar KESİNLEŞMİŞ
/// durumu anlatır. Tarif keşfi <c>discoveredRecipes</c> ile aynı istisnadır
/// (keşfedilen bilgi geri alınamaz). Quiz cevabının geri alınması yok.
///
/// Sonuç: birleştir -> geri al -> birleştir döngüsü XP üretmez. Anlık
/// görüntüye yeni alan eklemek gerekmez; eklenirse de yanlış olurdu
/// (keşif XP'sini geri yazardı). Ölçüm: <c>ProgressionTests</c>.
///
/// ================================================================
/// 3. ÇİFTLİK YOK: BÖLÜM XP'Sİ "EN İYİ SONUÇ" MUHASEBESİDİR
/// ================================================================
/// Bölüm başına kayıtta bir sayı durur: <c>levelXpGranted[i]</c> — o bölümün
/// bugüne kadar ÖDEDİĞİ toplam XP. Bir oynanışın hak ettiği tutar
/// <c>LevelClearXp + earnedScore / ScoreXpDivisor</c>; verilen XP bu tutarın
/// ödenmiş olandan FAZLASI kadardır.
///
///   ilk tamamlama        -> tam XP
///   aynı/daha kötü sonuç -> 0 XP
///   daha iyi sonuç       -> yalnızca fark
///
/// Neden bu kural: "yalnızca ilk tamamlama" demek, tekrar oynamanın XP
/// değerini sıfırlar ve bölümü daha iyi oynamanın karşılığı kalmaz.
/// "Her tamamlamada tam XP" demek, L1'i 6 hamlede bitirip sınırsız XP
/// basmak demek (ölçüm: L1 ortalama 6 hamle). Bu kural ikisinin arasında
/// durur ve TAVANI vardır: bir bölümün ödeyebileceği en büyük tutar, o
/// bölümde ulaşılabilen en yüksek puanın XP'sidir.
///
/// ================================================================
/// 4. QUIZ XP'Sİ KÖTÜYE KULLANILAMAZ
/// ================================================================
/// Yanlış cevaplanan soru havuza girip sonraki quizlerde önceliklenir
/// (<c>QuizSelector</c>). "Bilerek yanlış yap, sonra doğru yap" döngüsü
/// aynı soruyu tekrar tekrar ödetebilirdi. Kural: <b>bir soru ömründe BİR
/// KEZ XP öder</b> (<c>quizXpAwarded</c>). Ayrıca ödenen ödül sayısı
/// (<c>quizXpPaid</c>) banka boyutunu AŞAMAZ — bu ikinci sınır, soru adı
/// kaydedilmemiş eski kayıtların geriye dönük hesabı için gerekli
/// (bkz. <see cref="RunBackfillOnce"/>).
/// </summary>
public static class ProgressionService
{
    private const string Tag = "NEXUM_XP";

    /// <summary>
    /// Kabul edilen en büyük bölüm indeksi + 1. Kayıt katmanının sayı
    /// listesi tavanıyla (<c>SaveService</c> IntListMax) aynı olmalı:
    /// buradan büyük bir indeks, onarımın keseceği bir liste üretirdi.
    /// </summary>
    private const int MaxLevels = 64;

    /// <summary>
    /// XP ödemiş soru adı listesinin tavanı. Kayıt katmanının ad listesi
    /// tavanıyla (NameListMax) aynı; bugünkü banka 36 soru.
    /// </summary>
    private const int MaxQuizAwards = 256;

    // =================================================================== XP

    public static int Xp
    {
        get { return SaveService.Data.xp; }
    }

    public static int RankIndex
    {
        get { return ProgressionTable.RankForXp(Xp); }
    }

    public static string RankNameKey
    {
        get { return ProgressionTable.Ranks[RankIndex].nameKey; }
    }

    /// <summary>Bulunulan rütbenin eşiği.</summary>
    public static int RankThreshold
    {
        get { return ProgressionTable.Ranks[RankIndex].threshold; }
    }

    /// <summary>Bir sonraki rütbenin eşiği; son rütbedeyse -1.</summary>
    public static int NextThreshold
    {
        get { return ProgressionTable.NextThreshold(RankIndex); }
    }

    /// <summary>
    /// Rütbe içindeki ilerleme (0..1). Son rütbede 1 döner: çubuk dolu
    /// kalır, "sonsuza kadar boş" görünmez.
    /// </summary>
    public static float RankProgress01
    {
        get
        {
            int next = NextThreshold;
            if (next < 0) return 1f;

            int taban = RankThreshold;
            int aralik = next - taban;
            if (aralik <= 0) return 1f;
            return Mathf.Clamp01((Xp - taban) / (float)aralik);
        }
    }

    /// <summary>XP ekler. XP'nin yazıldığı TEK yer.</summary>
    private static int Add(int amount)
    {
        if (amount <= 0) return 0;

        SaveService.Data.xp += amount;
        SaveService.MarkDirty();
        return amount;
    }

    // ======================================================== KAYNAK: BÖLÜM

    /// <summary>
    /// Bölüm tamamlandı. Dönen değer VERİLEN XP (0 olabilir: aynı bölüm
    /// daha iyi oynanmadıysa).
    ///
    /// <paramref name="earnedScore"/> <c>LevelRun.EarnedScore</c> olmalıdır,
    /// kalan skor DEĞİL.
    /// </summary>
    public static int AwardLevelComplete(int levelIndex, int earnedScore)
    {
        if (!XpSources.LevelComplete) return 0;

        // Akıl dışı indeks kabul edilmez: defter listesi indeksle büyüyor,
        // yani sınırsız bir indeks sınırsız bellek demekti. Sınır kayıt
        // katmanının liste tavanıyla aynı (SaveService.IntListMax).
        if (levelIndex < 0 || levelIndex >= MaxLevels)
        {
            Debug.LogError($"{Tag}: akil disi bolum indeksi {levelIndex} -> 0 XP");
            return 0;
        }

        int hak = ProgressionTable.LevelClearXp + ScoreXp(earnedScore);
        List<int> granted = EnsureLevelList(levelIndex);

        int odenmis = granted[levelIndex];
        if (hak <= odenmis)
        {
            Debug.Log($"{Tag}: bolum {levelIndex} tekrar bitti, hak={hak} <= odenmis={odenmis} " +
                      $"-> 0 XP (ciftlik korumasi)");
            return 0;
        }

        int verilen = hak - odenmis;
        granted[levelIndex] = hak;
        SaveService.MarkDirty();

        Add(verilen);
        Debug.Log($"{Tag}: bolum {levelIndex} -> kazanilan puan {earnedScore}, hak {hak}, " +
                  $"odenmis {odenmis}, verilen {verilen}, toplam XP {Xp}");
        return verilen;
    }

    /// <summary>Kazanılan puanın XP karşılığı (tam sayı bölmesi).</summary>
    public static int ScoreXp(int earnedScore)
    {
        if (earnedScore <= 0) return 0;
        return earnedScore / ProgressionTable.ScoreXpDivisor;
    }

    /// <summary>Bir bölümün bugüne kadar ödediği XP (ölçüm ve test için).</summary>
    public static int GrantedForLevel(int levelIndex)
    {
        List<int> granted = SaveService.Data.levelXpGranted;
        if (granted == null || levelIndex < 0 || levelIndex >= granted.Count) return 0;
        return granted[levelIndex];
    }

    private static List<int> EnsureLevelList(int levelIndex)
    {
        List<int> granted = SaveService.Data.levelXpGranted;
        if (granted == null)
        {
            granted = new List<int>();
            SaveService.Data.levelXpGranted = granted;
        }

        while (granted.Count <= levelIndex) granted.Add(0);
        return granted;
    }

    // ======================================================== KAYNAK: TARİF

    /// <summary>
    /// Defterde İLK KEZ görülen tarif. Çağıran, keşfin gerçekten yeni
    /// olduğunu doğrulamış olmalı (<c>RecipeBook.Discover</c> true döndü).
    /// </summary>
    public static int AwardRecipeDiscovery(string recipeKey)
    {
        if (!XpSources.RecipeDiscovery) return 0;
        if (string.IsNullOrEmpty(recipeKey)) return 0;

        int verilen = Add(ProgressionTable.RecipeXp);
        if (verilen > 0)
        {
            Debug.Log($"{Tag}: tarif kesfi {recipeKey} -> +{verilen} XP, toplam {Xp}");
        }
        return verilen;
    }

    // ========================================================= KAYNAK: QUIZ

    /// <summary>
    /// Quiz sorusunun DOĞRU cevabı. Bir soru ömründe bir kez öder; ödenen
    /// ödül sayısı <paramref name="bankSize"/>'ı aşamaz.
    /// </summary>
    public static int AwardQuizCorrect(string questionName, int bankSize)
    {
        if (!XpSources.QuizCorrect) return 0;
        if (string.IsNullOrEmpty(questionName)) return 0;

        SaveData d = SaveService.Data;
        List<string> odenen = d.quizXpAwarded;
        if (odenen == null)
        {
            odenen = new List<string>();
            d.quizXpAwarded = odenen;
        }

        // Liste bankanın boyuyla sınırlı kalmalı: banka okunamazsa
        // (bankSize 0) tavan uygulanamıyor, o zaman bu sınır devreye girer.
        if (odenen.Count >= MaxQuizAwards)
        {
            Debug.LogError($"{Tag}: quiz odul listesi tavana dayandi ({odenen.Count}) -> 0 XP");
            return 0;
        }

        if (odenen.Contains(questionName))
        {
            Debug.Log($"{Tag}: {questionName} daha once XP odedi -> 0 XP");
            return 0;
        }

        // İkinci sınır: toplam ödül sayısı banka boyutunu aşmaz. Soru adı
        // kaydedilmemiş eski kayıtlarda geriye dönük hesap bu sayacı
        // doldurur, yani aynı öğrenme iki kez ödenmez.
        if (bankSize > 0 && d.quizXpPaid >= bankSize)
        {
            Debug.Log($"{Tag}: quiz XP tavani ({d.quizXpPaid}/{bankSize}) -> 0 XP");
            return 0;
        }

        odenen.Add(questionName);
        d.quizXpPaid++;
        SaveService.MarkDirty();

        int verilen = Add(ProgressionTable.QuizXp);
        Debug.Log($"{Tag}: quiz {questionName} dogru -> +{verilen} XP " +
                  $"({d.quizXpPaid}/{bankSize}), toplam {Xp}");
        return verilen;
    }

    // ==================================================== KUTLAMA (BİR KEZ)

    /// <summary>
    /// Gösterilmemiş bir rütbe atlaması var mı? Kutlama, oyuncunun bugünkü
    /// rütbesi daha önce kutlanandan yüksekse bekler.
    /// </summary>
    public static bool CelebrationPending
    {
        get { return RankIndex > CelebratedRank; }
    }

    /// <summary>Kutlanmış en yüksek rütbe. Tabloya kırpılır (bozuk veri).</summary>
    public static int CelebratedRank
    {
        get
        {
            return Mathf.Clamp(SaveService.Data.rankCelebrated, 0,
                               ProgressionTable.Ranks.Length - 1);
        }
    }

    /// <summary>Kutlanacak rütbenin ad anahtarı (bekleyen yoksa null).</summary>
    public static string PendingRankNameKey
    {
        get { return CelebrationPending ? RankNameKey : null; }
    }

    /// <summary>
    /// Kutlama gösterildi. Bir daha gösterilmez — her açılışta tekrar
    /// çıkmasını engelleyen tek nokta budur.
    /// </summary>
    public static void MarkCelebrationShown()
    {
        int rank = RankIndex;
        if (SaveService.Data.rankCelebrated == rank) return;

        SaveService.Data.rankCelebrated = rank;
        SaveService.SaveNow();
        Debug.Log($"{Tag}: kutlama gosterildi -> rutbe {rank}");
    }

    // ============================================= GERİYE DÖNÜK HESAP (GÖÇ)

    /// <summary>
    /// Mevcut oyuncu sıfır XP ile karşılaşmasın: KAYDEDİLMİŞ GERÇEK veriden
    /// XP hesaplanır. Bir kez çalışır (<c>xpBackfillDone</c>).
    ///
    /// KAYNAK VERİ VE SINIRLARI — uydurulan hiçbir şey yok:
    ///
    ///  - <b>Tamamlanan bölüm:</b> <c>maxLevelUnlocked - 1</c>. Bu sayaç
    ///    "açılan bölüm sayısı"dır; bir bölüm açılmak için öncekinin
    ///    bitirilmesi gerekir. SON bölümün bitirilmiş olup olmadığı
    ///    kaydedilmiş DEĞİL (bitirmek de, yalnızca açmak da aynı değeri
    ///    bırakıyor), bu yüzden MUHAFAZAKÂR davranılır: son bölüm
    ///    sayılmaz.
    ///  - <b>Puan payı VERİLMEZ.</b> Bölüm başına kazanılan puan hiç
    ///    kaydedilmedi; <c>totalScore</c> KALAN skorların toplamıdır
    ///    (docs/STATS_INVENTORY.md) ve XP kuralı kalan skordan beslenmeyi
    ///    yasaklıyor. Bu yüzden her tamamlanan bölüm yalnızca
    ///    <c>LevelClearXp</c> öder ve <c>levelXpGranted[i]</c> o tutara
    ///    kurulur — oyuncu bölümü tekrar oynarsa puan payını KAZANABİLİR.
    ///  - <b>Tarif:</b> <c>discoveredRecipes.Count</c>. Defterin geriye
    ///    dönük doldurması bu hesaptan ÖNCE koşmalıdır (ProgressionBootstrap
    ///    sırayı kuruyor).
    ///  - <b>Quiz:</b> <c>quizCorrect</c>, banka boyutuna kırpılır. Hangi
    ///    soruların doğru cevaplandığı kaydedilmediği için
    ///    <c>quizXpAwarded</c> BOŞ kalır; <c>quizXpPaid</c> sayacı
    ///    doldurulur, böylece toplam quiz ödülü banka boyutunu aşamaz ve
    ///    aynı öğrenme iki kez ödenmez.
    /// </summary>
    public static int RunBackfillOnce(int levelCount, int recipeCount, int quizBankSize)
    {
        SaveData d = SaveService.Data;
        if (d.xpBackfillDone == 1) return 0;

        int tamamlanan = 0;
        if (levelCount > 0)
        {
            tamamlanan = Mathf.Clamp(d.maxLevelUnlocked - 1, 0, levelCount);
        }

        int bolumXp = 0;
        if (tamamlanan > 0)
        {
            List<int> granted = EnsureLevelList(tamamlanan - 1);
            for (int i = 0; i < tamamlanan; i++)
            {
                if (granted[i] >= ProgressionTable.LevelClearXp) continue;
                bolumXp += ProgressionTable.LevelClearXp - granted[i];
                granted[i] = ProgressionTable.LevelClearXp;
            }
        }

        int kesfedilen = d.discoveredRecipes != null ? d.discoveredRecipes.Count : 0;
        if (recipeCount > 0) kesfedilen = Mathf.Min(kesfedilen, recipeCount);
        int tarifXp = kesfedilen * ProgressionTable.RecipeXp;

        int dogru = Mathf.Max(0, d.quizCorrect);
        if (quizBankSize > 0) dogru = Mathf.Min(dogru, quizBankSize);
        int quizXp = dogru * ProgressionTable.QuizXp;
        d.quizXpPaid = Mathf.Max(d.quizXpPaid, dogru);

        int toplam = bolumXp + tarifXp + quizXp;
        d.xp += toplam;
        d.xpBackfillDone = 1;
        SaveService.SaveNow();

        Debug.Log($"{Tag}: geriye donuk hesap -> tamamlanan bolum {tamamlanan} ({bolumXp} XP), " +
                  $"tarif {kesfedilen} ({tarifXp} XP), quiz {dogru} ({quizXp} XP), " +
                  $"toplam +{toplam} XP, rutbe {RankIndex}");
        return toplam;
    }
}
