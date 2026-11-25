using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// ROZET TABLOSUNUN ÖLÇÜMÜ (Faz 2 / 2.1).
///
/// İki soruya cevap verir:
///  1. Bugün kazanılabilir üç rozet GERÇEKTEN tetikleniyor mu?
///  2. Verisi olmayan rozetler GERÇEKTEN kazanılamıyor mu? ("tanımlı ama
///     kapalı" sözü, yalnızca bayrağa bakarak değil TryEarn'ü çağırarak
///     ölçülür.)
/// </summary>
public class BadgeTests
{
    private const string Tag = "NEXUM_ROZET";
    private const int TarifToplam = 21;

    private string yedek;

    [SetUp]
    public void TemizKayit()
    {
        yedek = SaveService.ExportRaw();
        SaveService.ImportRaw(null);
        SaveService.Data.xpBackfillDone = 1;
    }

    [TearDown]
    public void GeriYukle()
    {
        SaveService.ImportRaw(yedek);
    }

    [Test]
    public void TabloTutarli()
    {
        Assert.AreEqual(9, Badges.All.Length, "plandaki dokuz rozet");
        Assert.AreEqual(3, Badges.AvailableCount, "bugün kazanılabilir rozet sayısı");

        HashSet<string> anahtarlar = new HashSet<string>();
        foreach (Badges.Badge b in Badges.All)
        {
            Assert.IsTrue(anahtarlar.Add(b.key), "yinelenen rozet anahtarı: " + b.key);
            Assert.IsFalse(string.IsNullOrEmpty(b.glyph), b.key + " glifsiz");
            Debug.Log($"{Tag}_SATIR: {b.key,-18} {(b.available ? "ACIK " : "kapali")} " +
                      $"glif={b.glyph,-10} faz={b.phase}");
        }
    }

    [Test]
    public void IlkSentez_IlkBirlesmedeTetikleniyor()
    {
        Assert.IsEmpty(Badges.Evaluate(TarifToplam, false), "sıfır sentezde rozet verildi");

        SaveService.Data.totalSynthesis = 1;
        List<string> yeni = Badges.Evaluate(TarifToplam, false);

        Debug.Log($"{Tag}_ILKSENTEZ: {string.Join(",", yeni)}");
        Assert.Contains(Badges.FirstSynthesis, yeni);
        Assert.IsTrue(Badges.IsEarned(Badges.FirstSynthesis));

        // İkinci değerlendirme aynı rozeti tekrar vermez
        Assert.IsFalse(Badges.Evaluate(TarifToplam, false).Contains(Badges.FirstSynthesis));
    }

    [Test]
    public void TumTepkimeler_DefterDolunca()
    {
        for (int i = 0; i < TarifToplam - 1; i++) SaveService.Data.discoveredRecipes.Add("t" + i);
        Assert.IsFalse(Badges.Evaluate(TarifToplam, false).Contains(Badges.AllReactions),
            "defter dolmadan rozet verildi");

        SaveService.Data.discoveredRecipes.Add("son");
        List<string> yeni = Badges.Evaluate(TarifToplam, false);

        Debug.Log($"{Tag}_TUMTEPKIME: defter {SaveService.Data.discoveredRecipes.Count}/" +
                  $"{TarifToplam} -> {string.Join(",", yeni)}");
        Assert.Contains(Badges.AllReactions, yeni);
    }

    /// <summary>
    /// Tarif sayısı BİLİNMİYORSA (katalog okunamadı) rozet verilmez:
    /// bilinmeyen bir sınıra göre ödül dağıtılmaz.
    /// </summary>
    [Test]
    public void TarifSayisiBilinmiyorsa_TumTepkimelerVerilmez()
    {
        for (int i = 0; i < 50; i++) SaveService.Data.discoveredRecipes.Add("t" + i);

        List<string> yeni = Badges.Evaluate(0, false);
        Debug.Log($"{Tag}_BILINMEYEN: toplam=0 -> {yeni.Count} rozet");
        Assert.IsFalse(yeni.Contains(Badges.AllReactions));
    }

    [Test]
    public void KazasizBolum_YalnizcaKazasizBitiste()
    {
        Assert.IsFalse(Badges.Evaluate(TarifToplam, false).Contains(Badges.FlawlessLevel),
            "kaza olan bölümde rozet verildi");

        List<string> yeni = Badges.Evaluate(TarifToplam, true);
        Debug.Log($"{Tag}_KAZASIZ: {string.Join(",", yeni)}");
        Assert.Contains(Badges.FlawlessLevel, yeni);
    }

    /// <summary>
    /// Verisi olmayan rozet KAZANILAMAZ. Sessizce geçmez: her reddediş
    /// loglanır (bir yerden çağrılırsa görünsün).
    /// </summary>
    [Test]
    public void VerisiOlmayanRozetler_Kazanilamiyor()
    {
        string[] kapali = Badges.All.Where(b => !b.available).Select(b => b.key).ToArray();
        Assert.AreEqual(6, kapali.Length, "kapalı rozet sayısı beklenenden farklı");

        foreach (string key in kapali)
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "verisi olmayan rozet reddedildi -> " + key));

            bool kazanildi = Badges.TryEarn(key);
            Debug.Log($"{Tag}_KAPALI: {key} -> {(kazanildi ? "KAZANILDI (HATA)" : "reddedildi")}");

            Assert.IsFalse(kazanildi, key + " kazanılabildi");
            Assert.IsFalse(Badges.IsEarned(key), key + " kayda girdi");
        }

        Assert.AreEqual(0, Badges.EarnedCount);
    }

    [Test]
    public void TabloDisiAnahtarReddediliyor()
    {
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "tabloda olmayan rozet reddedildi"));

        Assert.IsFalse(Badges.TryEarn("uydurma_rozet"));
        Assert.AreEqual(0, Badges.EarnedCount);
    }

    /// <summary>Rozet XP vermez: ikinci bir XP musluğu açılmadı.</summary>
    [Test]
    public void RozetXpVermiyor()
    {
        SaveService.Data.totalSynthesis = 1;
        int once = ProgressionService.Xp;
        Badges.Evaluate(TarifToplam, true);
        int sonra = ProgressionService.Xp;

        Debug.Log($"{Tag}_XP: rozet oncesi {once}, sonrasi {sonra}, " +
                  $"kazanilan rozet {Badges.EarnedCount}");

        Assert.AreEqual(2, Badges.EarnedCount, "ölçüm anlamsız: rozet kazanılmadı");
        Assert.AreEqual(once, sonra, "rozet XP bastı");
    }
}
