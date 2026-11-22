using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quiz sorusunu seçer ve cevabı kaydeder (1.5).
///
/// NEDEN AYRI SINIF: seçim kuralı ölçülebilir olmalı. UIManager'ın içinde
/// <c>Random.Range</c> satırıyken ne oranı, ne tekrar yasağı test edilebiliyordu.
///
/// KURAL
/// -----
/// 1. <b>Yanlış cevap havuzu.</b> Yanlış cevaplanan soru
///    <c>SaveData.quizWrongPool</c>'a girer; doğru cevaplanınca ÇIKAR.
///    Havuz bir kümedir: aynı soru iki kez durmaz.
/// 2. <b>Oran tavanı.</b> Havuz boş değilse sorunun havuzdan gelme olasılığı
///    SABİT <see cref="PoolShare"/> (1/3): çekilişlerin üçte biri havuzdan,
///    üçte ikisi havuz DIŞINDAN gelir. İki kümenin ayrık tutulması bilinçli —
///    "havuz turu değilse bankanın tamamı" denirse havuz büyüdükçe toplam pay
///    rastgelelikle birlikte şişer (12/36 havuzda 0,56'ya çıkıyor) ve tavan
///    ölçülemez olur. Bu kuralla pay havuzun büyüklüğünden BAĞIMSIZ.
///    Tek istisna: havuz bankanın tamamını kapsıyorsa havuz dışı küme boştur,
///    çekiliş bankadan yapılır (sorulacak başka soru yok).
/// 3. <b>Üst üste tekrar yok.</b> En son sorulan soru
///    (<c>SaveData.quizLastAsked</c>) bir sonraki çekilişte seçilemez —
///    havuzdan gelse bile. Bankada tek soru varsa kural uygulanamaz;
///    o durumda aynı soru döner.
/// 4. <b>Havuz yalnızca KESİNLEŞMİŞ cevapla değişir.</b> Yazma noktası tek:
///    <see cref="RecordAnswer"/>, ve o da yalnızca cevap değerlendirilince
///    çağrılır. Panelin açılması, kapatılması ya da oyunun kapatılması
///    havuzu değiştirmez (quiz cevabının geri alınması yok).
/// 5. <b>Bozuk ad ayıklama.</b> Bankada karşılığı olmayan havuz girdisi
///    (silinmiş / adı değişmiş soru) ilk seçimde atılır.
/// </summary>
public static class QuizSelector
{
    private const string Tag = "NEXUM_QUIZ";

    /// <summary>Sorunun yanlış cevap havuzundan gelme olasılığı.</summary>
    public const float PoolShare = 1f / 3f;

    // ------------------------------------------------------------------ seçim

    /// <summary>
    /// Bankadan bir soru seçer ve <c>quizLastAsked</c>'ı günceller.
    /// Banka boşsa null döner.
    /// </summary>
    public static ChemistryQuiz Pick(IList<ChemistryQuiz> bank)
    {
        if (bank == null || bank.Count == 0) return null;

        Prune(bank);

        string last = SaveService.Data.quizLastAsked;
        List<string> pool = SaveService.Data.quizWrongPool;

        ChemistryQuiz secilen = null;

        // 1) Havuz turu mu? (oran tavanı burada uygulanır)
        bool havuzTuru = pool.Count > 0 && Random.value < PoolShare;
        if (havuzTuru) secilen = PickFrom(bank, pool, last, inPool: true);

        // 2) Havuz turu değilse havuz DIŞINDAN; o küme boşsa bankanın tamamı
        if (secilen == null) secilen = PickFrom(bank, pool, last, inPool: false);
        if (secilen == null) secilen = PickAny(bank, last);

        if (secilen == null) return null;

        if (SaveService.Data.quizLastAsked != secilen.name)
        {
            SaveService.Data.quizLastAsked = secilen.name;
            SaveService.MarkDirty();
        }
        return secilen;
    }

    /// <summary>
    /// Havuzun İÇİNDEN (<paramref name="inPool"/> true) ya da DIŞINDAN rastgele
    /// bir soru; en son sorulan hariç. Uygun aday yoksa null.
    /// </summary>
    private static ChemistryQuiz PickFrom(IList<ChemistryQuiz> bank, List<string> pool,
                                          string last, bool inPool)
    {
        List<ChemistryQuiz> adaylar = new List<ChemistryQuiz>(bank.Count);
        for (int i = 0; i < bank.Count; i++)
        {
            ChemistryQuiz q = bank[i];
            if (q == null || q.name == last) continue;
            if (pool.Contains(q.name) == inPool) adaylar.Add(q);
        }

        return adaylar.Count == 0 ? null : adaylar[Random.Range(0, adaylar.Count)];
    }

    /// <summary>Bankanın tamamından rastgele biri (son soru hariç).</summary>
    private static ChemistryQuiz PickAny(IList<ChemistryQuiz> bank, string last)
    {
        List<ChemistryQuiz> adaylar = new List<ChemistryQuiz>(bank.Count);
        for (int i = 0; i < bank.Count; i++)
        {
            if (bank[i] != null && bank[i].name != last) adaylar.Add(bank[i]);
        }

        // Bankada tek soru varsa tekrar yasağı uygulanamaz.
        if (adaylar.Count == 0)
        {
            for (int i = 0; i < bank.Count; i++)
            {
                if (bank[i] != null) adaylar.Add(bank[i]);
            }
        }

        return adaylar.Count == 0 ? null : adaylar[Random.Range(0, adaylar.Count)];
    }

    // ------------------------------------------------------------------ kayıt

    /// <summary>
    /// KESİNLEŞMİŞ cevabı kaydeder: yanlışsa soru havuza girer, doğruysa
    /// havuzdan çıkar. Havuzun değiştiği tek yer burasıdır.
    /// </summary>
    public static void RecordAnswer(ChemistryQuiz quiz, bool correct)
    {
        if (quiz == null) return;

        List<string> pool = SaveService.Data.quizWrongPool;
        if (pool == null)
        {
            pool = new List<string>();
            SaveService.Data.quizWrongPool = pool;
        }

        bool degisti;
        if (correct)
        {
            degisti = pool.Remove(quiz.name);
        }
        else
        {
            degisti = !pool.Contains(quiz.name);
            if (degisti) pool.Add(quiz.name);
        }

        if (degisti) SaveService.MarkDirty();
    }

    // --------------------------------------------------------------- bakım

    /// <summary>Bankada karşılığı olmayan havuz girdilerini atar.</summary>
    private static void Prune(IList<ChemistryQuiz> bank)
    {
        List<string> pool = SaveService.Data.quizWrongPool;
        if (pool == null)
        {
            SaveService.Data.quizWrongPool = new List<string>();
            return;
        }
        if (pool.Count == 0) return;

        HashSet<string> adlar = new HashSet<string>();
        for (int i = 0; i < bank.Count; i++)
        {
            if (bank[i] != null) adlar.Add(bank[i].name);
        }

        int atilan = pool.RemoveAll(ad => !adlar.Contains(ad));
        if (atilan > 0)
        {
            Debug.LogWarning($"{Tag}: havuzdan {atilan} tanınmayan soru adı atıldı");
            SaveService.MarkDirty();
        }
    }
}
