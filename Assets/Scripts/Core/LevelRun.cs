using System.Collections.Generic;

/// <summary>
/// BU OYNANIŞIN sayaçları (1.6b): bölüm sonu sonuç kartının okuduğu veri.
///
/// Neden ayrı sınıf: kartın gösterdiği üç değer (kazanılan puan, hamle
/// sayısı, bu bölümde keşfedilen tepkimeler) oyunun hiçbir kuralını
/// değiştirmiyor — yalnızca OLUP BİTENİ sayıyor. GridManager'ın içine
/// serpiştirilseydi, oyun mantığıyla rapor verisi aynı yerde dururdu.
///
/// <b>Kazanılan puan, kalan puan DEĞİLDİR.</b> <c>GameManager.currentScore</c>
/// geri alma ve Joker'e harcananlar düşüldükten sonra kalan tutardır; kartta
/// onu göstermek yardım kullanan oyuncuyu cezalandırırdı. Buradaki sayaç
/// yalnızca ARTAR (harcama düşmez).
///
/// <b>Geri alma kuralı (CLAUDE.md "Ömür boyu istatistikler"):</b>
/// <see cref="EarnedScore"/> ve <see cref="Moves"/> hamleyi anlattıkları için
/// geri alınır — anlık görüntüye girer, <c>ExecuteUndo</c> geri yazar.
/// <see cref="NewRecipes"/> geri ALINMAZ: keşfedilen bilgi geri alınamaz,
/// oyuncu o tepkimeyi gerçekten gördü (<c>discoveredRecipes</c> ile aynı
/// istisna).
///
/// Ömrü sahne örneği kadardır: <c>GridManager.Start</c> sıfırlar, yani
/// yeniden başlat / sonraki bölüm / menüden giriş temiz sayaçla başlar.
/// </summary>
public static class LevelRun
{
    /// <summary>Bu bölümde KAZANILAN toplam puan (harcananlar düşülmeden).</summary>
    public static int EarnedScore { get; private set; }

    /// <summary>Tahtayı değiştiren hamle sayısı (boşa kaydırma sayılmaz).</summary>
    public static int Moves { get; private set; }

    /// <summary>Bu bölümde İLK KEZ keşfedilen tarif anahtarları, keşif sırasıyla.</summary>
    public static IReadOnlyList<string> NewRecipes { get { return newRecipes; } }

    private static readonly List<string> newRecipes = new List<string>();

    /// <summary>
    /// Bu oynanışta laboratuvar kazası (oyun sonu ekranı) oldu mu?
    /// "Kazasız Bölüm" rozetinin tek ölçütü. GERİ ALINMAZ: kaza olduysa
    /// olmuştur, geri alma tahtayı döndürür ama oyun sonu ekranını
    /// görülmemiş yapmaz.
    /// </summary>
    public static bool AccidentHappened { get; private set; }

    /// <summary>
    /// Bu bölüm bitişinde VERİLEN XP. Sonuç kartındaki "+N XP" satırı bunu
    /// okur; kart kendisi hesap YAPMAZ (tek kaynak ProgressionService).
    /// 0 olabilir: aynı bölüm daha iyi oynanmadıysa XP verilmez.
    /// </summary>
    public static int XpAwarded { get; private set; }

    /// <summary>Bölüm başlangıcı: sayaçlar sıfırlanır.</summary>
    public static void Reset()
    {
        EarnedScore = 0;
        Moves = 0;
        newRecipes.Clear();
        AccidentHappened = false;
        XpAwarded = 0;
    }

    /// <summary>Oyun sonu ekranı açıldı (kaza sayıldı).</summary>
    public static void NoteAccident()
    {
        AccidentHappened = true;
    }

    /// <summary>Bölüm bitişinde verilen XP kaydedilir (yalnızca rapor için).</summary>
    public static void NoteXp(int amount)
    {
        XpAwarded = amount;
    }

    /// <summary>Sentezden puan kazanıldı. Harcama BURAYA GELMEZ.</summary>
    public static void AddScore(int amount)
    {
        if (amount <= 0) return;
        EarnedScore += amount;
    }

    /// <summary>Tahtayı değiştiren bir hamle tamamlandı.</summary>
    public static void CountMove()
    {
        Moves++;
    }

    /// <summary>Bu bölümde ilk kez görülen tarif. Yineleme eklenmez.</summary>
    public static void NoteDiscovery(string recipeKey)
    {
        if (string.IsNullOrEmpty(recipeKey)) return;
        if (newRecipes.Contains(recipeKey)) return;
        newRecipes.Add(recipeKey);
    }

    // ------------------------------------------------------------ geri alma

    /// <summary>Hamle öncesi değerler (yalnızca geri alınabilenler).</summary>
    public struct Snapshot
    {
        public int earnedScore;
        public int moves;
    }

    public static Snapshot Capture()
    {
        return new Snapshot { earnedScore = EarnedScore, moves = Moves };
    }

    public static void Restore(Snapshot snapshot)
    {
        EarnedScore = snapshot.earnedScore;
        Moves = snapshot.moves;
    }
}
