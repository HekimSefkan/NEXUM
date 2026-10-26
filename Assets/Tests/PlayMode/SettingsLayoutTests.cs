using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Ayarlar kartı: içerik kartın içinde kalıyor mu, kardeşler çakışıyor mu.
///
/// <b>2d-3a'da iki kusuru düzeltildi:</b>
///
/// 1) <b>Sprite ölçüleri sabit yazılıydı</b> (madde 4.3). Test
///    <c>796×1280</c> ve boyalı paylarını (<c>100/796</c>, <c>690/796</c>)
///    sabit tutuyor, kartın "iç alanını" bunlardan türetiyordu. Sprite Metal
///    kitine geçince bu sayılar EKRANDA OLMAYAN bir görseli tarif eder oldu;
///    test hayalet bir çerçeveye göre "içeride" demeye devam etti. Artık iç
///    alan canlı <c>Image.sprite</c>'ın kendi kenarlığından okunuyor.
///
/// 2) <b>Ölçülen nesneler elle yazılmış bir listeden geliyordu</b>
///    (madde 4.2). <c>Content[11]</c> içinde <c>ModeContainer</c> vardı ama
///    çöken üç buton (onun ÇOCUKLARI) yoktu. Artık kartın TÜM alt ağacı
///    geziliyor.
///
/// Çökme iddiası burada değil, <see cref="TouchTargetTests"/> içinde.
/// </summary>
public class SettingsLayoutTests
{
    private const string Tag = "NEXUM_SETTINGS";
    private const string CardPath = "SafeAreaRoot/SettingsPanel/GlassBackground";

    private RectTransform safeRoot;
    private RectTransform card;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");
        menu.GetType().GetMethod("OpenSettingsPanel", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(menu, null);
        yield return new WaitForSeconds(1f);

        safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        card = LayoutCheck.Find(CardPath) as RectTransform;
        Assert.IsNotNull(card, "GlassBackground yok");
    }

    [UnityTest]
    public IEnumerator AyarlarKartinIcindeKaliyor()
    {
        List<string> hatalar = new List<string>();

        // --- İÇ ALAN SPRITE'IN KENDİSİNDEN. Sabit sayı yok.
        Image kartImage = card.GetComponent<Image>();
        Assert.IsNotNull(kartImage, "GlassBackground'da Image yok");
        Assert.IsNotNull(kartImage.sprite, "GlassBackground'da sprite yok");

        Vector4 border = kartImage.sprite.border;
        float pay = Mathf.Max(Mathf.Max(border.x, border.y), Mathf.Max(border.z, border.w));
        Debug.Log($"{Tag}_SPRITE: {kartImage.sprite.name} " +
                  $"{kartImage.sprite.rect.width:F0}x{kartImage.sprite.rect.height:F0} " +
                  $"border ({border.x:F0},{border.y:F0},{border.z:F0},{border.w:F0}) " +
                  $"type {kartImage.type} -> iç pay {pay:F0}");
        Assert.Greater(pay, 0f,
            "kart sprite'ının 9-slice kenarlığı yok: iç alan türetilemez");

        foreach (Vector2 size in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, size);
            yield return null;
            yield return null;

            string ekran = $"{size.x:F0}x{size.y:F0}";

            Vector4 kartRect = LayoutCheck.LocalRect(safeRoot, card);
            Vector4 inner = new Vector4(kartRect.x + pay, kartRect.y + pay,
                                        kartRect.z - pay, kartRect.w - pay);

            Debug.Log($"{Tag}_KART: {ekran} | kart {kartRect.z - kartRect.x:F0}x" +
                      $"{kartRect.w - kartRect.y:F0} | iç alan x {inner.x:F0}..{inner.z:F0} " +
                      $"y {inner.y:F0}..{inner.w:F0}");

            // --- ELLE LİSTE YOK: kartın tüm alt ağacı
            // TouchArea HARİÇ: görünmez dokunma katmanı, yerleşim öğesi değil.
            // Ait olduğu butonun metniyle tanımı gereği üst üste biner.
            List<RectTransform> hepsi = card.GetComponentsInChildren<RectTransform>(true)
                .Where(r => r != card && r.gameObject.activeInHierarchy && r.name != "TouchArea")
                .ToList();

            foreach (RectTransform rt in hepsi)
            {
                Vector4 r = LayoutCheck.LocalRect(safeRoot, rt);
                float tasma = LayoutCheck.Overflow(r, inner);
                if (tasma > 1f)
                {
                    hatalar.Add($"{ekran} {PixelProbe.Path(rt)} kart dışına {tasma:F0} birim");
                }

                TMP_Text tmp = rt.GetComponent<TMP_Text>();
                string bilgi;
                if (tmp != null && LayoutCheck.IsClipped(tmp, out bilgi))
                {
                    hatalar.Add($"{ekran} {PixelProbe.Path(rt)} metin kırpılıyor ({bilgi})");
                }
            }

            // Çakışma: her ebeveyn altında kardeşler arasında
            foreach (var grup in hepsi.Where(r => r.parent != null).GroupBy(r => r.parent))
            {
                foreach (string c in LayoutCheck.FindOverlaps(safeRoot, grup.ToList()))
                {
                    hatalar.Add($"{ekran} çakışma: {c}");
                }
            }
        }

        List<string> tekil = hatalar.Distinct().ToList();
        Debug.Log($"{Tag}_SONUC: sorun {tekil.Count}");
        foreach (string h in tekil.Take(30)) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(tekil, string.Join(" | ", tekil.Take(8)));
    }
}
