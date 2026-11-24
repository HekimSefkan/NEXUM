using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// TEST KAPILARI YAYINA GİRMEMELİ (1.7).
///
/// İki kapı var ve ikisi de testlerin işini kolaylaştırmak için açıldı:
///   <c>SaveService.ExportRaw / ImportRaw</c> — kaydı ham JSON olarak
///   okuyup yazar; <c>ResultShare.ForceFailureForTests</c> — paylaşımı
///   zorla düşürür.
///
/// Oyuncunun elindeki pakette bunlar BULUNMAMALI: kaydı ham hâliyle
/// yazabilen bir giriş noktası, testin kolaylığı karşılığında yayına
/// eklenmiş bir saldırı yüzeyidir.
///
/// <b>Nasıl kanıtlanıyor:</b> APK almadan. Unity'nin derleme hattı, PLAYER
/// derlemelerinin tanımlı sembollerini veriyor
/// (<c>CompilationPipeline.GetAssemblies(AssembliesType.Player)</c>).
/// Kapılar <c>#if UNITY_EDITOR || UNITY_INCLUDE_TESTS</c> içinde; bu iki
/// sembol player derlemesinde tanımlı DEĞİLSE kapılar derlenmez. Kontrol
/// tam olarak bunu ölçüyor — ayrıca kapıların hâlâ bir <c>#if</c> içinde
/// durduğunu kaynaktan doğruluyor (biri korumayı kaldırırsa yakalanır).
///
/// Gerçek imzalı paket alınamıyor (şifre depoda değil, bilinçli kural), o
/// yüzden uçtan uca APK kanıtı yerine derleme sembolü kanıtı var.
///
/// <c>NexumValidation.Run</c> bunu çağırır; sorun FAIL'dir.
/// </summary>
public static class NexumTestGates
{
    public const string Tag = "NEXUM_TESTKAPISI";

    /// <summary>
    /// Kapıyı açan sembol. Player derlemesinde olmamalı.
    ///
    /// <c>UNITY_INCLUDE_TESTS</c> BİLEREK listede değil: bu projenin PLAYER
    /// derlemelerinde de tanımlı (ilk ölçümde çıktı). Onu kapı sembolü
    /// saymak, hiçbir şeyi kapatmayan bir koruma görüntüsü verirdi.
    /// </summary>
    private static readonly string[] KapiSembolleri = { "UNITY_EDITOR" };

    private struct Kapi
    {
        public string dosya;     // kaynak dosya (kapının bulunduğu)
        public string[] uyeler;  // dosyada #if içinde durması gereken üyeler

        public Kapi(string dosya, params string[] uyeler)
        {
            this.dosya = dosya;
            this.uyeler = uyeler;
        }
    }

    private static readonly Kapi[] Kapilar =
    {
        new Kapi("Assets/Scripts/Save/SaveService.cs", "ExportRaw", "ImportRaw"),
        new Kapi("Assets/Scripts/UI/ResultShare.cs", "ForceFailureForTests"),
    };

    [MenuItem("NEXUM/Test Kapılarını Denetle")]
    public static void Run()
    {
        List<string> sorunlar = Denetle(out List<string> dokum);

        foreach (string s in dokum) Debug.Log($"{Tag}_SATIR: {s}");
        foreach (string s in sorunlar) Debug.LogError($"{Tag}_SORUN: {s}");
        Debug.Log(sorunlar.Count == 0 ? $"{Tag}: OK" : $"{Tag}: FAIL");
    }

    /// <summary>Sorun listesi; boşsa kapılar yayına girmiyor.</summary>
    public static List<string> Denetle(out List<string> dokum)
    {
        List<string> sorunlar = new List<string>();
        dokum = new List<string>();

        Assembly[] player = CompilationPipeline.GetAssemblies(AssembliesType.Player);
        if (player == null || player.Length == 0)
        {
            sorunlar.Add("player derlemeleri okunamadı");
            return sorunlar;
        }

        foreach (Kapi kapi in Kapilar)
        {
            string tamYol = Path.GetFullPath(kapi.dosya).Replace('\\', '/');

            Assembly sahip = player.FirstOrDefault(a => a.sourceFiles != null &&
                a.sourceFiles.Any(f => Path.GetFullPath(f).Replace('\\', '/') == tamYol));

            if (sahip == null)
            {
                // Dosya player derlemesine HİÇ girmiyorsa kapı zaten yayında
                // değildir; ama bu beklenmedik bir durum, sessiz geçilmez.
                dokum.Add($"{kapi.dosya}: player derlemesinde yok (kapı yayına giremez)");
                continue;
            }

            string[] tanimli = sahip.defines ?? new string[0];
            string[] acikOlanlar = KapiSembolleri.Where(tanimli.Contains).ToArray();

            dokum.Add($"{kapi.dosya} -> {sahip.name}: kapı sembolü " +
                      (acikOlanlar.Length == 0 ? "YOK (iyi)" : string.Join(", ", acikOlanlar)));

            if (acikOlanlar.Length > 0)
            {
                sorunlar.Add($"{sahip.name} player derlemesinde {string.Join(", ", acikOlanlar)} " +
                             "tanımlı; test kapısı yayına girer");
            }

            // Kapı hâlâ bir #if içinde mi? Koruma kaldırılırsa semboller
            // tanımsız olsa bile üyeler yayına girer.
            KaynagiDenetle(kapi, sorunlar, dokum);
        }

        return sorunlar;
    }

    /// <summary>Üyenin önünde kapatılmamış bir <c>#if</c> var mı?</summary>
    private static void KaynagiDenetle(Kapi kapi, List<string> sorunlar, List<string> dokum)
    {
        if (!File.Exists(kapi.dosya))
        {
            sorunlar.Add($"kaynak yok: {kapi.dosya}");
            return;
        }

        string[] satirlar = File.ReadAllLines(kapi.dosya);

        foreach (string uye in kapi.uyeler)
        {
            int satirNo = -1;
            for (int i = 0; i < satirlar.Length; i++)
            {
                string t = satirlar[i].TrimStart();
                if (!t.StartsWith("public static") && !t.StartsWith("private const")) continue;
                if (!satirlar[i].Contains(uye)) continue;
                satirNo = i;
                break;
            }

            if (satirNo < 0)
            {
                sorunlar.Add($"{kapi.dosya}: {uye} bulunamadı (adı mı değişti?)");
                continue;
            }

            // Satırın üstünde açık kalmış bir #if var mı?
            int derinlik = 0;
            bool korumali = false;
            for (int i = 0; i < satirNo; i++)
            {
                string t = satirlar[i].TrimStart();
                if (t.StartsWith("#if")) { derinlik++; korumali = t.Contains("UNITY_EDITOR"); }
                else if (t.StartsWith("#endif")) { derinlik--; if (derinlik <= 0) korumali = false; }
            }

            dokum.Add($"{kapi.dosya}: {uye} (satır {satirNo + 1}) " +
                      (derinlik > 0 && korumali ? "#if korumasında" : "KORUMASIZ"));

            if (derinlik <= 0 || !korumali)
            {
                sorunlar.Add($"{kapi.dosya}: {uye} bir #if UNITY_EDITOR bloğu " +
                             "içinde değil; yayına girer");
            }
        }
    }
}
