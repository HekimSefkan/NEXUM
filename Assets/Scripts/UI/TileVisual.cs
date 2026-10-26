using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Taşın görsel durumları. Seviye sprite'ları prefab'a bağlıdır; hangi
/// seviyenin bağlanacağına <see cref="TileLevels"/> karar verir (tarif
/// grafiğinden türetilir, prefab'da elle seviye alanı YOKTUR).
///
/// Durumlar yalnızca SPRITE değiştirir; animasyon süreleri, doğma, birleşme
/// ve puanlama bu sınıftan etkilenmez.
/// </summary>
[RequireComponent(typeof(Image))]
public class TileVisual : MonoBehaviour
{
    [Tooltip("Türetilen seviye (NexumTileVisuals yazar; elle doldurulmaz).")]
    public int level = 1;

    public Sprite normal;
    public Sprite selected;
    public Sprite merging;
    public Sprite newborn;
    public Sprite discovered;

    private Image image;
    private float normaleDonusZamani = -1f;
    private bool seciliKilitli;          // ipucu vurgusu: süreyle değil, açma/kapama ile

    private Image Target
    {
        get
        {
            if (image == null) image = GetComponent<Image>();
            return image;
        }
    }

    private void OnEnable()
    {
        normaleDonusZamani = -1f;
        seciliKilitli = false;
        Apply(normal);
    }

    private void Update()
    {
        if (seciliKilitli) return;
        if (normaleDonusZamani < 0f) return;
        if (Time.unscaledTime < normaleDonusZamani) return;

        normaleDonusZamani = -1f;
        Apply(normal);
    }

    /// <summary>
    /// Yeni doğan taş kendini duyurur: parlak sprite gösterilir ve
    /// <paramref name="sure"/> dolunca normale döner.
    ///
    /// Süre ÇAĞIRANDAN gelir (GridManager.SpawnDelay); doğum animasyonunun
    /// süresini aşmaz, yani zamanlama değişmez.
    /// </summary>
    public void PlayNewborn(float sure)
    {
        Zamanli(newborn, sure);
    }

    /// <summary>
    /// Birleşmeden doğan taş: <paramref name="sure"/> boyunca birleşme
    /// sprite'ı. Süre ÇAĞIRANDAN gelir (GridManager'ın kendi pop tween
    /// süresi); yeni bir zamanlama sabiti girmez.
    /// </summary>
    public void PlayMerging(float sure)
    {
        Zamanli(merging, sure);
    }

    /// <summary>İlk kez keşfedilen tarif: keşif sprite'ı.</summary>
    public void PlayDiscovered(float sure)
    {
        Zamanli(discovered, sure);
    }

    /// <summary>
    /// İpucu vurgusu. Süreli değil: <c>GridManager.ActivateHint</c> açar,
    /// <c>StopHintHighlight</c> kapatır.
    /// </summary>
    public void SetSelected(bool acik)
    {
        seciliKilitli = acik;
        if (acik)
        {
            Apply(selected);
        }
        else
        {
            normaleDonusZamani = -1f;
            Apply(normal);
        }
    }

    private void Zamanli(Sprite s, float sure)
    {
        if (s == null || seciliKilitli) return;

        Apply(s);
        normaleDonusZamani = Time.unscaledTime + Mathf.Max(0f, sure);
    }

    public void SetState(string durum)
    {
        switch (durum)
        {
            case "selected": Apply(selected); break;
            case "merging": Apply(merging); break;
            case "discovered": Apply(discovered); break;
            case "newborn": Apply(newborn); break;
            default: Apply(normal); break;
        }
    }

    private void Apply(Sprite s)
    {
        if (s != null && Target != null && Target.sprite != s) Target.sprite = s;
    }
}
