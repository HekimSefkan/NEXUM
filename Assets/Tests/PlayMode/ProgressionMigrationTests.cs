using NUnit.Framework;
using UnityEngine;

/// <summary>
/// KAYIT GÖÇÜ VE GERİYE DÖNÜK HESAP (Faz 2 / 2.1).
///
/// Sürüm 4 alanları (xp, levelXpGranted, quizXpAwarded, quizXpPaid,
/// badges, rankCelebrated) eski kayıtta YOK. Ölçülenler:
///  - eski kayıt çökmeden sürüm 4'e geçiyor ve eski veri korunuyor,
///  - geriye dönük hesap YALNIZCA kaydedilmiş gerçek veriden besleniyor,
///  - hesap bir kez koşuyor,
///  - kutlama bir kez gösteriliyor,
///  - bozuk veri onarılıyor.
/// </summary>
public class ProgressionMigrationTests
{
    private const string Tag = "NEXUM_XP_GOC";

    private const int Bolum = 7;
    private const int Tarif = 21;
    private const int Soru = 36;

    private string yedek;

    [SetUp]
    public void Yedekle()
    {
        yedek = SaveService.ExportRaw();
        XpSources.Varsayilana();
    }

    [TearDown]
    public void GeriYukle()
    {
        SaveService.ImportRaw(yedek);
    }

    [Test]
    public void SurumUcKaydi_SurumDordeGociyorVeEskiVeriKoruyor()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":3,\"playerName\":\"Marie\",\"totalSynthesis\":678," +
            "\"maxLevelUnlocked\":5,\"unlockedLevel\":4,\"musicOn\":0," +
            "\"quizCorrect\":11,\"quizAttempts\":14," +
            "\"discoveredRecipes\":[\"a\",\"b\",\"c\"],\"recipeBackfillDone\":1}");

        SaveData d = SaveService.Data;      // ilk erişim göçü tetikler

        Debug.Log($"{Tag}: surum={d.saveVersion} ad=\"{d.playerName}\" sentez={d.totalSynthesis} " +
                  $"maxBolum={d.maxLevelUnlocked} defter={d.discoveredRecipes.Count} " +
                  $"xp={d.xp} hesapYapildi={d.xpBackfillDone}");

        Assert.AreEqual(SaveService.CurrentVersion, d.saveVersion);
        Assert.AreEqual("Marie", d.playerName);
        Assert.AreEqual(678, d.totalSynthesis);
        Assert.AreEqual(3, d.discoveredRecipes.Count);
        Assert.AreEqual(0, d.xp, "göç adımı XP hesaplamaz, yalnızca bayrağı kurar");
        Assert.AreEqual(0, d.xpBackfillDone, "hesap bayrağı açık kalmalı");
        Assert.IsNotNull(d.levelXpGranted);
        Assert.IsNotNull(d.quizXpAwarded);
        Assert.IsNotNull(d.badges);
    }

    /// <summary>
    /// Geriye dönük hesap: açılan 5 bölüm -> 4 TAMAMLANMIŞ sayılır (son
    /// bölümün bitirilip bitirilmediği kaydedilmedi), 3 tarif, 11 doğru quiz.
    /// Puan payı VERİLMEZ (bölüm başına kazanılan puan hiç kaydedilmedi).
    /// </summary>
    [Test]
    public void GeriyeDonukHesap_YalnizcaKaydedilmisVeridenBesleniyor()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":3,\"maxLevelUnlocked\":5,\"unlockedLevel\":4," +
            "\"quizCorrect\":11,\"quizAttempts\":14," +
            "\"discoveredRecipes\":[\"a\",\"b\",\"c\"],\"recipeBackfillDone\":1}");

        int verilen = ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);

        int beklenen = 4 * ProgressionTable.LevelClearXp
                       + 3 * ProgressionTable.RecipeXp
                       + 11 * ProgressionTable.QuizXp;

        Debug.Log($"{Tag}_HESAP: +{verilen} XP (beklenen {beklenen}), " +
                  $"rutbe {ProgressionService.RankIndex}, " +
                  $"odenen quiz odulu {SaveService.Data.quizXpPaid}");

        Assert.AreEqual(beklenen, verilen);
        Assert.AreEqual(beklenen, ProgressionService.Xp);
        Assert.AreEqual(11, SaveService.Data.quizXpPaid,
            "quiz sayaci dolmazsa ayni ogrenme iki kez odenir");

        // Puan payı verilmedi: bölüm defteri tam olarak LevelClearXp'te
        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(ProgressionTable.LevelClearXp, ProgressionService.GrantedForLevel(i),
                $"bolum {i} defteri puan payi icermemeli");
        }
        Assert.AreEqual(0, ProgressionService.GrantedForLevel(4),
            "son acilan bolum tamamlanmis sayilmamali");
    }

    /// <summary>
    /// Hesaptan sonra oyuncu o bölümleri TEKRAR oynarsa puan payını
    /// kazanabilir: hesap defteri kapatmıyor, yalnızca sabit payı ödüyor.
    /// </summary>
    [Test]
    public void HesaptanSonra_TekrarOynayan_PuanPayiniKazanabiliyor()
    {
        SaveService.ImportRaw("{\"saveVersion\":3,\"maxLevelUnlocked\":3,\"recipeBackfillDone\":1}");
        ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);

        int once = ProgressionService.Xp;
        int verilen = ProgressionService.AwardLevelComplete(0, 300);

        Debug.Log($"{Tag}_TEKRAR: hesap sonrasi {once} XP, bolum 0 tekrar -> +{verilen}");
        Assert.AreEqual(300 / ProgressionTable.ScoreXpDivisor, verilen,
            "yalnizca puan payi verilmeliydi");
    }

    [Test]
    public void HesapBirKezKosar()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":3,\"maxLevelUnlocked\":4,\"quizCorrect\":5," +
            "\"discoveredRecipes\":[\"a\"],\"recipeBackfillDone\":1}");

        int ilk = ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);
        int ikinci = ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);
        int ucuncu = ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);

        Debug.Log($"{Tag}_BIRKEZ: ilk +{ilk}, ikinci +{ikinci}, ucuncu +{ucuncu}, " +
                  $"toplam {ProgressionService.Xp}");

        Assert.Greater(ilk, 0);
        Assert.AreEqual(0, ikinci + ucuncu, "hesap tekrar kostu");
        Assert.AreEqual(ilk, ProgressionService.Xp);
    }

    [Test]
    public void YeniOyuncuda_HesapSifirKalir()
    {
        SaveService.ImportRaw(null);
        int verilen = ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);

        Debug.Log($"{Tag}_YENI: +{verilen} XP, rutbe {ProgressionService.RankIndex}, " +
                  $"kutlama bekliyor mu: {ProgressionService.CelebrationPending}");

        Assert.AreEqual(0, verilen);
        Assert.AreEqual(0, ProgressionService.RankIndex);
        Assert.IsFalse(ProgressionService.CelebrationPending,
            "yeni oyuncuya Cirak rutbesi kutlanmamali");
    }

    /// <summary>
    /// Geriye dönük hesapla rütbe atlayan oyuncu kutlamayı BİR KEZ görür —
    /// her açılışta değil.
    /// </summary>
    [Test]
    public void KutlamaBirKezGosterilir()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":3,\"maxLevelUnlocked\":6,\"quizCorrect\":20," +
            "\"discoveredRecipes\":[\"a\",\"b\",\"c\",\"d\",\"e\",\"f\"],\"recipeBackfillDone\":1}");

        ProgressionService.RunBackfillOnce(Bolum, Tarif, Soru);

        Debug.Log($"{Tag}_KUTLAMA: {ProgressionService.Xp} XP, rutbe " +
                  $"{ProgressionService.RankIndex}, bekliyor={ProgressionService.CelebrationPending}");

        Assert.Greater(ProgressionService.RankIndex, 0, "ölçüm anlamsız: rütbe atlamadı");
        Assert.IsTrue(ProgressionService.CelebrationPending, "kutlama beklemiyor");

        ProgressionService.MarkCelebrationShown();
        Assert.IsFalse(ProgressionService.CelebrationPending, "kutlama ikinci kez bekliyor");

        // Uygulama yeniden açılmış gibi: kayıttan okunur, yine beklemez
        string ham = SaveService.ExportRaw();
        SaveService.ImportRaw(ham);
        Assert.IsFalse(ProgressionService.CelebrationPending,
            "kutlama her acilista tekrar cikiyor");

        // Bir sonraki rütbeye geçince YENİDEN bekler
        SaveService.Data.xp = ProgressionTable.Ranks[ProgressionService.RankIndex + 1].threshold;
        Assert.IsTrue(ProgressionService.CelebrationPending,
            "yeni rutbe kutlanmiyor");
    }

    [Test]
    public void BozukVeriOnariliyor()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":4,\"xp\":-500,\"quizXpPaid\":-3,\"rankCelebrated\":9999," +
            "\"levelXpGranted\":[-10,60,-1],\"quizXpAwarded\":[\"a\",\"a\",\"\",null]," +
            "\"badges\":[\"first_synthesis\",\"first_synthesis\"]}");

        SaveData d = SaveService.Data;

        Debug.Log($"{Tag}_ONARIM: xp={d.xp} odenen={d.quizXpPaid} kutlanan={d.rankCelebrated} " +
                  $"defter=[{string.Join(",", d.levelXpGranted)}] " +
                  $"quizOdul={d.quizXpAwarded.Count} rozet={d.badges.Count}");

        Assert.AreEqual(0, d.xp, "negatif XP sıfırlanmalı");
        Assert.AreEqual(0, d.quizXpPaid);
        Assert.AreEqual(3, d.levelXpGranted.Count, "indeks anlam tasiyor, girdi atilmamali");
        Assert.AreEqual(0, d.levelXpGranted[0]);
        Assert.AreEqual(60, d.levelXpGranted[1], "gecerli deger korunmali");
        Assert.AreEqual(0, d.levelXpGranted[2]);
        Assert.AreEqual(1, d.quizXpAwarded.Count, "yinelenen/bos ad ayiklanmali");
        Assert.AreEqual(1, d.badges.Count);

        // Tabloyu aşan rütbe değeri okuma tarafında kırpılır
        Assert.AreEqual(ProgressionTable.Ranks.Length - 1, ProgressionService.CelebratedRank);
    }
}
