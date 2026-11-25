using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// RÜTBE TABLOSUNUN ÖLÇÜMÜ (Faz 2 / 2.1).
///
/// Tablo yalnızca "geçerli mi" diye değil, TASARIM SÖZÜNÜ tutuyor mu diye
/// ölçülür:
///  - ilk atlama ilk oturumda gelir,
///  - sonraki aralıklar giderek büyür,
///  - bugünkü içeriğin tavanı hangi rütbeye denk geliyor.
///
/// Sayılar teste SABİT YAZILMAZ: hepsi <c>ProgressionTable</c>'dan okunur,
/// yani bir sabit değişince test ölçümü yeniden yapar ve log'a yazar.
/// </summary>
public class RankTableTests
{
    private const string Tag = "NEXUM_RUTBE";

    // Bugünkü içerik (ölçüm girdisi, uydurulmadı):
    //   7 bölüm, 21 tarif, 36 soru.
    private const int Bolum = 7;
    private const int Tarif = 21;
    private const int Soru = 36;

    /// <summary>
    /// Kazanan oyunda bölüm başına KAZANILAN puan (Tools/sim_xp.py, 80 oyun,
    /// derinlik-2 bot). Tavan hesabı bu ölçümden türetilir.
    /// </summary>
    private static readonly int[] BolumPuani = { 58, 57, 81, 251, 215, 204, 430 };

    private string yedek;

    [SetUp]
    public void TemizKayit()
    {
        yedek = SaveService.ExportRaw();
        SaveService.ImportRaw(null);
        SaveService.Data.xpBackfillDone = 1;
        XpSources.Varsayilana();
    }

    [TearDown]
    public void GeriYukle()
    {
        SaveService.ImportRaw(yedek);
    }

    [Test]
    public void EsiklerArtiyorVeAraliklarBuyuyor()
    {
        ProgressionTable.Rank[] r = ProgressionTable.Ranks;
        Assert.AreEqual(7, r.Length, "plan yedi rütbe diyor");
        Assert.AreEqual(0, r[0].threshold, "ilk rütbenin eşiği 0 olmalı");

        List<int> araliklar = new List<int>();
        for (int i = 1; i < r.Length; i++)
        {
            Assert.Greater(r[i].threshold, r[i - 1].threshold,
                $"eşik artmıyor: {i - 1} -> {i}");
            araliklar.Add(r[i].threshold - r[i - 1].threshold);
        }

        Debug.Log($"{Tag}_ARALIK: {string.Join(" / ", araliklar)}");
        for (int i = 1; i < araliklar.Count; i++)
        {
            Assert.Greater(araliklar[i], araliklar[i - 1],
                $"aralık {i} öncekinden büyük değil ({araliklar[i - 1]} -> {araliklar[i]}): " +
                "ilerleme yavaşlamıyor");
        }
    }

    [Test]
    public void EsikGecisleriTamNoktadaOluyor()
    {
        ProgressionTable.Rank[] r = ProgressionTable.Ranks;
        for (int i = 1; i < r.Length; i++)
        {
            Assert.AreEqual(i - 1, ProgressionTable.RankForXp(r[i].threshold - 1),
                $"eşiğin 1 altında rütbe {i} oldu");
            Assert.AreEqual(i, ProgressionTable.RankForXp(r[i].threshold),
                $"eşikte rütbe {i} olmadı");
        }

        Assert.AreEqual(0, ProgressionTable.RankForXp(0));
        Assert.AreEqual(r.Length - 1, ProgressionTable.RankForXp(int.MaxValue / 2));
    }

    /// <summary>
    /// İLK OTURUM: L1 ve L2 bitirilir. L1'in hedefi H₂O, yani en az iki
    /// tarif (H+H, H₂+O) keşfedilir; L2'nin hedefi NaCl, en az bir tarif.
    /// Quiz hesaba KATILMAZ (quiz yalnızca kaybedince açılıyor).
    /// </summary>
    [Test]
    public void IlkRutbeAtlamasiIlkOturumdaGeliyor()
    {
        int xp = 0;
        xp += ProgressionService.AwardLevelComplete(0, BolumPuani[0]);
        xp += ProgressionService.AwardRecipeDiscovery("Tile_H_Tile_H");
        xp += ProgressionService.AwardRecipeDiscovery("Tile_H2_Tile_O");

        int l1Sonu = ProgressionService.Xp;
        int rutbeL1 = ProgressionService.RankIndex;

        xp += ProgressionService.AwardLevelComplete(1, BolumPuani[1]);
        xp += ProgressionService.AwardRecipeDiscovery("Tile_Cl_Tile_Na");

        Debug.Log($"{Tag}_ILKOTURUM: L1 sonu {l1Sonu} XP (rutbe {rutbeL1}), " +
                  $"L2 sonu {ProgressionService.Xp} XP (rutbe {ProgressionService.RankIndex}), " +
                  $"2. rutbe esigi {ProgressionTable.Ranks[1].threshold}");

        Assert.GreaterOrEqual(ProgressionService.RankIndex, 1,
            "ilk oturumda (L1 + L2) rütbe atlanmıyor");
        Assert.AreEqual(0, rutbeL1,
            "ilk bölüm tek başına rütbe atlatıyor: eşik çok düşük");
    }

    /// <summary>
    /// BUGÜNKÜ TAVAN: 7 bölüm (ölçülen puanlarla), 21 tarif, 36 soru.
    /// Sonuç log'a yazılır ve tabloyla karşılaştırılır.
    /// </summary>
    [Test]
    public void BugunkuIcerigin_UlasilabilirEnYuksekRutbesi()
    {
        int bolumXp = 0;
        for (int i = 0; i < Bolum; i++) bolumXp += ProgressionService.AwardLevelComplete(i, BolumPuani[i]);

        int tarifXp = 0;
        for (int i = 0; i < Tarif; i++) tarifXp += ProgressionService.AwardRecipeDiscovery("t" + i);

        int quizXp = 0;
        for (int i = 0; i < Soru; i++) quizXp += ProgressionService.AwardQuizCorrect("s" + i, Soru);

        int toplam = ProgressionService.Xp;
        int rutbe = ProgressionService.RankIndex;

        Debug.Log($"{Tag}_TAVAN: bolum {bolumXp} + tarif {tarifXp} + quiz {quizXp} = {toplam} XP");
        Debug.Log($"{Tag}_TAVAN: ulasilan rutbe {rutbe + 1}/{ProgressionTable.Ranks.Length} " +
                  $"({ProgressionTable.Ranks[rutbe].nameKey}); " +
                  $"sonraki esik {ProgressionTable.NextThreshold(rutbe)}");

        for (int i = 0; i < ProgressionTable.Ranks.Length; i++)
        {
            ProgressionTable.Rank r = ProgressionTable.Ranks[i];
            Debug.Log($"{Tag}_SATIR: {i + 1}. {r.nameKey,-18} esik {r.threshold,5} " +
                      $"{(toplam >= r.threshold ? "ULASILIR" : "ulasilmaz")}");
        }

        Assert.AreEqual(toplam, bolumXp + tarifXp + quizXp);
        Assert.Less(rutbe, ProgressionTable.Ranks.Length - 1,
            "bugünkü içerik son rütbeyi veriyor: Faz 3-5 için yer kalmamış");
        Assert.GreaterOrEqual(rutbe, 3,
            "her şeyi bitiren oyuncu 4. rütbeye bile ulaşamıyor: eşikler çok yüksek");
    }

    [Test]
    public void IlerlemeOraniSinirlarDaDogru()
    {
        ProgressionTable.Rank[] r = ProgressionTable.Ranks;

        SaveService.Data.xp = 0;
        Assert.AreEqual(0f, ProgressionService.RankProgress01, 0.001f);

        SaveService.Data.xp = r[1].threshold - 1;
        float neredeyse = ProgressionService.RankProgress01;

        SaveService.Data.xp = r[r.Length - 1].threshold + 10000;
        float sonRutbe = ProgressionService.RankProgress01;

        Debug.Log($"{Tag}_ORAN: esik-1 -> {neredeyse:F3}, son rutbe -> {sonRutbe:F3}");

        Assert.Greater(neredeyse, 0.9f, "eşiğin hemen altında çubuk dolmamış");
        Assert.AreEqual(1f, sonRutbe, 0.001f, "son rütbede çubuk dolu kalmalı");
    }

    /// <summary>
    /// Rütbe ve rozet adları SÖZLÜKTE olmalı: anahtar ekranda ham
    /// görünmemeli, iki dilde de.
    /// </summary>
    [Test]
    public void RutbeVeRozetAdlariSozlukte()
    {
        Loc.Language onceki = Loc.Current;
        List<string> eksik = new List<string>();

        IEnumerable<string> anahtarlar = ProgressionTable.Ranks.Select(r => r.nameKey)
            .Concat(Badges.All.Select(b => b.nameKey))
            .Concat(new[] { CodeStrings.RankUpTitle, CodeStrings.RankUpHint,
                            CodeStrings.RankXpTotal, CodeStrings.ResultXpLabel,
                            CodeStrings.ResultXpValue });

        foreach (Loc.Language dil in new[] { Loc.Language.Turkish, Loc.Language.English })
        {
            Loc.SetLanguage(dil);
            foreach (string key in anahtarlar)
            {
                string metin = Loc.Get(key);
                if (string.IsNullOrWhiteSpace(metin) || metin == key)
                {
                    eksik.Add($"[{dil}] {key}");
                }
            }
        }

        Loc.SetLanguage(onceki);
        Debug.Log($"{Tag}_SOZLUK: eksik {eksik.Count}");
        Assert.IsEmpty(eksik, "sözlükte karşılığı olmayan anahtar: " + string.Join(" | ", eksik));
    }
}
