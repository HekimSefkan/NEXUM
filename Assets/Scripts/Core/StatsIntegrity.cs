using UnityEngine;

/// <summary>
/// İSTATİSTİK BÜTÜNLÜĞÜ (Faz 1 / 1.4).
///
/// Kayıttaki ilerleme alanları var olmayan bölümleri gösteremez.
/// <c>UIManager.ShowWinScreen</c> kazanılan bölümün indeksine 1 ve 2
/// ekliyordu; son bölüm kazanılınca sonuç bölüm sayısını AŞIYORDU:
/// 7 bölümlük oyunda profil kartı "AÇILAN BÖLÜM 8" yazıyordu.
///
/// Yazma tarafı artık kırpıyor (<c>ShowWinScreen</c>), ama eski kayıtlarda
/// şişmiş değer duruyor. Burası onu açılışta onarır.
///
/// <b>Neden göç (saveVersion) değil.</b> Alanın ANLAMI değişmedi — her zaman
/// "açılan bölüm sayısı" idi, yazan taraf fazladan sayıyordu. Şema aynı
/// kaldığı için sürüm artırılmadı; değer bir SINIRA çekiliyor, sıfırlanmıyor:
/// oyuncunun gerçek ilerlemesi (7 bölümün hepsi açık) korunuyor.
///
/// <b>Bölüm sayısı nereden geliyor.</b> <c>LevelManager</c> yalnızca Game
/// sahnesinde; <c>RecipeCatalog</c> ise Resources altında ve iki sahneden de
/// okunabiliyor. Bölüm listesi oradan alınır (tarif defterinin geriye dönük
/// doldurması da aynı kaynağı kullanıyor).
/// </summary>
public static class StatsIntegrity
{
    private const string Tag = "NEXUM_STATS";

    /// <summary>İlk sahne yüklenmeden önce bir kez; sonucu loglar.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RepairOnLaunch()
    {
        int onarilan = Repair();
        if (onarilan > 0) Debug.Log($"{Tag}: {onarilan} alan onarıldı");
    }

    /// <summary>
    /// Bölüm sayacını bölüm sayısına çeker. Kaç alan değiştiyse onu döner
    /// (0 = kayıt zaten tutarlı). Bölüm sayısı okunamazsa hiçbir şey yapmaz:
    /// bilinmeyen bir sınıra göre veri kırpılmaz.
    /// </summary>
    public static int Repair()
    {
        int bolumSayisi = LevelCount();
        if (bolumSayisi <= 0) return 0;

        int onarilan = 0;
        SaveData d = SaveService.Data;

        if (d.maxLevelUnlocked > bolumSayisi)
        {
            Debug.Log($"{Tag}: maxLevelUnlocked {d.maxLevelUnlocked} -> {bolumSayisi} " +
                      $"(oyunda {bolumSayisi} bölüm var)");
            d.maxLevelUnlocked = bolumSayisi;
            onarilan++;
        }

        if (d.unlockedLevel > bolumSayisi - 1)
        {
            Debug.Log($"{Tag}: unlockedLevel {d.unlockedLevel} -> {bolumSayisi - 1}");
            d.unlockedLevel = bolumSayisi - 1;
            onarilan++;
        }

        if (onarilan > 0) SaveService.MarkDirty();
        return onarilan;
    }

    /// <summary>Oyundaki bölüm sayısı; okunamazsa 0.</summary>
    public static int LevelCount()
    {
        RecipeCatalog katalog = RecipeCatalog.Load();
        if (katalog == null || katalog.levels == null) return 0;
        return katalog.levels.Count;
    }
}
