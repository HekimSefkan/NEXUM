using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Taş seviyesi: türetimin doğruluğu ve DAVRANIŞIN DEĞİŞMEDİĞİ.
///
/// Seviye sistemi saf temizliktir: "bu taş element mi bileşik mi" sorusunu
/// birkaç yerde elle yapılan kontrollerden alıp tek kaynağa bağladı. Bu
/// yüzden testlerin yarısı türetimi, yarısı da ESKİ KURALLA AYNI SONUCU
/// verdiğini ölçer.
/// </summary>
public class TileLevelTests
{
    private const string Tag = "NEXUM_TILELEVEL_TEST";
    private const string Levels = "TileLevels";
    private const string Catalog = "RecipeCatalog";

    private static object Katalog()
    {
        object k = Reflect.Call(Catalog, "Load");
        Assert.IsNotNull(k, "RecipeCatalog yok (NEXUM -> Tarif Kataloğunu Kur)");
        return k;
    }

    private static List<object> Girdiler()
    {
        return ((System.Collections.IList)Reflect.Get(Katalog(), "entries")).Cast<object>().ToList();
    }

    private static List<object> Taslar()
    {
        return ((System.Collections.IList)Reflect.Get(Katalog(), "tiles")).Cast<object>().ToList();
    }

    private static int Derinlik(string ad)
    {
        return Reflect.Call<int>(Levels, "Depth", ad);
    }

    private static int Seviye(string ad)
    {
        return Reflect.Call<int>(Levels, "Level", ad);
    }

    // ------------------------------------------------------------- türetim

    // Her taşın derinliği ve seviyesi; tablo rapora da çıkar.
    [UnityTest]
    public IEnumerator DerinlikVeSeviyeTablosu()
    {
        Reflect.Call(Levels, "Clear");

        List<object> taslar = Taslar();
        Assert.Greater(taslar.Count, 0, "katalogda taş listesi yok (NEXUM -> Tarif Kataloğunu Kur)");

        List<string> hatalar = new List<string>();
        Dictionary<int, int> seviyeSayisi = new Dictionary<int, int>();

        foreach (object t in taslar.OrderBy(x => Reflect.Get<string>(x, "name")))
        {
            string ad = Reflect.Get<string>(t, "name");
            int d = Derinlik(ad);
            int s = Seviye(ad);

            Debug.Log($"{Tag}_TABLO: {ad,-14} derinlik={d,2} seviye={s}");

            if (d < 0) hatalar.Add($"{ad}: derinlik türetilemedi");
            seviyeSayisi[s] = seviyeSayisi.TryGetValue(s, out int n) ? n + 1 : 1;
        }

        foreach (var kv in seviyeSayisi.OrderBy(k => k.Key))
        {
            Debug.Log($"{Tag}_DAGILIM: seviye {kv.Key} -> {kv.Value} taş");
        }

        // Beklenen dağılım: alt seviyeler kalabalık, üst seviyeler nadir.
        int[] beklenenDagilim = { 8, 10, 4, 4 };
        for (int i = 0; i < beklenenDagilim.Length; i++)
        {
            int sayi = seviyeSayisi.TryGetValue(i + 1, out int n) ? n : 0;
            if (sayi != beklenenDagilim[i])
            {
                hatalar.Add($"seviye {i + 1}: {sayi} taş (beklenen {beklenenDagilim[i]})");
            }
        }

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
        yield return null;
    }

    // Türetim kuralı: ürün = 1 + max(girdiler), birden fazla tarifte EN KISA.
    [UnityTest]
    public IEnumerator TuretimKuraliTutarli()
    {
        Reflect.Call(Levels, "Clear");

        List<string> hatalar = new List<string>();

        // Her ürün için en kısa yolu bağımsız hesapla ve karşılaştır
        Dictionary<string, int> beklenen = new Dictionary<string, int>();
        foreach (object t in Taslar())
        {
            string ad = Reflect.Get<string>(t, "name");
            beklenen[ad] = Girdiler().Any(e => Reflect.Get<string>(e, "productName") == ad)
                ? int.MaxValue : 0;
        }

        for (int tur = 0; tur < 64; tur++)
        {
            bool degisti = false;
            foreach (object e in Girdiler())
            {
                string a = Reflect.Get<string>(e, "inputAName");
                string b = Reflect.Get<string>(e, "inputBName");
                string c = Reflect.Get<string>(e, "productName");
                if (!beklenen.ContainsKey(a) || !beklenen.ContainsKey(b)) continue;
                if (beklenen[a] == int.MaxValue || beklenen[b] == int.MaxValue) continue;

                int aday = 1 + Mathf.Max(beklenen[a], beklenen[b]);
                if (aday < beklenen[c]) { beklenen[c] = aday; degisti = true; }
            }
            if (!degisti) break;
        }

        foreach (var kv in beklenen)
        {
            int d = Derinlik(kv.Key);
            if (kv.Value == int.MaxValue) continue;
            if (d != kv.Value) hatalar.Add($"{kv.Key}: türetim {d}, bağımsız hesap {kv.Value}");
        }

        Debug.Log($"{Tag}_TURETIM: {beklenen.Count} taş karşılaştırıldı, sorun {hatalar.Count}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
        yield return null;
    }

    // Eşik tablosu parametre: seviye sayısına sabitlenmemiş.
    [UnityTest]
    public IEnumerator EsikTablosuParametre()
    {
        int[] esikler = Reflect.Prop<int[]>(Levels, "LevelThresholds");
        Debug.Log($"{Tag}_ESIK: [{string.Join(", ", esikler)}]");

        Assert.AreEqual(1, Reflect.Call<int>(Levels, "LevelForDepth", 0), "derinlik 0 -> seviye 1");
        Assert.AreEqual(2, Reflect.Call<int>(Levels, "LevelForDepth", 1), "derinlik 1 -> seviye 2");
        Assert.AreEqual(3, Reflect.Call<int>(Levels, "LevelForDepth", 2), "derinlik 2 -> seviye 3");
        Assert.AreEqual(4, Reflect.Call<int>(Levels, "LevelForDepth", 3), "derinlik 3 -> seviye 4");
        Assert.AreEqual(4, Reflect.Call<int>(Levels, "LevelForDepth", 9), "derinlik 9 -> seviye 4");
        Assert.AreEqual(esikler.Length, Reflect.Call<int>(Levels, "LevelForDepth", 99),
            "en derin taş en üst seviyede olmalı");
        yield return null;
    }

    // --------------------------------------------- davranış değişmedi

    // ESKİ KURAL: ElementData.recipe boşsa temel element.
    // YENİ KURAL: TileLevels.IsBasic. İkisi 26 taşın hepsinde AYNI sonucu vermeli.
    [UnityTest]
    public IEnumerator TemelElementAyrimiEskisiyleAyni()
    {
        Reflect.Call(Levels, "Clear");

        List<string> fark = new List<string>();
        int temel = 0, bilesik = 0;

        foreach (object t in Taslar())
        {
            string ad = Reflect.Get<string>(t, "name");
            var data = Reflect.Get(t, "data") as ScriptableObject;
            if (data == null) continue;

            string recipe = Reflect.Get<string>(data, "recipe");
            bool eski = string.IsNullOrWhiteSpace(recipe);
            bool yeni = Reflect.Call<bool>(Levels, "IsBasic", ad);

            if (eski) temel++; else bilesik++;
            if (eski != yeni) fark.Add($"{ad}: eski={eski} yeni={yeni} (recipe=\"{recipe}\")");
        }

        Debug.Log($"{Tag}_AYRIM: temel {temel}, bileşik {bilesik}, fark {fark.Count}");
        foreach (string f in fark) Debug.Log($"{Tag}_FARK: {f}");
        Assert.IsEmpty(fark, string.Join(" | ", fark));
        yield return null;
    }

    // GridManager.basicElements ile türetilen derinlik-0 kümesi aynı olmalı.
    [UnityTest]
    public IEnumerator SahnedekiTemelElementlerleAyni()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        var basics = Reflect.Get<GameObject[]>(grid, "basicElements");
        HashSet<string> sahne = new HashSet<string>(basics.Where(b => b != null).Select(b => b.name));

        Reflect.Call(Levels, "Clear");
        HashSet<string> turetilen = new HashSet<string>(
            Taslar().Select(t => Reflect.Get<string>(t, "name"))
                    .Where(ad => Reflect.Call<bool>(Levels, "IsBasic", ad)));

        Debug.Log($"{Tag}_SAHNE: basicElements {sahne.Count} [{string.Join(", ", sahne.OrderBy(x => x))}]");
        Debug.Log($"{Tag}_SAHNE: derinlik0  {turetilen.Count} [{string.Join(", ", turetilen.OrderBy(x => x))}]");

        var eksik = sahne.Except(turetilen).ToList();
        var fazla = turetilen.Except(sahne).ToList();
        Assert.IsEmpty(eksik, "sahnede temel ama türetimde değil: " + string.Join(", ", eksik));
        Assert.IsEmpty(fazla, "türetimde temel ama sahnede değil: " + string.Join(", ", fazla));
    }

    // Geriye dönük doldurma tablosu DEĞİŞMEMELİ (2d-1 öncesi ölçülen değerler).
    [UnityTest]
    public IEnumerator DoldurmaTablosuDegismedi()
    {
        Reflect.Call(Levels, "Clear");

        int[] beklenen = { 0, 2, 2, 3, 7, 8, 9, 10 };   // açılan bölüm 0..7
        List<string> hatalar = new List<string>();

        for (int acilan = 0; acilan <= 7; acilan++)
        {
            var sonuc = Reflect.Call<List<string>>("RecipeBook", "ComputeBackfill", acilan);
            Debug.Log($"{Tag}_DOLDURMA: bolum {acilan} -> {sonuc.Count} (beklenen {beklenen[acilan]})");
            if (sonuc.Count != beklenen[acilan])
            {
                hatalar.Add($"bolum {acilan}: {sonuc.Count} != {beklenen[acilan]}");
            }
        }

        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
        yield return null;
    }
}
