using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// YILDIZ MUHASEBESİ — yıldızların kayda yazıldığı tek yer (Faz 2 / 2.2).
///
/// <b>En iyi sonuç korunur.</b> Bölüm başına iki şey saklanır:
/// kazanılmış yıldızların maskesi (<c>levelStars</c>) ve en iyi hamle
/// sayısı (<c>levelBestMoves</c>). Maske OR ile birikir — bir kez
/// kazanılan yıldız geri alınmaz; hamle sayısı MIN ile iyileşir. Kötü
/// bir oynanış iyi sonucun üstüne yazamaz.
///
/// <b>Çiftlik koruması maskeden BEDAVA geliyor.</b> XP yalnızca YENİ
/// bitler için ödenir (<c>yeni &amp; ~eski</c>), yani aynı ikinci
/// yıldızı tekrar almak 0 XP. Bölüm XP'sindeki "en iyi sonuç defteri"
/// ile aynı desen; ikinci bir sayaç yazılmadı.
/// </summary>
public static class StarService
{
    private const string Tag = "NEXUM_STAR";

    /// <summary>
    /// Kabul edilen en büyük bölüm indeksi + 1. Kayıt katmanının sayı
    /// listesi tavanıyla aynı (<c>SaveService</c> IntListMax): defter
    /// indeksle büyüdüğü için sınırsız indeks sınırsız bellek demekti.
    /// </summary>
    private const int MaxLevels = 64;

    // ------------------------------------------------------------- okuma

    /// <summary>Bölümün bugüne kadar kazanılmış yıldızları (bit maskesi).</summary>
    public static int StarsFor(int levelIndex)
    {
        List<int> list = SaveService.Data.levelStars;
        if (list == null || levelIndex < 0 || levelIndex >= list.Count) return 0;
        return StarRules.Sanitize(list[levelIndex]);
    }

    /// <summary>Bölümün en iyi hamle sayısı; hiç bitirilmediyse 0.</summary>
    public static int BestMoves(int levelIndex)
    {
        List<int> list = SaveService.Data.levelBestMoves;
        if (list == null || levelIndex < 0 || levelIndex >= list.Count) return 0;
        return Mathf.Max(0, list[levelIndex]);
    }

    /// <summary>Toplam kazanılmış yıldız (profil ve rozet için).</summary>
    public static int TotalStars
    {
        get
        {
            List<int> list = SaveService.Data.levelStars;
            if (list == null) return 0;

            int n = 0;
            foreach (int mask in list) n += StarRules.CountOf(StarRules.Sanitize(mask));
            return n;
        }
    }

    /// <summary>Üç yıldızı tamamlanmış bölüm var mı? ("Üç Yıldızlı Bölüm" rozeti)</summary>
    public static bool HasAnyThreeStarLevel
    {
        get
        {
            List<int> list = SaveService.Data.levelStars;
            if (list == null) return false;

            foreach (int mask in list)
            {
                if (StarRules.Sanitize(mask) == StarRules.All) return true;
            }
            return false;
        }
    }

    // ------------------------------------------------------------- yazma

    /// <summary>
    /// Bir oynanışın sonucunu kaydeder ve YENİ yıldızların XP'sini öder.
    /// Dönen değer verilen XP (0 olabilir: yeni yıldız yoksa).
    ///
    /// <paramref name="stars"/> <c>StarRules.Evaluate</c>'ten gelmeli;
    /// bu metot kuralı yeniden hesaplamaz (tek kaynak).
    /// </summary>
    public static int Record(int levelIndex, int stars, int moves)
    {
        if (levelIndex < 0 || levelIndex >= MaxLevels)
        {
            Debug.LogError($"{Tag}: akil disi bolum indeksi {levelIndex} -> kayit yok");
            return 0;
        }

        stars = StarRules.Sanitize(stars);
        if (stars == 0) return 0;          // serbest mod: yıldız yok, kayıt da yok

        SaveData d = SaveService.Data;
        List<int> maskeler = EnsureList(ref d.levelStars, levelIndex);
        List<int> hamleler = EnsureList(ref d.levelBestMoves, levelIndex);

        int eski = StarRules.Sanitize(maskeler[levelIndex]);
        int yeni = stars & ~eski;

        maskeler[levelIndex] = eski | stars;

        // En iyi hamle: 0 "hiç bitirilmedi" demek, o yüzden ilk kayıt
        // doğrudan yazılır.
        int enIyi = hamleler[levelIndex];
        if (moves > 0 && (enIyi <= 0 || moves < enIyi)) hamleler[levelIndex] = moves;

        SaveService.MarkDirty();

        int xp = ProgressionService.AwardStars(StarRules.CountOf(yeni));

        Debug.Log($"{Tag}: bolum {levelIndex} -> yildiz {Yaz(stars)}, yeni {Yaz(yeni)}, " +
                  $"birikmis {Yaz(maskeler[levelIndex])}, hamle {moves} " +
                  $"(en iyi {hamleler[levelIndex]}), +{xp} XP");
        return xp;
    }

    private static List<int> EnsureList(ref List<int> list, int levelIndex)
    {
        if (list == null) list = new List<int>();
        while (list.Count <= levelIndex) list.Add(0);
        return list;
    }

    private static string Yaz(int mask)
    {
        return (StarRules.Has(mask, StarRules.Complete) ? "1" : "-")
             + (StarRules.Has(mask, StarRules.UnderMoveLimit) ? "2" : "-")
             + (StarRules.Has(mask, StarRules.NoHelp) ? "3" : "-");
    }

    // ============================================= GERİYE DÖNÜK HESAP (GÖÇ)

    /// <summary>
    /// Mevcut oyuncunun TAMAMLADIĞI bölümlere birinci yıldız verilir —
    /// ve YALNIZCA birinci.
    ///
    /// Gerekçe: "bölümü tamamla" kaydedilmiş gerçek veridir
    /// (<c>maxLevelUnlocked</c>). İkinci yıldız hamle sayısı ister,
    /// üçüncü yıldız ipucu/Joker geçmişi ister; ikisi de 2.2 öncesinde
    /// HİÇ kaydedilmedi. Uydurulmaz.
    ///
    /// Son bölüm MUHAFAZAKÂR sayılmaz: bir bölümü bitirmek de yalnızca
    /// açmak da aynı değeri bırakıyor (XP'nin geriye dönük hesabıyla
    /// aynı kural).
    ///
    /// Bir kez çalışır (<c>starBackfillDone</c>). Verilen birinci
    /// yıldızların XP'si de burada ödenir; rütbe atlarsa kutlama
    /// <c>ProgressionService</c>'in bayrağıyla bir kez çıkar.
    /// </summary>
    public static int RunBackfillOnce(int levelCount)
    {
        SaveData d = SaveService.Data;
        if (d.starBackfillDone == 1) return 0;

        int tamamlanan = 0;
        if (levelCount > 0) tamamlanan = Mathf.Clamp(d.maxLevelUnlocked - 1, 0, levelCount);

        int verilenYildiz = 0;
        if (tamamlanan > 0)
        {
            List<int> maskeler = EnsureList(ref d.levelStars, tamamlanan - 1);
            EnsureList(ref d.levelBestMoves, tamamlanan - 1);

            for (int i = 0; i < tamamlanan; i++)
            {
                if (StarRules.Has(maskeler[i], StarRules.Complete)) continue;
                maskeler[i] |= StarRules.Complete;
                verilenYildiz++;
            }
        }

        int xp = ProgressionService.AwardStars(verilenYildiz);
        d.starBackfillDone = 1;
        SaveService.SaveNow();

        Debug.Log($"{Tag}: geriye donuk hesap -> tamamlanan bolum {tamamlanan}, " +
                  $"verilen birinci yildiz {verilenYildiz}, +{xp} XP, " +
                  $"toplam yildiz {TotalStars}");
        return xp;
    }
}
