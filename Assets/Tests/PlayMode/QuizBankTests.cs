using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Soru bankasının bütünlüğü (1.5).
///
/// Neyi koruyor:
///  - AÇIKLAMASI OLMAYAN SORU. 1.5'in sözü "her soruda açıklama"; anahtar
///    asset adından türetildiği için bir soru yeniden adlandırılırsa ya da
///    yeni soru sözlüğe yazılmadan eklenirse ekranda köşeli parantezli
///    anahtar görünür. Oyuncu bunu quiz'i doğru cevaplayınca görür, yani
///    nadir — cihaz testinde kaçar.
///  - Eksik/bozuk şık, aralık dışı doğru cevap indeksi, boş etiket.
///  - Literal alt simge karakteri (U+2080-2089): fontlarda glif yok.
///  - Yinelenen soru gövdesi.
///
/// Banka listesi ELLE YAZILMAZ: sahnedeki <c>UIManager.allQuizzes</c> okunur,
/// yani kurucunun sahneye gerçekten bağladığı liste ölçülür.
/// </summary>
public class QuizBankTests
{
    private const string Tag = "NEXUM_QUIZBANK_TEST";

    /// <summary>1.5 sonunda bankadaki soru sayısı (16 eski + 20 yeni).</summary>
    private const int BeklenenSoruSayisi = 36;

    private IList banka;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        banka = Reflect.Get(ui, "allQuizzes") as IList;
        Assert.IsNotNull(banka, "UIManager.allQuizzes okunamadı");
    }

    [UnityTest]
    public IEnumerator BankaBeklenenBoyutta()
    {
        Debug.Log($"{Tag}: bankada {banka.Count} soru");
        Assert.AreEqual(BeklenenSoruSayisi, banka.Count,
            "sahnedeki banka boyutu beklenenden farklı (NexumQuizBank.Run koşuldu mu?)");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HerSorununAciklamasiVar()
    {
        List<string> eksikler = new List<string>();
        List<string> tablo = new List<string>();

        foreach (object q in banka)
        {
            Assert.IsNotNull(q, "bankada null giriş var");

            string ad = ((ScriptableObject)q).name;
            string anahtar = Reflect.Get<string>(q, "ExplanationKey");

            if (!Loc.Has(anahtar)) { eksikler.Add($"{ad}: sözlükte yok -> {anahtar}"); continue; }

            string metin = Loc.Get(anahtar);
            if (string.IsNullOrWhiteSpace(metin) || metin.StartsWith("["))
            {
                eksikler.Add($"{ad}: açıklama boş -> {anahtar}");
                continue;
            }

            tablo.Add($"{ad,-9} {metin.Length,3} krk  {anahtar}");
        }

        tablo.Sort();
        foreach (string s in tablo) Debug.Log($"{Tag}_ACIKLAMA: {s}");
        Debug.Log($"{Tag}: açıklaması olan {tablo.Count}/{banka.Count}");

        Assert.IsEmpty(eksikler, "açıklaması olmayan soru: " + string.Join(" | ", eksikler));
        yield return null;
    }

    [UnityTest]
    public IEnumerator SorularVeEtiketlerGecerli()
    {
        List<string> sorunlar = new List<string>();
        List<string> bilesiksiz = new List<string>();
        HashSet<string> govdeler = new HashSet<string>();
        HashSet<string> adlar = new HashSet<string>();
        Dictionary<string, int> zorluklar = new Dictionary<string, int>();
        Dictionary<string, int> konular = new Dictionary<string, int>();

        foreach (object q in banka)
        {
            string ad = ((ScriptableObject)q).name;
            if (!adlar.Add(ad)) sorunlar.Add($"{ad}: aynı ad iki kez");

            string govde = Reflect.Get<string>(q, "questionText");
            string[] siklar = Reflect.Get<string[]>(q, "options");
            int dogru = Reflect.Get<int>(q, "correctAnswerIndex");
            string konu = Reflect.Get<string>(q, "topic");
            int zorluk = Reflect.Get<int>(q, "difficulty");
            string bilesik = Reflect.Get<string>(q, "relatedCompound");

            if (string.IsNullOrWhiteSpace(govde)) sorunlar.Add($"{ad}: soru gövdesi boş");
            else if (!govdeler.Add(govde.Trim())) sorunlar.Add($"{ad}: soru gövdesi yineleniyor");

            if (siklar == null || siklar.Length != 4) sorunlar.Add($"{ad}: şık sayısı 4 değil");
            else
            {
                HashSet<string> sikKume = new HashSet<string>();
                for (int i = 0; i < 4; i++)
                {
                    if (string.IsNullOrWhiteSpace(siklar[i])) sorunlar.Add($"{ad}: {i}. şık boş");
                    else if (!sikKume.Add(siklar[i].Trim())) sorunlar.Add($"{ad}: yinelenen şık \"{siklar[i]}\"");
                    else if (siklar[i] != siklar[i].Trim()) sorunlar.Add($"{ad}: {i}. şıkta baş/son boşluk");
                }
            }

            if (dogru < 0 || dogru > 3) sorunlar.Add($"{ad}: doğru cevap indeksi {dogru}");
            if (string.IsNullOrWhiteSpace(konu)) sorunlar.Add($"{ad}: konu etiketi boş");
            if (zorluk < 1 || zorluk > 3) sorunlar.Add($"{ad}: zorluk {zorluk} (1-3 olmalı)");

            // İlgili bileşik BOŞ OLABİLİR: her soru bir bileşiğe bağlanmıyor
            // (ör. "katalizör nedir" bir kavram sorusu). Boş bırakılanlar
            // loglanır ki gözden kaçarak çoğalmasınlar; konu etiketi ise
            // her soruda zorunlu, Faz 5 süzmesi onun üzerinden yapılacak.
            if (string.IsNullOrWhiteSpace(bilesik)) bilesiksiz.Add(ad);
            else if (bilesik != bilesik.Trim()) sorunlar.Add($"{ad}: ilgili bileşikte baş/son boşluk");

            if (!string.IsNullOrWhiteSpace(konu)) konular[konu] = konular.TryGetValue(konu, out int k) ? k + 1 : 1;
            if (zorluk >= 1 && zorluk <= 3)
            {
                string anahtar = "zorluk " + zorluk;
                zorluklar[anahtar] = zorluklar.TryGetValue(anahtar, out int z) ? z + 1 : 1;
            }

            // Literal alt simge: fontlarda glif yok, <sub> kullanılmalı
            string hepsi = govde + " " + (siklar != null ? string.Join(" ", siklar) : "");
            foreach (char c in hepsi)
            {
                if (c >= '₀' && c <= '₉')
                {
                    sorunlar.Add($"{ad}: literal alt simge '{c}' (<sub> kullanılmalı)");
                    break;
                }
            }
        }

        foreach (KeyValuePair<string, int> p in zorluklar.OrderBy(p => p.Key))
            Debug.Log($"{Tag}_ZORLUK: {p.Key} -> {p.Value} soru");
        foreach (KeyValuePair<string, int> p in konular.OrderBy(p => p.Key))
            Debug.Log($"{Tag}_KONU: {p.Key,-26} -> {p.Value} soru");

        Debug.Log($"{Tag}_BILESIKSIZ: {bilesiksiz.Count} soru " +
                  $"({string.Join(", ", bilesiksiz)})");

        Assert.IsEmpty(sorunlar, "banka kusurları: " + string.Join(" | ", sorunlar));
        yield return null;
    }
}
