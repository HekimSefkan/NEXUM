using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BÖLÜM HARİTASI DÜĞÜMÜNDEKİ YILDIZLAR (Faz 2 / 2.2).
///
/// Her düğümün kendi küçük bileşeni var; <c>LevelMenuManager</c>'a
/// dokunulmadı. Gerekçe: oradaki <c>levelButtons</c> elle bağlanmış tek
/// boyutlu bir dizi, ve yıldızlar bölüm başına ÜÇ görsel demek —
/// Unity iki boyutlu diziyi serileştirmiyor, yani oraya eklemek ya
/// yedi ayrı alan ya da sahte bir sarmalayıcı tip isterdi.
///
/// Bileşen kendini doldurur (<c>OnEnable</c>): harita paneli her
/// açıldığında kayıttan okunur, yani bölüm bitirip menüye dönünce
/// yıldız hemen görünür.
///
/// <b>Haritanın yeniden tasarımı 2.5'in işi.</b> Burada yalnızca
/// yıldız eklendi; düğümlerin yeri, rengi ve kapsül görselleri
/// DEĞİŞMEDİ.
/// </summary>
public class LevelNodeStars : MonoBehaviour
{
    [Tooltip("Bu düğümün bölüm indeksi (0 tabanlı).")]
    public int levelIndex;

    [Tooltip("Üç yuva, soldan sağa (StarRules bit sırası).")]
    public Image[] stars;

    [Tooltip("Kazanılmış yıldız görseli.")]
    public Sprite fullSprite;

    [Tooltip("Kazanılmamış yıldız görseli.")]
    public Sprite emptySprite;

    private void OnEnable()
    {
        Apply();
    }

    /// <summary>Kayıttaki EN İYİ sonucu çizer. Hiç oynanmadıysa üçü de boş.</summary>
    public void Apply()
    {
        if (stars == null) return;

        int mask = StarService.StarsFor(levelIndex);

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;

            bool dolu = StarRules.Has(mask, StarRules.StarAt(i));
            Sprite hedef = dolu ? fullSprite : emptySprite;
            if (hedef != null) stars[i].sprite = hedef;
        }
    }
}
