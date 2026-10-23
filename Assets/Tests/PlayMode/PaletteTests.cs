using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Palet tek kaynak kalsın: <c>NexumPalette</c> (çalışma zamanı) ile
/// <c>NexumTheme.asset</c> (kurucular) ayrışmamalı.
///
/// Tema asset'i Resources'ta olmadığı için kodda ayrı bir sabit tablo var;
/// bu test o tablonun tema ile birebir aynı kaldığını her koşuda doğrular.
/// Tema değişip <c>NexumPalette</c> unutulursa test düşer.
///
/// Oyun kodu Assembly-CSharp'ta olduğu için erişim <see cref="Reflect"/> ile.
/// </summary>
public class PaletteTests
{
    private const string Tag = "NEXUM_PALETTE";

    [UnityTest]
    public IEnumerator PaletKodVeTemaAyrismiyor()
    {
#if UNITY_EDITOR
        string yol = Reflect.Prop<string>("NexumTheme", "AssetPath");
        var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(yol);
        Assert.IsNotNull(theme, "NexumTheme asset'i yok: " + yol);

        object palette = Reflect.Get(theme, "palette");
        Assert.IsNotNull(palette, "tema paleti okunamadı");

        string[] roller = Reflect.Prop<string[]>("NexumPalette", "Roles");
        Assert.IsNotNull(roller);

        List<string> fark = new List<string>();

        foreach (string rol in roller)
        {
            Color kod = Reflect.Call<Color>("NexumPalette", "ByRole", rol);
            Color tema = Reflect.Get<Color>(palette, rol);

            string kodHex = ColorUtility.ToHtmlStringRGB(kod);
            string temaHex = ColorUtility.ToHtmlStringRGB(tema);

            Debug.Log($"{Tag}: {rol,-16} kod #{kodHex}  tema #{temaHex}");
            if (kodHex != temaHex) fark.Add($"{rol}: kod #{kodHex} != tema #{temaHex}");
        }

        Debug.Log($"{Tag}_SONUC: {roller.Length} rol karşılaştırıldı, fark {fark.Count}");
        Assert.IsEmpty(fark, string.Join(" | ", fark));
#else
        Assert.Ignore("Tema asset'i yalnızca Editor'de okunabiliyor.");
#endif
        yield return null;
    }
}
