using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// EKRANDA GÖSTERİLEN HER KARAKTER ATLASTA OLMALI.
///
/// Neyi koruyor: bir metne yeni bir karakter girdiğinde (ör. `·`, `°`, `₂`)
/// statik atlasta karşılığı yoksa TMP sessizce DİNAMİK FALLBACK'e düşer.
/// Ekranda kutu ya da başka bir fontun harfi çıkar; nadir görülen bir
/// ekranda bu cihaz testinde fark edilmez.
///
/// Daha önce ölçümle yakalananlar:
///   - Orbitron'un TTF'inde `İ` yok (font elendi)
///   - Oxanium'da `₅`–`₉` yok (formüller TMP &lt;sub&gt; ile yazılıyor)
///   - Bilgi kartlarında literal `₂` / `₃` kullanılıyordu; hiçbir atlasta
///     yoktu, yani cihazda kutu çiziliyordu — &lt;sub&gt;'a çevrildi
///
/// Metin kaynakları ve atlas üretimi AYNI listeyi kullanır
/// (<c>NexumTextSources</c>), yoksa test yanlış güven verir.
/// </summary>
public class AtlasCoversAllDisplayedTextTests
{
    private const string Tag = "NEXUM_ATLASCOVER";

    private static readonly string[] Atlaslar =
    {
        "Assets/Fonts/Oxanium-Regular SDF.asset",
        "Assets/Fonts/Oxanium-Medium SDF.asset",
        "Assets/Fonts/Oxanium-SemiBold SDF.asset",
    };

    [UnityTest]
    public IEnumerator EkrandakiHerKarakterAtlastaVar()
    {
#if UNITY_EDITOR
        // Toplama Editor tarafında (NexumTextSources); test derlemesi o
        // derlemeyi referans almadığı için yansımayla çağrılır.
        System.Type kaynak = System.Type.GetType("NexumTextSources, Assembly-CSharp-Editor");
        Assert.IsNotNull(kaynak, "NexumTextSources bulunamadı");

        System.Reflection.MethodInfo topla = kaynak.GetMethod("Topla");
        Assert.IsNotNull(topla, "NexumTextSources.Topla bulunamadı");

        // Play modunda yalnizca YUKLU sahne taranabiliyor; iki sahne de
        // sirayla yuklenip birlesim aliniyor (Editor tarafi ikisini acar).
        SortedSet<char> kume = new SortedSet<char>();
        Dictionary<char, string> nerede = new Dictionary<char, string>();

        foreach (string sahne in new[] { "MainMenu", "Game" })
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sahne);
            yield return null;
            yield return new WaitForSeconds(0.5f);

            object[] args = { null };
            string parca = (string)topla.Invoke(null, args);
            var kaynak2 = (Dictionary<char, string>)args[0];

            foreach (char c in parca)
            {
                if (kume.Add(c)) nerede[c] = kaynak2[c];
            }
        }

        Assert.Greater(kume.Count, 40, "metin kaynaklarından karakter toplanamadı");
        Debug.Log($"{Tag}: {kume.Count} benzersiz karakter toplandı (iki sahne)");

        List<string> eksikler = new List<string>();
        List<string> altSimge = new List<string>();

        foreach (string yol in Atlaslar)
        {
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(yol);
            Assert.IsNotNull(font, "atlas yok: " + yol);

            HashSet<uint> mevcut = new HashSet<uint>(font.characterTable.Select(c => c.unicode));
            string ad = System.IO.Path.GetFileNameWithoutExtension(yol);

            Debug.Log($"{Tag}: {ad} {font.characterTable.Count} karakter, " +
                      $"mod {font.atlasPopulationMode}, atlas " +
                      $"{(font.atlasTextures.Length > 0 ? font.atlasTextures[0].width : 0)}px");

            Assert.AreEqual(AtlasPopulationMode.Static, font.atlasPopulationMode,
                ad + " statik olmalı; dinamik atlas çalışma zamanında büyür ve " +
                "depoda sürekli değişir");

            foreach (char c in kume)
            {
                if (mevcut.Contains(c)) continue;

                string satir = $"{ad}: '{c}' U+{((int)c):X4}  ← {nerede[c]}";
                if (c >= '\u2080' && c <= '\u2089') altSimge.Add(satir);
                else eksikler.Add(satir);
            }
        }

        foreach (string s in altSimge) Debug.Log($"{Tag}_ALTSIMGE: {s}");
        foreach (string s in eksikler) Debug.Log($"{Tag}_EKSIK: {s}");

        Debug.Log($"{Tag}_SONUC: eksik {eksikler.Count}, literal alt simge {altSimge.Count}");

        // Literal alt simge AYRI bir hata: Oxanium'da ₅–₉ zaten yok, yani
        // atlasa eklenerek çözülemez. Çözüm metni <sub> ile yazmak.
        Assert.IsEmpty(altSimge,
            "Literal alt simge karakteri kullanılmış. Oxanium'da ₅–₉ YOK, atlasa " +
            "eklenerek çözülemez; metni TMP <sub> etiketiyle yaz:\n" +
            string.Join("\n", altSimge));

        Assert.IsEmpty(eksikler,
            "Atlasta olmayan karakter var; NEXUM → Font Atlaslarını Üret çalıştırın:\n" +
            string.Join("\n", eksikler));
#else
        Assert.Ignore("Metin kaynakları yalnızca Editor'de taranabiliyor.");
#endif
        yield return null;
    }
}
