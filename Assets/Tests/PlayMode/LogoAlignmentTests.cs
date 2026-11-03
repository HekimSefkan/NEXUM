using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// LOGO KİLİDİ KENDİ SINIRLARINA GÖRE ORTALANIR (3c/4).
///
/// Kusur: <c>NEXUM_logo_metal.png</c> 1366x320 tuvalde duruyordu ama boyalı
/// içerik x=19..955 arasındaydı; kilidin merkezi tuvalin merkezinden
/// %14,3 SOLDAYDI. Bir Image'ı ortalamak logoyu ortalamıyordu, çünkü
/// ortalanan şey boşluğu da içeren tuvaldi. Üstüne logo Image'larının
/// çoğunda <c>preserveAspect</c> kapalıydı: kilit kutusunun oranına
/// GERİLİYORDU.
///
/// ÖLÇÜM TOKEN TABLOSUNDAN DEĞİL PİKSELDEN: kare render edilir, logo
/// kutusunun içinde zeminden ayrışan piksellerin yatay sınırları bulunur ve
/// bu BOYALI İÇERİĞİN merkezi kutunun merkeziyle karşılaştırılır. Kırpma
/// öncesi hâlde sapma kutu genişliğinin ~%7'si kadardı.
///
/// Elle yazılmış obje listesi yok: alt ağaç gezilir, logo sprite'ını
/// gösteren her Image ölçülür. Sprite ölçüsü teste sabit yazılmaz.
/// </summary>
public class LogoAlignmentTests
{
    private const string Tag = "NEXUM_LOGO";

    /// <summary>Merkez sapması, kutu genişliğinin en çok bu oranı kadar olabilir.</summary>
    private const float ToleransOran = 0.02f;

    /// <summary>Bir pikselin "logoya ait" sayılması için zeminle en az bu fark.</summary>
    private const float AyrismaEsigi = 0.12f;

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator MenudeLogoOrtali()
    {
        yield return Olc("MainMenu");
    }

    [UnityTest]
    [Timeout(900000)]
    public IEnumerator OyundaLogoOrtali()
    {
        yield return Olc("Game");
    }

    private IEnumerator Olc(string sahne)
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene(sahne);
        yield return null;
        yield return new WaitForSeconds(1f);

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        List<Image> logolar = canvas.GetComponentsInChildren<Image>(true)
            .Where(i => i != null && i.sprite != null && LogoMu(i.sprite))
            .ToList();
        Assert.IsNotEmpty(logolar, $"{sahne}: logo gösteren Image yok");

        List<string> hatalar = new List<string>();

        // Oran kuralı: kilit kutusunun oranına gerilmemeli
        foreach (Image img in logolar)
        {
            if (!img.preserveAspect)
            {
                hatalar.Add($"{PixelProbe.Path(img.transform)}: preserveAspect kapalı");
            }
        }

        // Her logo KENDİ panelinde ölçülür; diğer paneller kapalı kalır
        foreach (Image img in logolar)
        {
            Transform panel = Panel(canvas, img.transform);

            foreach (Transform p in PixelProbe.Panels(canvas))
            {
                p.gameObject.SetActive(p == panel);
            }
            if (panel != null) panel.localScale = Vector3.one;

            // Logonun kendisi ve ataları açık olmalı (panel dışı kalanlar dahil)
            for (Transform ata = img.transform; ata != null; ata = ata.parent)
            {
                if (!ata.gameObject.activeSelf) ata.gameObject.SetActive(true);
            }

            // ÖLÇÜM YALNIZ ZEMİNDE: arka plan gradyanı (merkez parlaması)
            // sütun karşılaştırmasını kirletiyordu — kutunun orta sütunları
            // kenar sütunlarından farklı çıkıyor ve logo olmayan yer "logo"
            // sayılıyordu. Ölçüm sırasında logodan başka hiçbir Image
            // çizilmez; kare kameranın düz temiz rengine düşer.
            List<Image> kapatilan = new List<Image>();
            foreach (Image diger in canvas.GetComponentsInChildren<Image>(true))
            {
                if (diger == img || !diger.enabled) continue;
                diger.enabled = false;
                kapatilan.Add(diger);
            }

            yield return null;
            yield return null;

            PixelProbe.Session s = PixelProbe.Begin(canvas.gameObject, new Vector2Int(540, 960), true);
            yield return null;
            yield return null;
            PixelProbe.Render(s);

            // BOŞ KARE MUHAFIZI: burada PixelProbe.IsBlank kullanılamaz —
            // ölçüm sırasında logodan başka hiçbir şey çizilmediği için
            // karenin köşeleri ZATEN tek renktir. Muhafız aşağıda: hiçbir
            // logo için boyalı sütun bulunamazsa (-nographics) test düşer.

            RectInt? kutu = PixelProbe.Bounds(s, img.rectTransform);
            if (kutu == null)
            {
                PixelProbe.End(s);
                foreach (Image geri in kapatilan) geri.enabled = true;
                continue;
            }

            Vector2? sinir = IcerikSiniri(s.frame, kutu.Value);
            if (sinir == null)
            {
                PixelProbe.End(s);
                foreach (Image geri in kapatilan) geri.enabled = true;
                hatalar.Add($"{PixelProbe.Path(img.transform)}: logo karede görünmüyor " +
                            "(süit -nographics ile koşuyor olabilir)");
                continue;
            }

            float kutuMerkez = kutu.Value.x + kutu.Value.width * 0.5f;
            float icerikMerkez = (sinir.Value.x + sinir.Value.y) * 0.5f;
            float sapma = Mathf.Abs(icerikMerkez - kutuMerkez);
            float oran = sapma / kutu.Value.width;

            Debug.Log($"{Tag}_ORTALAMA: {PixelProbe.Path(img.transform)} | " +
                      $"kutu {kutu.Value.x}..{kutu.Value.xMax} (merkez {kutuMerkez:F1}) | " +
                      $"boyalı içerik {sinir.Value.x:F0}..{sinir.Value.y:F0} " +
                      $"(merkez {icerikMerkez:F1}) | sapma {sapma:F1}px = %{oran * 100f:F1}");

            if (oran > ToleransOran)
            {
                hatalar.Add($"{PixelProbe.Path(img.transform)}: boyalı içeriğin merkezi " +
                            $"kutunun merkezinden %{oran * 100f:F1} kaymış");
            }

            PixelProbe.End(s);
            foreach (Image geri in kapatilan) geri.enabled = true;
        }

        Debug.Log($"{Tag}_SONUC: {sahne}: {logolar.Count} logo, sorun {hatalar.Count}");
        foreach (string h in hatalar.Distinct()) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar.Distinct().ToList(),
            string.Join(" | ", hatalar.Distinct().Take(6)));
    }

    /// <summary>
    /// ÜST BANT (1.3/0.1-0.2): logo yatayda ortalı, avatar buton bandının
    /// sol kenarında ve üçü (logo × avatar × ilk buton) çakışmıyor.
    /// Dört canvas boyutunda ölçülür.
    /// </summary>
    [UnityTest]
    [Timeout(900000)]
    public IEnumerator MenuUstBandiHizali()
    {
        SaveService.Data.selectedLevel = 0;
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        RectTransform logo = Bul(safeRoot, "MainMenuPanel/Logo");
        RectTransform avatar = Bul(safeRoot, "MainMenuPanel/ProfileButton");
        RectTransform oyna = Bul(safeRoot, "MainMenuPanel/PlayButton");
        Assert.IsNotNull(logo, "ana menüde Logo yok");
        Assert.IsNotNull(avatar, "ProfileButton yok");
        Assert.IsNotNull(oyna, "PlayButton yok");

        List<string> hatalar = new List<string>();

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";
            Vector4 l = LayoutCheck.LocalRect(safeRoot, logo);
            Vector4 a = LayoutCheck.LocalRect(safeRoot, avatar);
            Vector4 p = LayoutCheck.LocalRect(safeRoot, oyna);

            float logoMerkez = (l.x + l.z) * 0.5f;
            float bandSol = p.x;
            float avatarSol = a.x;

            Debug.Log($"{Tag}_USTBANT: {ekran} | logo merkez {logoMerkez:F1} " +
                      $"({l.y:F0}..{l.w:F0}) | avatar sol {avatarSol:F1} ({a.y:F0}..{a.w:F0}) | " +
                      $"band sol {bandSol:F1} | OYNA üst {p.w:F0}");

            if (Mathf.Abs(logoMerkez) > 1f)
            {
                hatalar.Add($"{ekran}: logo kutusu ortada değil ({logoMerkez:F1})");
            }
            if (Mathf.Abs(avatarSol - bandSol) > 1f)
            {
                hatalar.Add($"{ekran}: avatar sol kenarı band kenarında değil " +
                            $"({avatarSol:F1} ≠ {bandSol:F1})");
            }
            if (LayoutCheck.OverlapArea(l, a) > 1f) hatalar.Add($"{ekran}: logo × avatar çakışıyor");
            if (LayoutCheck.OverlapArea(l, p) > 1f) hatalar.Add($"{ekran}: logo × OYNA çakışıyor");
            if (LayoutCheck.OverlapArea(a, p) > 1f) hatalar.Add($"{ekran}: avatar × OYNA çakışıyor");
        }

        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    private static RectTransform Bul(RectTransform safeRoot, string yol)
    {
        Transform t = safeRoot.Find(yol);
        return t as RectTransform;
    }

    /// <summary>
    /// Kutunun içindeki BOYALI içeriğin yatay sınırları (x0, x1).
    /// Zemin, kutunun en sol ve en sağ sütunlarının ortalamasıdır; ondan
    /// yeterince ayrışan sütun "logo var" sayılır.
    /// </summary>
    private static Vector2? IcerikSiniri(Texture2D frame, RectInt r)
    {
        Color zemin = Kolon(frame, r, r.x) ;
        Color zemin2 = Kolon(frame, r, r.xMax - 1);
        zemin = new Color((zemin.r + zemin2.r) * 0.5f, (zemin.g + zemin2.g) * 0.5f,
                          (zemin.b + zemin2.b) * 0.5f);

        int x0 = -1, x1 = -1;
        for (int x = r.x; x < r.xMax; x++)
        {
            Color c = Kolon(frame, r, x);
            float fark = Mathf.Abs(c.r - zemin.r) + Mathf.Abs(c.g - zemin.g) + Mathf.Abs(c.b - zemin.b);
            if (fark < AyrismaEsigi) continue;

            if (x0 < 0) x0 = x;
            x1 = x;
        }

        if (x0 < 0 || x1 <= x0) return null;
        return new Vector2(x0, x1);
    }

    /// <summary>Sütunun ortalama rengi (kutu yüksekliği boyunca).</summary>
    private static Color Kolon(Texture2D frame, RectInt r, int x)
    {
        float sr = 0f, sg = 0f, sb = 0f;
        int n = 0;
        for (int y = r.y; y < r.yMax; y++)
        {
            Color c = frame.GetPixel(x, y);
            sr += c.r; sg += c.g; sb += c.b;
            n++;
        }
        return n == 0 ? Color.black : new Color(sr / n, sg / n, sb / n);
    }

    private static Transform Panel(Transform canvas, Transform t)
    {
        List<Transform> paneller = PixelProbe.Panels(canvas);
        while (t != null)
        {
            if (paneller.Contains(t)) return t;
            t = t.parent;
        }
        return null;
    }

    /// <summary>Logo sprite'ı mı? Obje adına değil, SPRITE adına bakılır.</summary>
    private static bool LogoMu(Sprite s)
    {
        return s.name.StartsWith("NEXUM_logo") || s.name.StartsWith("NEXUM_mark");
    }
}
