using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// XP KURALLARININ ÖLÇÜMÜ (Faz 2 / 2.1).
///
/// Sahne GEREKMEZ: <c>ProgressionService</c> kendi derlemesinde ve içerik
/// sayıları parametre olarak geliyor. Bu yüzden testler kuralı doğrudan
/// ölçer, oyun kurulumuna bağlı kalmaz. Oyun içindeki bağlanma noktaları
/// (birleşme, quiz, kazanma ekranı) <c>ProgressionWiringTests</c> içinde.
/// </summary>
public class ProgressionTests
{
    private const string Tag = "NEXUM_XP_TEST";

    private const int QuizBank = 36;
    private const int RecipeTotal = 21;
    private const int LevelCount = 7;

    private string yedek;

    [SetUp]
    public void TemizKayit()
    {
        yedek = SaveService.ExportRaw();
        SaveService.ImportRaw(null);          // temiz başlangıç
        SaveService.Data.xpBackfillDone = 1;  // göç bu testlerin konusu değil
        XpSources.Varsayilana();
    }

    [TearDown]
    public void GeriYukle()
    {
        XpSources.Varsayilana();
        SaveService.ImportRaw(yedek);
    }

    // ===================================================== kaynaklar

    [Test]
    public void UcKaynakTablodakiTutariVeriyor()
    {
        int bolum = ProgressionService.AwardLevelComplete(0, 100);
        int beklenenBolum = ProgressionTable.LevelClearXp + 100 / ProgressionTable.ScoreXpDivisor;

        int tarif = ProgressionService.AwardRecipeDiscovery("Tile_H_Tile_O");
        int quiz = ProgressionService.AwardQuizCorrect("Soru_1", QuizBank);

        Debug.Log($"{Tag}: bolum={bolum} (beklenen {beklenenBolum}), tarif={tarif}, " +
                  $"quiz={quiz}, toplam={ProgressionService.Xp}");

        Assert.AreEqual(beklenenBolum, bolum, "bölüm XP'si tablodan gelmiyor");
        Assert.AreEqual(ProgressionTable.RecipeXp, tarif);
        Assert.AreEqual(ProgressionTable.QuizXp, quiz);
        Assert.AreEqual(beklenenBolum + tarif + quiz, ProgressionService.Xp);
    }

    [Test]
    public void KapaliKaynakXpVermiyor()
    {
        XpSources.LevelComplete = false;
        XpSources.RecipeDiscovery = false;
        XpSources.QuizCorrect = false;

        int a = ProgressionService.AwardLevelComplete(0, 500);
        int b = ProgressionService.AwardRecipeDiscovery("Tile_H_Tile_O");
        int c = ProgressionService.AwardQuizCorrect("Soru_1", QuizBank);

        Debug.Log($"{Tag}_KAPALI: {a} / {b} / {c}, toplam XP {ProgressionService.Xp}");
        Assert.AreEqual(0, a + b + c, "bayrağı kapalı kaynak XP verdi");
        Assert.AreEqual(0, ProgressionService.Xp);
    }

    /// <summary>
    /// XP KAZANILAN puandan beslenir. Kalan skordan beslenseydi geri alma /
    /// ipucu / Joker harcayan oyuncu iki kez cezalandırılırdı.
    /// </summary>
    [Test]
    public void XpKazanilanPuandanBesleniyor()
    {
        const int kazanilan = 800;
        const int harcanan = 300;             // geri alma + Joker
        int kalan = kazanilan - harcanan;

        int verilen = ProgressionService.AwardLevelComplete(0, kazanilan);
        int kalandanOlsaydi = ProgressionTable.LevelClearXp + kalan / ProgressionTable.ScoreXpDivisor;

        Debug.Log($"{Tag}_KAYNAK: kazanilan {kazanilan} -> {verilen} XP; " +
                  $"kalan {kalan} olsaydi {kalandanOlsaydi} XP");

        Assert.AreEqual(ProgressionTable.LevelClearXp + kazanilan / ProgressionTable.ScoreXpDivisor,
                        verilen);
        Assert.Greater(verilen, kalandanOlsaydi,
            "ölçüm anlamsız: harcama XP'yi değiştirmiyor olurdu");
    }

    // ===================================================== çiftlik koruması

    /// <summary>
    /// Aynı bölümü 10 kez AYNI sonuçla bitirmek bir kezlik XP'den fazlasını
    /// vermez.
    /// </summary>
    [Test]
    public void AyniBolumOnKezBitirilince_XpArtisiSinirli()
    {
        const int puan = 250;
        int ilk = ProgressionService.AwardLevelComplete(3, puan);
        int sonraki = 0;

        for (int i = 0; i < 10; i++) sonraki += ProgressionService.AwardLevelComplete(3, puan);

        Debug.Log($"{Tag}_CIFTLIK: ilk {ilk} XP, sonraki 10 tekrar {sonraki} XP, " +
                  $"toplam {ProgressionService.Xp}");

        Assert.Greater(ilk, 0, "ilk tamamlama XP vermedi");
        Assert.AreEqual(0, sonraki, "tekrar oynamak XP bastı");
        Assert.AreEqual(ilk, ProgressionService.Xp);
    }

    /// <summary>Daha iyi sonuç YALNIZCA farkı öder.</summary>
    [Test]
    public void DahaIyiSonuc_YalnizcaFarkiOduyor()
    {
        int ilk = ProgressionService.AwardLevelComplete(2, 100);
        int kotu = ProgressionService.AwardLevelComplete(2, 60);
        int iyi = ProgressionService.AwardLevelComplete(2, 300);

        int beklenenFark = (300 - 100) / ProgressionTable.ScoreXpDivisor;

        Debug.Log($"{Tag}_ENIYI: 100 puan -> {ilk}, 60 puan -> {kotu}, 300 puan -> {iyi} " +
                  $"(beklenen fark {beklenenFark})");

        Assert.AreEqual(0, kotu, "daha kötü sonuç XP verdi");
        Assert.AreEqual(beklenenFark, iyi, "daha iyi sonuç farktan fazlasını verdi");
    }

    /// <summary>Her bölümün kendi defteri var; biri diğerini kapatmaz.</summary>
    [Test]
    public void BolumlerinDefteriAyri()
    {
        for (int i = 0; i < LevelCount; i++) ProgressionService.AwardLevelComplete(i, 100);

        int beklenen = LevelCount * (ProgressionTable.LevelClearXp +
                                     100 / ProgressionTable.ScoreXpDivisor);
        Debug.Log($"{Tag}_DEFTER: {LevelCount} bölüm -> {ProgressionService.Xp} XP " +
                  $"(beklenen {beklenen})");
        Assert.AreEqual(beklenen, ProgressionService.Xp);
    }

    // ===================================================== quiz kötüye kullanım

    /// <summary>
    /// "Bilerek yanlış yap, sonra doğru yap" döngüsü: aynı soru ömründe BİR
    /// KEZ öder.
    /// </summary>
    [Test]
    public void AyniSoruOnKezDogruCevaplaninca_BirKezOduyor()
    {
        int ilk = ProgressionService.AwardQuizCorrect("Soru_7", QuizBank);
        int sonraki = 0;

        for (int i = 0; i < 10; i++)
        {
            // Yanlış cevap havuza sokar, doğru cevap çıkarır: döngü budur.
            QuizSelectorHavuzunaSok("Soru_7");
            sonraki += ProgressionService.AwardQuizCorrect("Soru_7", QuizBank);
        }

        Debug.Log($"{Tag}_QUIZ: ilk {ilk} XP, 10 tekrar {sonraki} XP, " +
                  $"odenen odul {SaveService.Data.quizXpPaid}");

        Assert.AreEqual(ProgressionTable.QuizXp, ilk);
        Assert.AreEqual(0, sonraki, "aynı soru ikinci kez XP ödedi");
        Assert.AreEqual(1, SaveService.Data.quizXpPaid);
    }

    /// <summary>Ödenen ödül sayısı banka boyutunu aşamaz.</summary>
    [Test]
    public void QuizOdulleriBankaBoyunuAsmiyor()
    {
        const int banka = 5;
        for (int i = 0; i < 20; i++) ProgressionService.AwardQuizCorrect("Soru_" + i, banka);

        Debug.Log($"{Tag}_TAVAN: 20 farkli soru, banka {banka} -> " +
                  $"odenen {SaveService.Data.quizXpPaid}, XP {ProgressionService.Xp}");

        Assert.AreEqual(banka, SaveService.Data.quizXpPaid);
        Assert.AreEqual(banka * ProgressionTable.QuizXp, ProgressionService.Xp);
    }

    // ===================================================== tarif keşfi

    [Test]
    public void TarifKesfiHerAnahtariBirKezOduyor()
    {
        // Çağıran taraf (GridManager) yalnızca YENİ keşifte çağırıyor; burada
        // ölçülen, kaynağın miktarının tablodan gelmesi ve toplamın 21 tarifle
        // sınırlı kalması.
        for (int i = 0; i < RecipeTotal; i++) ProgressionService.AwardRecipeDiscovery("tarif_" + i);

        Debug.Log($"{Tag}_TARIF: {RecipeTotal} kesif -> {ProgressionService.Xp} XP");
        Assert.AreEqual(RecipeTotal * ProgressionTable.RecipeXp, ProgressionService.Xp);
    }

    // ===================================================== yardımcı

    /// <summary>
    /// Kötüye kullanım döngüsünün ilk yarısı: soruyu yanlış cevapla.
    /// <c>QuizSelector.RecordAnswer</c> bir <c>ChemistryQuiz</c> istiyor ve
    /// o tip Assembly-CSharp'ta; test havuzu doğrudan yazar, ölçülen şey
    /// zaten XP tarafı.
    /// </summary>
    private static void QuizSelectorHavuzunaSok(string ad)
    {
        List<string> havuz = SaveService.Data.quizWrongPool;
        if (!havuz.Contains(ad)) havuz.Add(ad);
        SaveService.MarkDirty();
    }
}
