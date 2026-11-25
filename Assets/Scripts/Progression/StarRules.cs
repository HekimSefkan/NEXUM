/// <summary>
/// ÜÇ YILDIZ KURALLARI — tek kaynak (Faz 2 / 2.2).
///
/// <b>Yıldızlar BAĞIMSIZ, sayı değil BİT MASKESİ.</b> İkinci yıldızı
/// kaçırıp üçüncüyü almak mümkün: oyuncu hamle sınırını aşıp yine de
/// hiç yardım almamış olabilir. Sayı tutulsaydı bu durum "2 yıldız"
/// diye kaydedilir ve hangi ikisi olduğu kaybolurdu; ekranda da yanlış
/// yuva dolardı.
///
/// <b>Hamle sınırları ÖLÇÜMLE kalibre edildi</b> (<c>Tools/sim_stars.py</c>,
/// bölüm başına 2000 tohum, derinlik-2 bot): kazanılan oyunların
/// hamle dağılımının %75'i. Ayrıntı ve tablo: docs/DESIGN_TOKENS.md 13.
/// <b>SINIRLAR KULLANICI ONAYINA TABİDİR.</b>
///
/// Bu sınıf oyun kurallarını BİLMEZ: mod, ipucu bedeli, Joker bedeli
/// hep çağıranın işi. Buraya yalnızca "kaç hamle, yardım alındı mı,
/// yıldız verilebilir mi" gelir.
/// </summary>
public static class StarRules
{
    /// <summary>Bölümü tamamla.</summary>
    public const int Complete = 1 << 0;

    /// <summary>Hamle sınırının altında tamamla.</summary>
    public const int UnderMoveLimit = 1 << 1;

    /// <summary>İpucu kullanmadan VE ÜCRETLİ Joker kullanmadan tamamla.</summary>
    public const int NoHelp = 1 << 2;

    /// <summary>Üçünün tamamı.</summary>
    public const int All = Complete | UnderMoveLimit | NoHelp;

    /// <summary>Yuva sayısı (ekranda çizilen yıldız adedi).</summary>
    public const int Count = 3;

    /// <summary>
    /// Bölüm başına hamle sınırı. ÖLÇÜLDÜ, uydurulmadı: kazanılan
    /// oyunların %75'i bu değerin altında bitiyor (2000 tohum,
    /// derinlik-2 bot). Yuvarlama YAPILMADI — beşin katlarına
    /// yuvarlamak L2'yi 11'den 15'e çıkarıyordu, yani sınır %90'ın
    /// üstüne kayıp ikinci yıldız anlamını yitiriyordu.
    ///
    /// Kalibrasyon YÜKSELTMESİZ zeminde yapıldı: Araştırma Puanı
    /// yükseltmeleri (Faz 2 sonrası) bölümleri kolaylaştıracak, ama
    /// sınırlar onlar yokken ölçüldü (docs/ECONOMY_PROPOSAL.md §3).
    /// </summary>
    public static readonly int[] MoveLimits = { 8, 11, 15, 33, 29, 28, 37 };

    /// <summary>
    /// Bölümün hamle sınırı; tabloda yoksa -1 (ikinci yıldız
    /// DEĞERLENDİRİLMEZ — bilinmeyen bir sınıra göre yıldız verilmez).
    /// </summary>
    public static int MoveLimit(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= MoveLimits.Length) return -1;
        return MoveLimits[levelIndex];
    }

    /// <summary>
    /// Bir oynanışın yıldızları.
    ///
    /// <paramref name="starsAllowed"/> false ise HİÇ yıldız verilmez.
    /// Çağıran bunu serbest moddan türetir: serbest modda kaybetme
    /// yok, ipucu bedava ve sınırsız, üstelik bölüm hedefi de yok
    /// (<c>GridManager.CheckWinCondition</c> serbest modda hemen
    /// dönüyor). Yıldız orada bir başarıyı ölçmezdi.
    ///
    /// <b>Geri alma yıldızı bozmaz:</b> hamle sayısı geri almayla
    /// birlikte geri gidiyor (<c>LevelRun.Moves</c> anlık görüntüye
    /// giriyor), yardım bayrakları ise geri ALINMIYOR — ipucunu
    /// gerçekten gördün.
    ///
    /// <b>Katalizörün BEDAVA Joker hakkı üçüncü yıldızı bozmaz:</b>
    /// katalizör bir ödül olarak tasarlandı, cezaya dönmemeli. Çağıran
    /// yalnızca ÜCRETLİ Joker'i bildiriyor.
    /// </summary>
    public static int Evaluate(int levelIndex, int moves, bool hintUsed,
                               bool paidJokerUsed, bool starsAllowed)
    {
        if (!starsAllowed) return 0;

        int stars = Complete;

        int limit = MoveLimit(levelIndex);
        if (limit > 0 && moves <= limit) stars |= UnderMoveLimit;

        if (!hintUsed && !paidJokerUsed) stars |= NoHelp;

        return stars;
    }

    public static bool Has(int mask, int star)
    {
        return (mask & star) != 0;
    }

    /// <summary>Maskedeki yıldız sayısı (ekranda "2 / 3" için).</summary>
    public static int CountOf(int mask)
    {
        int n = 0;
        for (int bit = Complete; bit <= NoHelp; bit <<= 1)
        {
            if ((mask & bit) != 0) n++;
        }
        return n;
    }

    /// <summary>Geçersiz bitleri atar (bozuk kayıt koruması).</summary>
    public static int Sanitize(int mask)
    {
        return mask & All;
    }

    /// <summary>Ekrandaki yuva sırasına karşılık gelen bit (0, 1, 2).</summary>
    public static int StarAt(int slot)
    {
        return 1 << slot;
    }
}
