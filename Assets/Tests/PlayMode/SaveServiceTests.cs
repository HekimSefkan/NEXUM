using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Kayıt katmanının üç kritik davranışı:
//  1) Eski (şemasız) anahtarlardan göç: 18 değerin hepsi korunuyor
//  2) Veri yokken temiz başlangıç
//  3) Bozuk/eksik veriyle çökmeden varsayılana dönüş
//
// Test, gerçek PlayerPrefs üzerinde çalıştığı için önce mevcut kaydı yedekler,
// sonunda geri yazar.
public class SaveServiceTests
{
    private const string Tag = "NEXUM_SAVE_TEST";

    private string yedek;

    [SetUp]
    public void Yedekle()
    {
        yedek = PlayerPrefs.GetString(SaveService.SaveKey, "");
    }

    [TearDown]
    public void GeriYukle()
    {
        if (string.IsNullOrEmpty(yedek)) PlayerPrefs.DeleteKey(SaveService.SaveKey);
        else PlayerPrefs.SetString(SaveService.SaveKey, yedek);
        PlayerPrefs.Save();
        SaveService.Unload();
    }

    /// <summary>Eski sürümün yazdığı anahtarları kurar.</summary>
    private static void EskiAnahtarlariYaz()
    {
        PlayerPrefs.SetString("PlayerName", "Marie");
        PlayerPrefs.SetString("PlayerEmail", "marie@example.com");
        PlayerPrefs.SetString("NexumID", "NX-4242");
        PlayerPrefs.SetInt("PlayerAvatarIndex", 6);
        PlayerPrefs.SetInt("SelectedLevel", 3);
        PlayerPrefs.SetInt("SelectedGameMode", 1);
        PlayerPrefs.SetInt("UnlockedLevel", 4);
        PlayerPrefs.SetInt("MaxLevelUnlocked", 5);
        PlayerPrefs.SetInt("TotalScore", 12345);
        PlayerPrefs.SetInt("TotalSynthesis", 678);
        PlayerPrefs.SetInt("TotalAccidents", 9);
        PlayerPrefs.SetInt("QuizCorrect", 11);
        PlayerPrefs.SetInt("QuizAttempts", 14);
        PlayerPrefs.SetInt("MusicOn", 0);
        PlayerPrefs.SetInt("SfxOn", 1);
        PlayerPrefs.SetInt("MentorHintsOn", 0);
        PlayerPrefs.SetInt("FlashcardsOn", 1);
        PlayerPrefs.SetInt("TutorialRead_Level_0", 1);
        PlayerPrefs.SetInt("TutorialRead_Level_2", 1);
        PlayerPrefs.Save();
    }

    private static void EskiAnahtarlariSil()
    {
        foreach (string k in new[]
        {
            "PlayerName", "PlayerEmail", "NexumID", "PlayerAvatarIndex", "SelectedLevel",
            "SelectedGameMode", "UnlockedLevel", "MaxLevelUnlocked", "TotalScore",
            "TotalSynthesis", "TotalAccidents", "QuizCorrect", "QuizAttempts",
            "MusicOn", "SfxOn", "MentorHintsOn", "FlashcardsOn",
        }) PlayerPrefs.DeleteKey(k);

        for (int i = 0; i < 32; i++) PlayerPrefs.DeleteKey("TutorialRead_Level_" + i);
        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator EskiAnahtarlardanGocButunDegerleriKoruyor()
    {
        PlayerPrefs.DeleteKey(SaveService.SaveKey);
        EskiAnahtarlariYaz();
        SaveService.Unload();

        SaveData d = SaveService.Data;   // ilk erişim göçü tetikler

        List<string> hatalar = new List<string>();
        void Kontrol(string ad, object beklenen, object gelen)
        {
            bool ok = beklenen.ToString() == gelen.ToString();
            Debug.Log($"{Tag}_GOC: {ad,-22} beklenen={beklenen,-18} gelen={gelen,-18} {(ok ? "OK" : "HATA")}");
            if (!ok) hatalar.Add($"{ad}: {beklenen} != {gelen}");
        }

        Kontrol("playerName", "Marie", d.playerName);
        Kontrol("playerEmail", "marie@example.com", d.playerEmail);
        Kontrol("nexumId", "NX-4242", d.nexumId);
        Kontrol("avatarIndex", 6, d.avatarIndex);
        Kontrol("selectedLevel", 3, d.selectedLevel);
        Kontrol("gameMode", 1, d.gameMode);
        Kontrol("unlockedLevel", 4, d.unlockedLevel);
        Kontrol("maxLevelUnlocked", 5, d.maxLevelUnlocked);
        Kontrol("totalScore", 12345, d.totalScore);
        Kontrol("totalSynthesis", 678, d.totalSynthesis);
        Kontrol("totalAccidents", 9, d.totalAccidents);
        Kontrol("quizCorrect", 11, d.quizCorrect);
        Kontrol("quizAttempts", 14, d.quizAttempts);
        Kontrol("musicOn", 0, d.musicOn);
        Kontrol("sfxOn", 1, d.sfxOn);
        Kontrol("mentorHintsOn", 0, d.mentorHintsOn);
        Kontrol("flashcardsOn", 1, d.flashcardsOn);
        Kontrol("tutorialReadLevels", "0,2", string.Join(",", d.tutorialReadLevels));
        Kontrol("saveVersion", SaveService.CurrentVersion, d.saveVersion);

        // Eski anahtarlar geri dönüş güvenliği için SİLİNMEMELİ
        bool eskiDuruyor = PlayerPrefs.HasKey("PlayerName") && PlayerPrefs.HasKey("TotalSynthesis");
        Debug.Log($"{Tag}_GOC: eski anahtarlar duruyor = {eskiDuruyor}");
        if (!eskiDuruyor) hatalar.Add("eski anahtarlar silinmiş");

        // Göç bir kez çalışır: eski anahtarlar değişse bile yeni kayıt sabit kalır
        PlayerPrefs.SetInt("TotalSynthesis", 1);
        SaveService.Unload();
        int ikinciOkuma = SaveService.Data.totalSynthesis;
        Debug.Log($"{Tag}_GOC: ikinci yuklemede totalSynthesis={ikinciOkuma} (goc tekrarlanmamali)");
        if (ikinciOkuma != 678) hatalar.Add($"göç tekrarlandı: {ikinciOkuma}");

        Debug.Log($"{Tag}_GOC_SONUC: 19 alan kontrol edildi, hata {hatalar.Count}");
        EskiAnahtarlariSil();

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
        yield return null;
    }

    [UnityTest]
    public IEnumerator VeriYokkenTemizBaslangic()
    {
        PlayerPrefs.DeleteKey(SaveService.SaveKey);
        EskiAnahtarlariSil();
        SaveService.Unload();

        SaveData d = SaveService.Data;

        Debug.Log($"{Tag}_TEMIZ: ad=\"{d.playerName}\" sentez={d.totalSynthesis} " +
                  $"muzik={d.musicOn} sfx={d.sfxOn} ipucu={d.mentorHintsOn} " +
                  $"kart={d.flashcardsOn} surum={d.saveVersion} foy={d.tutorialReadLevels.Count}");

        Assert.AreEqual("", d.playerName, "ad boş olmalı");
        Assert.AreEqual(0, d.totalSynthesis);
        Assert.AreEqual(0, d.totalScore);
        Assert.AreEqual(1, d.musicOn, "ses varsayılanı açık");
        Assert.AreEqual(1, d.sfxOn);
        Assert.AreEqual(1, d.mentorHintsOn);
        Assert.AreEqual(1, d.flashcardsOn);
        Assert.AreEqual(SaveService.CurrentVersion, d.saveVersion);
        Assert.IsNotNull(d.tutorialReadLevels);
        Assert.AreEqual(0, d.tutorialReadLevels.Count);
        Assert.IsFalse(SaveService.HasProfile);
        yield return null;
    }

    [UnityTest]
    public IEnumerator BozukVeriCokertmez()
    {
        // Bozuk veri hata loglar; test bunu beklenen sayar.
        LogAssert.ignoreFailingMessages = true;

        // 1) Hiç JSON olmayan içerik
        PlayerPrefs.SetString(SaveService.SaveKey, "{bu json degil!!!");
        EskiAnahtarlariSil();
        SaveService.Unload();
        SaveData bozuk = SaveService.Data;
        Debug.Log($"{Tag}_BOZUK: cozumlenemeyen veri -> ad=\"{bozuk.playerName}\" surum={bozuk.saveVersion}");
        Assert.IsNotNull(bozuk, "çökmemeli");
        Assert.AreEqual(SaveService.CurrentVersion, bozuk.saveVersion);

        // 2) Geçerli JSON ama saçma değerler
        PlayerPrefs.SetString(SaveService.SaveKey,
            "{\"saveVersion\":0,\"playerName\":null,\"avatarIndex\":-7,\"gameMode\":9," +
            "\"totalSynthesis\":-100,\"quizCorrect\":50,\"quizAttempts\":2," +
            "\"musicOn\":5,\"tutorialReadLevels\":null}");
        SaveService.Unload();

        SaveData d = SaveService.Data;
        LogAssert.ignoreFailingMessages = false;

        Debug.Log($"{Tag}_BOZUK: onarim sonrasi ad=\"{d.playerName}\" avatar={d.avatarIndex} " +
                  $"mod={d.gameMode} sentez={d.totalSynthesis} quiz={d.quizCorrect}/{d.quizAttempts} " +
                  $"muzik={d.musicOn} foy={(d.tutorialReadLevels == null ? -1 : d.tutorialReadLevels.Count)} " +
                  $"surum={d.saveVersion}");

        Assert.AreEqual("", d.playerName, "null metin boşa dönmeli");
        Assert.AreEqual(0, d.avatarIndex, "negatif indeks sıfırlanmalı");
        Assert.AreEqual(2, d.gameMode, "mod aralığa sıkıştırılmalı");
        Assert.AreEqual(0, d.totalSynthesis, "negatif sayaç sıfırlanmalı");
        Assert.AreEqual(50, d.quizAttempts, "doğru sayısı denemeyi aşamaz");
        Assert.AreEqual(1, d.musicOn, "ayar 0/1 aralığına sıkıştırılmalı");
        Assert.IsNotNull(d.tutorialReadLevels, "null liste kurtarılmalı");
        Assert.AreEqual(SaveService.CurrentVersion, d.saveVersion, "sürüm damgalanmalı");
        yield return null;
    }
}
