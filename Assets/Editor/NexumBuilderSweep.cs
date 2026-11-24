using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// KURUCU SAPMASI TARAYICISI (1.6).
///
/// Soru şu: bir kurucuyu bugün çalıştırsam, depodaki sahneyi aynen mi
/// üretirdi? Cevap "hayır" ise sahne iki kaynaklıdır — kurucu bir şey,
/// sahnenin kayıtlı hâli başka bir şey söylüyordur ve kurucuyu çağıran
/// herhangi bir iş, farkı yanında getirir.
///
/// 1.5'te bu yaşandı: `RecipeBookBuilder` tek koşuda 25 ilgisiz objeyi
/// (tarif defteri satırlarının `LockIcon` sprite'ı ve rengi, `Backdrop`,
/// `BackButton`) değiştirdi. Sapma o turun işi değildi, ama o turun PR'ına
/// girecekti.
///
/// NASIL ÖLÇER: her kurucunun <c>Build(bool dryRun)</c> metodunu
/// <b>dry-run</b> ile çağırır — hiçbir dosya yazılmaz. <c>UIBuilder.End</c>
/// her koşuyu <c>UIBuilder.SweepLog</c>'a yazar; oluşturulan + güncellenen
/// sayısı sıfır değilse o kurucu sapmıştır.
///
/// "İkinci koşu boş" YETMEZ: idempotans, kurucunun kendi çıktısını tekrar
/// üretmesidir. Buradaki soru daha serttir — <b>ilk</b> koşu da boş olmalı,
/// yani kurucu depodaki hâli üretmeli.
///
/// Menüden: NEXUM -> Kurucu Sapmasını Tara
/// Batch mode: -executeMethod NexumBuilderSweep.Run
/// NexumValidation.Run da bunu çağırır; sapma FAIL'dir.
/// </summary>
public static class NexumBuilderSweep
{
    public const string Tag = "NEXUM_SWEEP";

    /// <summary>
    /// Taramanın dışında tutulanlar. Boş olmalı; bir kurucu buraya
    /// yazılacaksa gerekçesi yanına ve docs/NEXT_3B.md'ye yazılır.
    /// </summary>
    private static readonly string[] Muaf = new string[0];

    [MenuItem("NEXUM/Kurucu Sapmasını Tara")]
    public static void Run()
    {
        List<string> sapanlar = Tara(out int kurucuSayisi, out List<string> dokum);

        foreach (string s in dokum) Debug.Log($"{Tag}_SATIR: {s}");
        Debug.Log($"{Tag}: {kurucuSayisi} kurucu tarandı, sapan {sapanlar.Count}");

        foreach (string s in sapanlar) Debug.LogError($"{Tag}_SAPMA: {s}");
        Debug.Log(sapanlar.Count == 0 ? $"{Tag}: OK" : $"{Tag}: FAIL");
    }

    /// <summary>
    /// Bütün kurucuları dry-run koşturur. Dönen liste sapan kurucuların
    /// açıklamalarıdır; boşsa her kurucu depodaki sahneyi aynen üretiyor.
    /// </summary>
    public static List<string> Tara(out int kurucuSayisi, out List<string> dokum)
    {
        List<MethodInfo> kurucular = Kurucular();
        kurucuSayisi = kurucular.Count;

        List<string> sapanlar = new List<string>();
        dokum = new List<string>();

        bool oncekiDurum = UIBuilder.SweepActive;
        UIBuilder.SweepActive = true;
        UIBuilder.SweepLog.Clear();

        try
        {
            foreach (MethodInfo m in kurucular)
            {
                int oncekiSatir = UIBuilder.SweepLog.Count;
                try
                {
                    m.Invoke(null, new object[] { true });   // dryRun
                }
                catch (Exception e)
                {
                    // Yığın izi OLMADAN bu satır işe yaramaz: "patladı" bilgisi
                    // hangi çağrıda patladığını söylemiyor (1.6a'da ölçüldü).
                    Exception ic = e.InnerException ?? e;
                    string iz = ic.StackTrace != null
                        ? string.Join(" << ", ic.StackTrace
                            .Split('\n')
                            .Select(x => x.Trim())
                            .Where(x => x.Length > 0)
                            .Take(4))
                        : "(yığın izi yok)";

                    sapanlar.Add($"{m.DeclaringType.Name}: koşarken patladı -> " +
                                 $"{ic.GetType().Name}: {ic.Message} || {iz}");
                    continue;
                }

                // Bir kurucu birden fazla sahne kurabiliyor (her sahne bir
                // UIBuilder örneği, yani SweepLog'da ayrı satır).
                for (int i = oncekiSatir; i < UIBuilder.SweepLog.Count; i++)
                {
                    UIBuilder.SweepEntry e = UIBuilder.SweepLog[i];
                    string sahne = System.IO.Path.GetFileNameWithoutExtension(e.scene);

                    dokum.Add($"{e.builder,-26} {sahne,-9} oluşturulan={e.created.Count} " +
                              $"güncellenen={e.updated.Count} hata={e.errors.Count}");

                    if (e.errors.Count > 0)
                    {
                        sapanlar.Add($"{e.builder} ({sahne}): {e.errors.Count} hata -> " +
                                     string.Join(" | ", e.errors.Take(3)));
                    }

                    if (e.Changes == 0) continue;

                    IEnumerable<string> ornekler = e.created.Select(x => "YENİ " + x)
                        .Concat(e.updated.Select(x => "DEĞİŞTİRİR " + x))
                        .Take(6);

                    sapanlar.Add($"{e.builder} ({sahne}): kayıtlı sahneyi aynen üretmiyor — " +
                                 $"{e.created.Count} yeni, {e.updated.Count} değişiklik; " +
                                 string.Join(" | ", ornekler));
                }
            }
        }
        finally
        {
            UIBuilder.SweepActive = oncekiDurum;
            UIBuilder.SweepLog.Clear();
        }

        return sapanlar;
    }

    /// <summary>
    /// <c>Assets/Editor/UIBuilder</c> altındaki her kurucunun
    /// <c>Build(bool)</c> metodu. Liste ELLE YAZILMAZ: yeni bir kurucu
    /// eklendiğinde taramaya kendiliğinden girsin.
    /// </summary>
    private static List<MethodInfo> Kurucular()
    {
        List<MethodInfo> sonuc = new List<MethodInfo>();

        foreach (Type t in typeof(UIBuilder).Assembly.GetTypes())
        {
            if (!t.IsClass || !t.IsAbstract || !t.IsSealed) continue;   // static class
            if (!t.Name.EndsWith("Builder", StringComparison.Ordinal)) continue;
            if (Muaf.Contains(t.Name)) continue;

            MethodInfo m = t.GetMethod("Build",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(bool) }, null);
            if (m == null) continue;

            sonuc.Add(m);
        }

        return sonuc.OrderBy(m => m.DeclaringType.Name, StringComparer.Ordinal).ToList();
    }
}
