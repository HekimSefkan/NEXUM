using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Sözlük katmanının üç davranışı:
//  1) Sözlük yüklendi ve koddan istenen anahtarların hepsi var
//  2) Dil değişince sahnedeki metin ANINDA güncelleniyor
//  3) Eksik anahtar çökmüyor, anahtar adı görünür şekilde dönüyor
public class LocalizationTests
{
    private const string Tag = "NEXUM_LOC_TEST";

    private Loc.Language onceki;

    [SetUp]
    public void Yedekle()
    {
        onceki = Loc.Current;
    }

    [TearDown]
    public void GeriYukle()
    {
        Loc.SetLanguage(onceki);
    }

    [UnityTest]
    public IEnumerator SozlukYuklendiVeKodAnahtarlariTam()
    {
        Debug.Log($"{Tag}_SOZLUK: yuklu={Loc.IsLoaded} anahtar={Loc.KeyCount} dil={Loc.Current}");
        Assert.IsTrue(Loc.IsLoaded, "sözlük yüklenmeli");
        Assert.Greater(Loc.KeyCount, 0, "sözlük boş olmamalı");

        var eksik = CodeStrings.All.Keys.Where(k => !Loc.Has(k)).ToList();
        Debug.Log($"{Tag}_SOZLUK: kod anahtari {CodeStrings.All.Count}, eksik {eksik.Count}");
        Assert.IsEmpty(eksik, "kodun istediği anahtar sözlükte yok: " + string.Join(", ", eksik));
        yield return null;
    }

    [UnityTest]
    public IEnumerator DilDegisinceMetinAnindaDegisir()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        LocalizedText ornek = Object.FindObjectsOfType<LocalizedText>(true)
            .FirstOrDefault(l => !string.IsNullOrEmpty(l.key) && l.GetComponent<TMP_Text>() != null);
        Assert.IsNotNull(ornek, "sahnede bağlı metin yok");

        TMP_Text tmp = ornek.GetComponent<TMP_Text>();

        Loc.SetLanguage(Loc.Language.Turkish);
        ornek.Apply();
        string trMetin = tmp.text;

        Loc.SetLanguage(Loc.Language.English);
        // Olay tabanlı: SetLanguage LanguageChanged'i tetikler, bileşen kendini yazar.
        string enMetin = tmp.text;

        Debug.Log($"{Tag}_DIL: anahtar={ornek.key}");
        Debug.Log($"{Tag}_DIL: TR=\"{trMetin}\"");
        Debug.Log($"{Tag}_DIL: EN=\"{enMetin}\" (cogu anahtar Faz 6'ya kadar TR metnini tasiyor)");

        Assert.AreEqual(Loc.Get(ornek.key), enMetin, "dil değişince metin anında güncellenmeli");

        Loc.SetLanguage(Loc.Language.Turkish);
        Debug.Log($"{Tag}_DIL: geri donuste TR=\"{tmp.text}\"");
        Assert.AreEqual(trMetin, tmp.text, "dil geri alınınca eski metne dönmeli");

        // GERÇEKTEN çevrilmiş tek anahtarla uçtan uca kanıt: metin değişmeli.
        // (Faz 6'da çeviriler bitince bu kontrol tüm anahtarlara genişleyebilir.)
        GameObject sonda = new GameObject("LocProbe", typeof(TextMeshPro), typeof(LocalizedText));
        LocalizedText probe = sonda.GetComponent<LocalizedText>();
        probe.key = CodeStrings.Combo1;
        probe.Apply();

        Loc.SetLanguage(Loc.Language.Turkish);
        string probeTr = sonda.GetComponent<TMP_Text>().text;
        Loc.SetLanguage(Loc.Language.English);
        string probeEn = sonda.GetComponent<TMP_Text>().text;

        Debug.Log($"{Tag}_DIL_KANIT: {CodeStrings.Combo1}  TR=\"{probeTr}\"  EN=\"{probeEn}\"");
        Object.DestroyImmediate(sonda);

        Assert.AreNotEqual(probeTr, probeEn, "çevrilmiş anahtar dil değişince farklı metin vermeli");
        Assert.AreEqual("BAŞARILI SENTEZ!", probeTr);
        Assert.AreEqual("SUCCESSFUL SYNTHESIS!", probeEn);
    }

    [UnityTest]
    public IEnumerator EksikAnahtarCokertmez()
    {
        const string yokAnahtar = "test.bu.anahtar.yok";
        Assert.IsFalse(Loc.Has(yokAnahtar), "test anahtarı gerçekten olmamalı");

        LogAssert.ignoreFailingMessages = true;
        string sonuc = Loc.Get(yokAnahtar);
        string bicimli = Loc.Format(yokAnahtar, 1, 2);
        LogAssert.ignoreFailingMessages = false;

        Debug.Log($"{Tag}_EKSIK: Get -> \"{sonuc}\"  Format -> \"{bicimli}\"");

        Assert.AreEqual("[" + yokAnahtar + "]", sonuc, "anahtar adı görünür şekilde dönmeli");
        Assert.IsTrue(bicimli.Contains(yokAnahtar), "biçimli çağrı da anahtarı göstermeli");

        // Boş anahtar da çökmemeli
        Assert.AreEqual("", Loc.Get(null));
        Assert.AreEqual("", Loc.Get(""));
        yield return null;
    }
}
