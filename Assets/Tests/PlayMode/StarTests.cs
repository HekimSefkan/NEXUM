using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// ÜÇ YILDIZ KURALLARININ ÖLÇÜMÜ (Faz 2 / 2.2).
///
/// Sahne GEREKMEZ: <c>StarRules</c> ve <c>StarService</c> kendi
/// derlemesinde ve oyun tiplerine bağlı değil. Oyundaki bağlanma
/// noktaları (ipucu, Joker, kazanma ekranı, serbest mod)
/// <c>StarWiringTests</c> içinde ölçülüyor.
/// </summary>
public class StarTests
{
    private const string Tag = "NEXUM_YILDIZ_TEST";
    private const int LevelCount = 7;

    private string yedek;

    [SetUp]
    public void TemizKayit()
    {
        yedek = SaveService.ExportRaw();
        SaveService.ImportRaw(null);
        SaveService.Data.xpBackfillDone = 1;
        SaveService.Data.starBackfillDone = 1;
        XpSources.Varsayilana();
    }

    [TearDown]
    public void GeriYukle()
    {
        XpSources.Varsayilana();
        SaveService.ImportRaw(yedek);
    }

    // ===================================================== koşullar

    [Test]
    public void BirinciYildiz_TamamlamaylaGelir()
    {
        // Sınırın ÜSTÜNDE, yardımla bitirilmiş bir oynanış: yalnızca birinci.
        int limit = StarRules.MoveLimit(0);
        int mask = StarRules.Evaluate(0, limit + 10, true, true, true);

        Debug.Log($"{Tag}_BIR: sinir {limit}, hamle {limit + 10}, yardim var -> {Yaz(mask)}");

        Assert.IsTrue(StarRules.Has(mask, StarRules.Complete));
        Assert.IsFalse(StarRules.Has(mask, StarRules.UnderMoveLimit));
        Assert.IsFalse(StarRules.Has(mask, StarRules.NoHelp));
        Assert.AreEqual(1, StarRules.CountOf(mask));
    }

    [Test]
    public void IkinciYildiz_SinirinAltindaVeTamSinirda()
    {
        int limit = StarRules.MoveLimit(3);

        Assert.IsTrue(StarRules.Has(StarRules.Evaluate(3, limit - 1, true, true, true),
                                    StarRules.UnderMoveLimit), "sınırın altı");
        Assert.IsTrue(StarRules.Has(StarRules.Evaluate(3, limit, true, true, true),
                                    StarRules.UnderMoveLimit), "tam sınır dahil olmalı");
        Assert.IsFalse(StarRules.Has(StarRules.Evaluate(3, limit + 1, true, true, true),
                                     StarRules.UnderMoveLimit), "sınırın bir üstü");

        Debug.Log($"{Tag}_IKI: L4 siniri {limit}; {limit - 1} ve {limit} alir, {limit + 1} almaz");
    }

    [Test]
    public void UcuncuYildiz_IpucuVeUcretliJokerBozar()
    {
        int az = 1;

        Assert.IsTrue(StarRules.Has(StarRules.Evaluate(0, az, false, false, true),
                                    StarRules.NoHelp), "yardımsız");
        Assert.IsFalse(StarRules.Has(StarRules.Evaluate(0, az, true, false, true),
                                     StarRules.NoHelp), "ipucu bozmalı");
        Assert.IsFalse(StarRules.Has(StarRules.Evaluate(0, az, false, true, true),
                                     StarRules.NoHelp), "ücretli Joker bozmalı");

        Debug.Log($"{Tag}_UC: ipucu ve ucretli Joker ucuncu yildizi bozuyor");
    }

    /// <summary>
    /// Katalizörün BEDAVA Joker hakkı üçüncü yıldızı bozmaz. Kural
    /// burada "paidJokerUsed = false" olarak görünür; bedava hakkın
    /// bayrağı hiç yazılmıyor (TileInteraction ölçümü
    /// StarWiringTests'te).
    /// </summary>
    [Test]
    public void BedavaJoker_UcuncuYildiziBozmaz()
    {
        int mask = StarRules.Evaluate(0, 1, false, false, true);

        Debug.Log($"{Tag}_KATALIZOR: bedava Joker sonrasi -> {Yaz(mask)}");
        Assert.IsTrue(StarRules.Has(mask, StarRules.NoHelp));
        Assert.AreEqual(StarRules.All, mask, "bedava Joker uc yildizi engellememeli");
    }

    /// <summary>Yıldızlar BAĞIMSIZ: ikinci kaçıp üçüncü kazanılabilir.</summary>
    [Test]
    public void Yildizlar_Bagimsiz()
    {
        int limit = StarRules.MoveLimit(2);
        int mask = StarRules.Evaluate(2, limit + 5, false, false, true);

        Debug.Log($"{Tag}_BAGIMSIZ: sinir {limit}, hamle {limit + 5}, yardimsiz -> {Yaz(mask)}");

        Assert.IsFalse(StarRules.Has(mask, StarRules.UnderMoveLimit), "ikinci kaçmalı");
        Assert.IsTrue(StarRules.Has(mask, StarRules.NoHelp), "üçüncü kazanılmalı");
        Assert.AreEqual(2, StarRules.CountOf(mask));

        // Tersi de mümkün: sınırın altında ama yardımla
        int ters = StarRules.Evaluate(2, 1, true, false, true);
        Assert.IsTrue(StarRules.Has(ters, StarRules.UnderMoveLimit));
        Assert.IsFalse(StarRules.Has(ters, StarRules.NoHelp));
    }

    [Test]
    public void SerbestModda_HicYildizYok()
    {
        int mask = StarRules.Evaluate(0, 1, false, false, starsAllowed: false);

        Debug.Log($"{Tag}_SERBEST: kusursuz oynanis ama starsAllowed=false -> {Yaz(mask)}");
        Assert.AreEqual(0, mask, "serbest modda yıldız verildi");
    }

    /// <summary>
    /// Sınırı olmayan bölümde ikinci yıldız DEĞERLENDİRİLMEZ:
    /// bilinmeyen bir sınıra göre yıldız verilmez.
    /// </summary>
    [Test]
    public void SiniriOlmayanBolumde_IkinciYildizVerilmez()
    {
        int gelecekBolum = StarRules.MoveLimits.Length;
        Assert.AreEqual(-1, StarRules.MoveLimit(gelecekBolum));

        int mask = StarRules.Evaluate(gelecekBolum, 1, false, false, true);
        Debug.Log($"{Tag}_SINIRSIZ: bolum {gelecekBolum} -> {Yaz(mask)}");

        Assert.IsFalse(StarRules.Has(mask, StarRules.UnderMoveLimit));
        Assert.IsTrue(StarRules.Has(mask, StarRules.NoHelp), "üçüncü yine verilebilmeli");
    }

    [Test]
    public void SinirTablosu_HerBolumIcinTanimli()
    {
        Assert.AreEqual(LevelCount, StarRules.MoveLimits.Length,
            "oyundaki bölüm sayısı kadar sınır olmalı");

        for (int i = 0; i < StarRules.MoveLimits.Length; i++)
        {
            Assert.Greater(StarRules.MoveLimits[i], 0, $"L{i + 1} sınırı pozitif olmalı");
        }

        Debug.Log($"{Tag}_TABLO: " + string.Join(" · ",
            System.Array.ConvertAll(StarRules.MoveLimits, x => x.ToString())));
    }

    // ===================================================== muhasebe

    [Test]
    public void EnIyiSonucKorunur_KotuSonucUstuneYazmaz()
    {
        StarService.Record(0, StarRules.All, 5);
        Assert.AreEqual(StarRules.All, StarService.StarsFor(0));
        Assert.AreEqual(5, StarService.BestMoves(0));

        // Kötü oynanış: yalnızca birinci yıldız, daha çok hamle
        StarService.Record(0, StarRules.Complete, 40);

        Debug.Log($"{Tag}_ENIYI: kotu oynanis sonrasi {Yaz(StarService.StarsFor(0))}, " +
                  $"en iyi hamle {StarService.BestMoves(0)}");

        Assert.AreEqual(StarRules.All, StarService.StarsFor(0), "yıldız kaybedildi");
        Assert.AreEqual(5, StarService.BestMoves(0), "en iyi hamle bozuldu");
    }

    [Test]
    public void YildizlarBirikir()
    {
        StarService.Record(1, StarRules.Complete | StarRules.NoHelp, 30);
        StarService.Record(1, StarRules.Complete | StarRules.UnderMoveLimit, 8);

        Debug.Log($"{Tag}_BIRIKIM: {Yaz(StarService.StarsFor(1))}, " +
                  $"en iyi hamle {StarService.BestMoves(1)}");

        Assert.AreEqual(StarRules.All, StarService.StarsFor(1),
            "iki ayrı oynanıştan gelen yıldızlar birikmeli");
        Assert.AreEqual(8, StarService.BestMoves(1));
    }

    [Test]
    public void YildizXpsi_YildizBasinaBirKez()
    {
        int ilk = StarService.Record(2, StarRules.All, 10);
        int tekrar = 0;
        for (int i = 0; i < 10; i++) tekrar += StarService.Record(2, StarRules.All, 10);

        Debug.Log($"{Tag}_XP: ilk {ilk} XP, 10 tekrar {tekrar} XP, toplam {ProgressionService.Xp}");

        Assert.AreEqual(3 * ProgressionTable.StarXp, ilk);
        Assert.AreEqual(0, tekrar, "aynı yıldız tekrar XP ödedi");
    }

    [Test]
    public void KismiYildiz_YalnizcaYeniBitiOduyor()
    {
        int ilk = StarService.Record(3, StarRules.Complete, 50);
        int sonra = StarService.Record(3, StarRules.Complete | StarRules.NoHelp, 50);

        Debug.Log($"{Tag}_KISMI: birinci {ilk} XP, sonra ucuncu eklenince {sonra} XP");

        Assert.AreEqual(ProgressionTable.StarXp, ilk);
        Assert.AreEqual(ProgressionTable.StarXp, sonra, "yalnızca YENİ yıldız ödenmeli");
    }

    [Test]
    public void SerbestModSonucu_KaydaGirmez()
    {
        int xp = StarService.Record(4, 0, 12);

        Debug.Log($"{Tag}_SERBEST_KAYIT: xp={xp}, maske={StarService.StarsFor(4)}, " +
                  $"en iyi hamle={StarService.BestMoves(4)}");

        Assert.AreEqual(0, xp);
        Assert.AreEqual(0, StarService.StarsFor(4));
        Assert.AreEqual(0, StarService.BestMoves(4), "yıldızsız sonuç hamle kaydı bırakmamalı");
    }

    [Test]
    public void KapaliKaynak_YildizXpsiVermez()
    {
        XpSources.Stars = false;
        int xp = StarService.Record(0, StarRules.All, 5);

        Debug.Log($"{Tag}_KAPALI: XpSources.Stars=false -> {xp} XP");

        Assert.AreEqual(0, xp);
        Assert.AreEqual(StarRules.All, StarService.StarsFor(0),
            "bayrak kapalıyken bile yıldız KAYDEDİLMELİ, yalnızca XP verilmemeli");
    }

    [Test]
    public void UcYildizliBolumRozeti_UcuTamamlanincaGelir()
    {
        StarService.Record(0, StarRules.Complete | StarRules.UnderMoveLimit, 5);
        Assert.IsFalse(StarService.HasAnyThreeStarLevel, "iki yıldız yetmemeli");
        Assert.IsFalse(Badges.Evaluate(21, false).Contains(Badges.ThreeStars));

        StarService.Record(0, StarRules.All, 5);
        Assert.IsTrue(StarService.HasAnyThreeStarLevel);

        List<string> yeni = Badges.Evaluate(21, false);
        Debug.Log($"{Tag}_ROZET: {string.Join(",", yeni)}");
        Assert.Contains(Badges.ThreeStars, yeni);
    }

    [Test]
    public void ToplamYildizSayilir()
    {
        StarService.Record(0, StarRules.All, 5);
        StarService.Record(1, StarRules.Complete, 9);
        StarService.Record(2, StarRules.Complete | StarRules.NoHelp, 20);

        Debug.Log($"{Tag}_TOPLAM: {StarService.TotalStars} yildiz");
        Assert.AreEqual(6, StarService.TotalStars);
    }

    // ===================================================== göç

    [Test]
    public void SurumDortKaydi_SurumBeseGociyor()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":4,\"playerName\":\"Marie\",\"maxLevelUnlocked\":5," +
            "\"unlockedLevel\":4,\"xp\":600,\"xpBackfillDone\":1}");

        SaveData d = SaveService.Data;

        Debug.Log($"{Tag}_GOC: surum={d.saveVersion} ad=\"{d.playerName}\" xp={d.xp} " +
                  $"yildizHesap={d.starBackfillDone} liste={d.levelStars.Count}");

        Assert.AreEqual(SaveService.CurrentVersion, d.saveVersion);
        Assert.AreEqual("Marie", d.playerName, "eski veri korunmalı");
        Assert.AreEqual(600, d.xp);
        Assert.AreEqual(0, d.starBackfillDone, "hesap bayrağı açık kalmalı");
        Assert.IsNotNull(d.levelStars);
        Assert.IsNotNull(d.levelBestMoves);
    }

    /// <summary>
    /// Geriye dönük hesap YALNIZCA birinci yıldızı verir: ikinci hamle
    /// sayısı, üçüncü yardım geçmişi ister ve ikisi de kaydedilmedi.
    /// </summary>
    [Test]
    public void GeriyeDonukHesap_YalnizcaBirinciYildiz()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":4,\"maxLevelUnlocked\":5,\"unlockedLevel\":4,\"xpBackfillDone\":1}");

        int xp = StarService.RunBackfillOnce(LevelCount);

        Debug.Log($"{Tag}_HESAP: +{xp} XP, toplam yildiz {StarService.TotalStars}");

        // maxLevelUnlocked 5 -> 4 TAMAMLANMIŞ bölüm (son bölüm sayılmaz)
        Assert.AreEqual(4, StarService.TotalStars);
        Assert.AreEqual(4 * ProgressionTable.StarXp, xp);

        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(StarRules.Complete, StarService.StarsFor(i),
                $"bolum {i} yalnizca birinci yildizi almali");
            Assert.AreEqual(0, StarService.BestMoves(i),
                $"bolum {i} icin hamle sayisi UYDURULMAMALI");
        }
        Assert.AreEqual(0, StarService.StarsFor(4), "son acilan bolum tamamlanmis sayilmamali");
    }

    [Test]
    public void GeriyeDonukHesap_BirKezKosar()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":4,\"maxLevelUnlocked\":4,\"xpBackfillDone\":1}");

        int ilk = StarService.RunBackfillOnce(LevelCount);
        int ikinci = StarService.RunBackfillOnce(LevelCount);

        Debug.Log($"{Tag}_BIRKEZ: ilk +{ilk}, ikinci +{ikinci}");

        Assert.Greater(ilk, 0);
        Assert.AreEqual(0, ikinci, "hesap tekrar koştu");
    }

    [Test]
    public void BozukYildizVerisi_Onarilir()
    {
        SaveService.ImportRaw(
            "{\"saveVersion\":5,\"levelStars\":[255,-3,7],\"levelBestMoves\":[-5,12,0]}");

        SaveData d = SaveService.Data;

        Debug.Log($"{Tag}_ONARIM: maskeler=[{string.Join(",", d.levelStars)}] " +
                  $"hamleler=[{string.Join(",", d.levelBestMoves)}]");

        Assert.AreEqual(StarRules.All, d.levelStars[0], "tanımsız bitler kırpılmalı");
        Assert.AreEqual(0, d.levelStars[1], "negatif maske sıfırlanmalı");
        Assert.AreEqual(StarRules.All, d.levelStars[2], "geçerli maske korunmalı");
        Assert.AreEqual(0, d.levelBestMoves[0], "negatif hamle sıfırlanmalı");
        Assert.AreEqual(12, d.levelBestMoves[1]);
        Assert.AreEqual(3, d.levelStars.Count, "indeks anlam taşıyor, girdi atılmamalı");
    }

    private static string Yaz(int mask)
    {
        return (StarRules.Has(mask, StarRules.Complete) ? "1" : "-")
             + (StarRules.Has(mask, StarRules.UnderMoveLimit) ? "2" : "-")
             + (StarRules.Has(mask, StarRules.NoHelp) ? "3" : "-");
    }
}
