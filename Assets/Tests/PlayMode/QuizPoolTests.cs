using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Yanlış cevap havuzu ve soru seçimi (1.5).
///
/// Neyi koruyor:
///  1. Yanlış cevaplanan soru havuza GİRER, doğru cevaplanınca ÇIKAR.
///  2. Aynı soru ÜST ÜSTE sorulmaz.
///  3. Havuzun payı tavanda kalır: havuz ne kadar büyürse büyüsün
///     çekilişlerin yalnızca <c>QuizSelector.PoolShare</c> kadarı havuzdan
///     gelir. Bu kuralın bozulması "oyuncu hep aynı 5 soruyu görüyor"
///     demektir ve rastgeleliğin içinde fark edilmez.
///  4. Havuz YALNIZCA kesinleşmiş cevapla değişir: soru seçmek (panelin
///     açılması) havuzu değiştirmez.
///  5. Bankada olmayan ad (silinmiş/yeniden adlandırılmış soru) ayıklanır.
///  6. Eski kayıt sürüm 3'e göç eder, bozuk havuz onarılır.
///
/// Kalıcı veriye ham erişim <c>SaveService.ExportRaw/ImportRaw</c> kapısından
/// (PlayerPrefs'e doğrudan dokunulmaz).
/// </summary>
public class QuizPoolTests
{
    private const string Tag = "NEXUM_QUIZPOOL";

    private string yedek;
    private IList banka;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        banka = Reflect.Get(ui, "allQuizzes") as IList;
        Assert.IsNotNull(banka, "banka okunamadı");
        Assert.Greater(banka.Count, 4, "banka ölçüm için çok küçük");

        SaveService.Data.quizWrongPool.Clear();
        SaveService.Data.quizLastAsked = "";
    }

    [TearDown]
    public void GeriYukle()
    {
        SaveService.ImportRaw(yedek);
    }

    // --------------------------------------------------------------- yardımcı

    private object Soru(int i) { return banka[i]; }

    private static string Ad(object quiz) { return ((ScriptableObject)quiz).name; }

    private object Sec() { return Reflect.Call("QuizSelector", "Pick", banka); }

    private void Kaydet(object quiz, bool dogru)
    {
        Reflect.Call("QuizSelector", "RecordAnswer", quiz, dogru);
    }

    // ----------------------------------------------------------------- testler

    [UnityTest]
    public IEnumerator YanlisCevapHavuzaGirerDogruCevapCikarir()
    {
        object q = Soru(0);
        List<string> havuz = SaveService.Data.quizWrongPool;

        Kaydet(q, false);
        Debug.Log($"{Tag}: yanlış sonrası havuz = [{string.Join(", ", havuz)}]");
        Assert.Contains(Ad(q), havuz, "yanlış cevaplanan soru havuza girmedi");

        // Aynı soru iki kez yanlış: havuz küme gibi davranmalı
        Kaydet(q, false);
        Assert.AreEqual(1, havuz.Count(a => a == Ad(q)), "havuzda aynı soru iki kez duruyor");

        Kaydet(q, true);
        Debug.Log($"{Tag}: doğru sonrası havuz = [{string.Join(", ", havuz)}]");
        Assert.IsFalse(havuz.Contains(Ad(q)), "doğru cevaplanan soru havuzdan çıkmadı");

        yield return null;
    }

    [UnityTest]
    public IEnumerator AyniSoruUstUsteSorulmaz()
    {
        // Havuz dolu olsun: tekrar yasağı havuz turunda da geçerli olmalı
        SaveService.Data.quizWrongPool.AddRange(new[] { Ad(Soru(0)), Ad(Soru(1)) });

        string onceki = "";
        int tekrar = 0;
        const int cekilis = 300;

        for (int i = 0; i < cekilis; i++)
        {
            object q = Sec();
            Assert.IsNotNull(q, "seçim null döndü");
            string ad = Ad(q);
            if (ad == onceki) tekrar++;
            onceki = ad;
        }

        Debug.Log($"{Tag}: {cekilis} çekilişte üst üste tekrar = {tekrar}");
        Assert.AreEqual(0, tekrar, "aynı soru üst üste geldi");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HavuzunPayiTavandaKaliyor()
    {
        // Havuz bankanın yaklaşık onda biri; pay havuz boyutundan BAĞIMSIZ
        // olmalı, yani bu oran değiştirilse de sonuç ~1/3 çıkmalı.
        string[] havuz = { Ad(Soru(0)), Ad(Soru(1)), Ad(Soru(2)), Ad(Soru(3)) };
        SaveService.Data.quizWrongPool.AddRange(havuz);
        HashSet<string> havuzKume = new HashSet<string>(havuz);

        const int cekilis = 3000;
        int havuzdan = 0;
        Dictionary<string, int> sayim = new Dictionary<string, int>();

        for (int i = 0; i < cekilis; i++)
        {
            string ad = Ad(Sec());
            if (havuzKume.Contains(ad)) havuzdan++;
            sayim[ad] = sayim.TryGetValue(ad, out int n) ? n + 1 : 1;
        }

        float pay = (float)havuzdan / cekilis;
        float beklenen = Reflect.Prop<float>("QuizSelector", "PoolShare");

        Debug.Log($"{Tag}: havuz {havuz.Length}/{banka.Count}, {cekilis} çekiliş -> " +
                  $"havuzdan %{pay * 100f:F1} (beklenen %{beklenen * 100f:F1})");
        Debug.Log($"{Tag}: havuz kaydı tutulmadı mı diye -> havuz hâlâ " +
                  $"{SaveService.Data.quizWrongPool.Count} girdi");

        // 3000 çekilişte dört standart sapma payı yaklaşık 0,034
        Assert.That(pay, Is.EqualTo(beklenen).Within(0.045f),
            $"havuz payı beklenenden saptı: %{pay * 100f:F1}");
        Assert.Less(pay, 0.5f, "havuz baskın hâle geldi");

        // Havuz dışındaki sorular da gerçekten geliyor mu?
        int havuzDisi = sayim.Keys.Count(a => !havuzKume.Contains(a));
        Debug.Log($"{Tag}: çekilişte görülen havuz dışı soru sayısı = {havuzDisi}");
        Assert.Greater(havuzDisi, 10, "havuz dışı sorular yeterince gelmiyor");

        yield return null;
    }

    [UnityTest]
    public IEnumerator SoruSecmekHavuzuDegistirmez()
    {
        SaveService.Data.quizWrongPool.AddRange(new[] { Ad(Soru(0)), Ad(Soru(1)) });
        string once = string.Join(",", SaveService.Data.quizWrongPool);

        for (int i = 0; i < 100; i++) Sec();

        string sonra = string.Join(",", SaveService.Data.quizWrongPool);
        Debug.Log($"{Tag}: 100 çekiliş öncesi [{once}] sonrası [{sonra}]");
        Assert.AreEqual(once, sonra, "soru seçmek havuzu değiştirdi");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TaninmayanAdHavuzdanAyiklanir()
    {
        SaveService.Data.quizWrongPool.AddRange(new[] { "Soru_Olmayan", Ad(Soru(0)) });

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("tanınmayan soru adı"));
        Sec();

        List<string> havuz = SaveService.Data.quizWrongPool;
        Debug.Log($"{Tag}: ayıklama sonrası havuz = [{string.Join(", ", havuz)}]");
        Assert.IsFalse(havuz.Contains("Soru_Olmayan"), "bankada olmayan ad havuzda kaldı");
        Assert.Contains(Ad(Soru(0)), havuz, "geçerli ad da atılmış");
        yield return null;
    }

    // ------------------------------------------------------------------- göç

    [UnityTest]
    public IEnumerator EskiKayitSurum3eGocEder()
    {
        // Sürüm 2 kaydı: havuz alanları HİÇ YOK
        SaveService.ImportRaw("{\"saveVersion\":2,\"playerName\":\"Marie\"," +
                              "\"totalSynthesis\":678,\"quizCorrect\":11,\"quizAttempts\":14," +
                              "\"maxLevelUnlocked\":5}");

        SaveData d = SaveService.Data;

        Debug.Log($"{Tag}: göç sonrası sürüm={d.saveVersion} havuz={d.quizWrongPool.Count} " +
                  $"son=\"{d.quizLastAsked}\" sentez={d.totalSynthesis}");

        Assert.AreEqual(SaveService.CurrentVersion, d.saveVersion, "sürüm yükselmedi");
        Assert.IsNotNull(d.quizWrongPool, "havuz null");
        Assert.IsEmpty(d.quizWrongPool, "eski kayda havuz UYDURULDU (geriye dönük doldurma yok)");
        Assert.AreEqual("", d.quizLastAsked, "son sorulan soru alanı boş değil");

        // Eski veriler korunmuş olmalı
        Assert.AreEqual("Marie", d.playerName);
        Assert.AreEqual(678, d.totalSynthesis);
        Assert.AreEqual(11, d.quizCorrect);
        Assert.AreEqual(14, d.quizAttempts);
        yield return null;
    }

    [UnityTest]
    public IEnumerator BozukHavuzOnarilir()
    {
        // Yineleme + boş girdi + boşluktan oluşan girdi
        SaveService.ImportRaw("{\"saveVersion\":3,\"quizWrongPool\":" +
                              "[\"Soru_1\",\"Soru_1\",\"\",\"   \",\"Soru_2\"]}");

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("quizWrongPool"));

        List<string> havuz = SaveService.Data.quizWrongPool;
        Debug.Log($"{Tag}: onarım sonrası havuz = [{string.Join(", ", havuz)}]");

        Assert.AreEqual(2, havuz.Count, "bozuk havuz onarılmadı");
        Assert.AreEqual("Soru_1", havuz[0]);
        Assert.AreEqual("Soru_2", havuz[1]);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AsiriUzunHavuzKirpilir()
    {
        List<string> uzun = new List<string>();
        for (int i = 0; i < 400; i++) uzun.Add("Soru_" + i);

        SaveService.ImportRaw("{\"saveVersion\":3,\"quizWrongPool\":[\"" +
                              string.Join("\",\"", uzun) + "\"]}");

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("quizWrongPool"));

        int sayi = SaveService.Data.quizWrongPool.Count;
        Debug.Log($"{Tag}: 400 girdilik havuz -> {sayi} girdi");
        Assert.Less(sayi, 400, "akıl dışı uzunluk kırpılmadı");
        yield return null;
    }
}
