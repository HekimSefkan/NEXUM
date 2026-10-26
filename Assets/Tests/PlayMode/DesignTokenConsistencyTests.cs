using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// BELGE ↔ TABLO TUTARLILIĞI (2d-3a düzeltmesi, madde 4.6).
///
/// <c>docs/DESIGN_TOKENS.md</c> bir renk kararı yazıyorsa, uygulama tablosu
/// (<c>NexumColorTokens.Tablo</c>) o kararı İÇERMEK ZORUNDA.
///
/// NEDEN: taş harf renkleri tam olarak bu boşluktan kaçtı. Belgenin 145-148.
/// satırları `#00FF00 → success`, `#C0C0C0 → textPrimary`,
/// `#60E8F5 → primary/secondary` eşlemelerini zaten yazmıştı; eşleme tablosu
/// bu üçünü hiç içermiyordu. 26 taş prefab'ı geçiş öncesinin renkleriyle
/// kaldı ve hiçbir test fark etmedi — çünkü belgeyi okuyan bir test yoktu.
///
/// Belgedeki tablo biçimi:
///   | `#00FF00` (7), `#C4FFB9` (5) | `success` | açıklama |
/// Satırdaki her hex, ikinci sütundaki role eşlenmiş sayılır. Birden fazla
/// rol yazılmışsa (`primary` / `secondary`) herhangi biri kabul edilir.
/// </summary>
public class DesignTokenConsistencyTests
{
    private const string Tag = "NEXUM_TOKENBELGE";
    private const string DocPath = "docs/DESIGN_TOKENS.md";

    /// <summary>
    /// Belgenin eşleme bölümü. Başlıktan sonraki ilk tablo okunur.
    /// </summary>
    private const string SectionMarker = "aykırı renk";

    [Test]
    public void BelgedekiEslemelerTablodaVar()
    {
        string tam = Path.Combine(Directory.GetCurrentDirectory(), DocPath);
        Assert.IsTrue(File.Exists(tam), $"{DocPath} yok");

        string[] satirlar = File.ReadAllLines(tam);
        Dictionary<string, List<string>> belge = new Dictionary<string, List<string>>();

        foreach (string satir in satirlar)
        {
            if (!satir.StartsWith("|")) continue;

            string[] hucre = satir.Split('|');
            if (hucre.Length < 4) continue;

            List<string> hexler = Regex.Matches(hucre[1], @"#([0-9A-Fa-f]{6})")
                .Cast<Match>().Select(m => m.Groups[1].Value.ToUpperInvariant()).ToList();
            if (hexler.Count == 0) continue;

            List<string> roller = Regex.Matches(hucre[2], @"`([a-zA-Z]+)`")
                .Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            roller = roller.Where(r => Roller().Contains(r)).ToList();
            if (roller.Count == 0) continue;

            foreach (string h in hexler)
            {
                if (!belge.ContainsKey(h)) belge[h] = roller;
            }
        }

        Assert.Greater(belge.Count, 5,
            $"{DocPath} içinde eşleme tablosu bulunamadı (biçim değişmiş olabilir)");

        Dictionary<string, string> tablo = Reflect.ColorTokenTable();
        Assert.IsNotNull(tablo, "NexumColorTokens.Tablo okunamadı");

        List<string> eksik = new List<string>();
        List<string> celiski = new List<string>();

        foreach (var kv in belge)
        {
            // Paletin kendi değerleri zaten doğru renkte: eşleme gerekmez.
            if (PaletteHexleri().Contains(kv.Key)) continue;

            string rol;
            if (!tablo.TryGetValue(kv.Key, out rol))
            {
                eksik.Add($"#{kv.Key} -> {string.Join("/", kv.Value)} (belgede var, tabloda yok)");
                continue;
            }

            if (!kv.Value.Contains(rol))
            {
                celiski.Add($"#{kv.Key}: belge {string.Join("/", kv.Value)}, tablo {rol}");
            }
        }

        Debug.Log($"{Tag}_SONUC: belgede {belge.Count} eşleme, tabloda {tablo.Count}, " +
                  $"eksik {eksik.Count}, çelişki {celiski.Count}");
        foreach (string e in eksik) Debug.Log($"{Tag}_EKSIK: {e}");
        foreach (string c in celiski) Debug.Log($"{Tag}_CELISKI: {c}");

        Assert.IsEmpty(eksik, string.Join(" | ", eksik));
        Assert.IsEmpty(celiski, string.Join(" | ", celiski));
    }

    /// <summary>Palet Assembly-CSharp'ta; test asmdef'i onu referans almaz.</summary>
    private static string[] Roller()
    {
        return Reflect.Prop<string[]>("NexumPalette", "Roles");
    }

    private static HashSet<string> PaletteHexleri()
    {
        HashSet<string> h = new HashSet<string>();
        foreach (string r in Roller())
        {
            Color32 c = Reflect.Call<Color>("NexumPalette", "ByRole", r);
            h.Add($"{c.r:X2}{c.g:X2}{c.b:X2}");
        }
        return h;
    }
}
