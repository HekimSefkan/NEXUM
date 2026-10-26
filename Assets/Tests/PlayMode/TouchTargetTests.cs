using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// ÇÖKME İDDİASI (2d-3a düzeltmesi, madde 4.1).
///
/// Eski yerleşim testleri yalnızca "kartın içinde mi" ve "kardeşiyle
/// çakışıyor mu" diye soruyordu. 56×56'ya ÇÖKEN bir buton kartın daha ÇOK
/// içindedir ve daha AZ çakışır — yani o ölçüt kümesi çökmeyi ÖDÜLLENDİRİYOR.
/// Ayarlar panelinin üç mod butonu tam olarak böyle kaçtı.
///
/// İki kural, İKİ FARKLI ŞEYİ ölçer:
///
///   1) <b>DOKUNMA ALANI</b> >= <see cref="Min"/> (144 birim = 48dp), iki
///      eksende de. Ölçülen şey grafiğin boyutu DEĞİL, raycast alanıdır:
///      elemanın kendi rect'i ile saydam <c>TouchArea</c> çocuğunun birleşimi
///      (bkz. <c>NexumTouchTargets</c>). Grafik olduğu gibi kalır.
///
///   2) <b>GRAFİK</b>, aynı ebeveyn altındaki kardeşlerinin ortalamasının
///      <see cref="SiblingFloor"/> katından küçük olamaz. Çökmeyi yakalayan
///      kural budur: 56x56'ya düşen mod butonu 144'lük dokunma alanıyla
///      1. kuralı geçerdi ama grafiği kardeşlerinin yarısından küçük olurdu.
///
///   3) <b>İKİ DOKUNMA ALANI KESİŞEMEZ.</b> uGUI raycast'i ebeveyn rect'ine
///      KIRPILMAZ: 92 birimlik tarif satırlarına konan 144 birimlik alan
///      komşu satırlara taşar ve oyuncu yanlış satırı açar. Komşuyu
///      çalmamak, 144'e ulaşmaktan önce gelir — çözülemeyen bağlamda kural
///      bilinçli gevşetilir ve gerekçesi docs/DESIGN_TOKENS.md'ye yazılır.
///
/// Ölçüm dört canvas boyutunda, GERÇEK (layout sonrası) rect üzerinde.
/// </summary>
public class TouchTargetTests
{
    private const string Tag = "NEXUM_DOKUNMA";
    private const float Min = 144f;
    private const float SiblingFloor = 0.5f;

    [UnityTest]
    public IEnumerator MenuDokunmaHedefleri()
    {
        yield return Olc("MainMenu");
    }

    [UnityTest]
    public IEnumerator OyunDokunmaHedefleri()
    {
        yield return Olc("Game");
    }

    private static void KesisimAra(List<KeyValuePair<RectTransform, Rect>> kume,
                                   string ekran, List<string> hatalar)
    {
        for (int i = 0; i < kume.Count; i++)
        {
            for (int j = i + 1; j < kume.Count; j++)
            {
                Rect a = kume[i].Value, b = kume[j].Value;
                float ox = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                float oy = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                if (ox <= 1f || oy <= 1f) continue;

                hatalar.Add($"{ekran}: {PixelProbe.Path(kume[i].Key)} ile " +
                            $"{PixelProbe.Path(kume[j].Key)} dokunma alanları " +
                            $"kesişiyor ({ox:F0}x{oy:F0} = {ox * oy:F0} birim²)");
            }
        }
    }

    /// <summary>En üstteki "...Panel" atasının adı; yoksa "HUD".</summary>
    private static string EkranAnahtari(Transform t)
    {
        string anahtar = "HUD";
        while (t != null)
        {
            if (t.name.EndsWith("Panel")) anahtar = t.name;
            t = t.parent;
        }
        return anahtar;
    }

    /// <summary>Grafiğin ve (varsa) TouchArea çocuğunun birleşimi, kök-yerel.</summary>
    private static Rect DokunmaKapsami(RectTransform kok, RectTransform rt)
    {
        Rect r = Kutu(kok, rt);
        Transform alan = rt.Find("TouchArea");
        if (alan is RectTransform ar && ar.rect.width > 1f && ar.rect.height > 1f)
        {
            Rect a = Kutu(kok, ar);
            r = Rect.MinMaxRect(Mathf.Min(r.xMin, a.xMin), Mathf.Min(r.yMin, a.yMin),
                                Mathf.Max(r.xMax, a.xMax), Mathf.Max(r.yMax, a.yMax));
        }
        return r;
    }

    private static Rect Kutu(RectTransform kok, RectTransform rt)
    {
        Vector3[] k = new Vector3[4];
        rt.GetWorldCorners(k);
        Vector3 a = kok.InverseTransformPoint(k[0]);
        Vector3 b = kok.InverseTransformPoint(k[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                               Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    private IEnumerator Olc(string sahne)
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene(sahne);
        yield return null;
        yield return new WaitForSeconds(1f);

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        Transform canvas = LayoutCheck.Find("");
        if (canvas == null)
        {
            canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        }
        Assert.IsNotNull(canvas, "Canvas yok");

        // Bütün paneller açılır: kapalı panelin içindeki buton da ölçülmeli.
        foreach (Transform p in PixelProbe.Panels(canvas))
        {
            p.gameObject.SetActive(true);
            p.localScale = Vector3.one;
        }

        List<string> hatalar = new List<string>();
        float enKucuk = float.MaxValue;
        string enKucukAd = "-";
        int olculen = 0;

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";

            Selectable[] hepsi = canvas.GetComponentsInChildren<Selectable>(true);
            foreach (Selectable s in hepsi)
            {
                RectTransform rt = s.transform as RectTransform;
                if (rt == null) continue;

                // KAYDIRMA ÇUBUĞU MUAF. Çubuk dokunulan değil SÜRÜKLENEN bir
                // denetim ve asıl etkileşimi ScrollRect'in kendi içerik
                // alanıdır (tam ekran). 144'e çıkarmak içeriğin üstünü
                // kapatırdı. Gerekçe docs/DESIGN_TOKENS.md'de yazılı.
                if (s is Scrollbar) continue;

                olculen++;

                // Dokunma alanı: elemanın kendi rect'i + saydam TouchArea çocuğu
                float w = rt.rect.width, h = rt.rect.height;
                Transform alan = rt.Find("TouchArea");
                if (alan is RectTransform ar)
                {
                    w = Mathf.Max(w, ar.rect.width);
                    h = Mathf.Max(h, ar.rect.height);
                }

                float kucuk = Mathf.Min(w, h);
                if (kucuk < enKucuk) { enKucuk = kucuk; enKucukAd = $"{ekran} {PixelProbe.Path(rt)}"; }

                if (w + 0.5f < Min || h + 0.5f < Min)
                {
                    hatalar.Add($"{ekran}: {PixelProbe.Path(rt)} dokunma alanı {w:F0}x{h:F0} " +
                                $"(alt sınır {Min:F0})");
                }
            }

            // --- 3. KURAL: dokunma alanları kesişemez
            // Yalnızca AYNI EKRANDA bulunabilecek çiftler karşılaştırılır:
            // iki panel aynı anda görünmez, modal açıkken arkadaki HUD'un
            // raycast'ini karartma katmanı zaten keser. Ekran anahtarı =
            // en üstteki "...Panel" atası, yoksa "HUD".
            Dictionary<string, List<KeyValuePair<RectTransform, Rect>>> ekranlar =
                new Dictionary<string, List<KeyValuePair<RectTransform, Rect>>>();
            foreach (Selectable s in hepsi)
            {
                RectTransform rt = s.transform as RectTransform;
                if (rt == null || !rt.gameObject.activeInHierarchy) continue;
                if (s is Scrollbar) continue;

                string anahtar = EkranAnahtari(rt);
                if (!ekranlar.ContainsKey(anahtar))
                {
                    ekranlar[anahtar] = new List<KeyValuePair<RectTransform, Rect>>();
                }
                ekranlar[anahtar].Add(
                    new KeyValuePair<RectTransform, Rect>(rt, DokunmaKapsami(safeRoot, rt)));
            }

            foreach (var kume in ekranlar.Values)
            {
                KesisimAra(kume, ekran, hatalar);
            }

            // Kardeş oranı: aynı ebeveyn altındaki etkileşimli kardeşler.
            // ScrollRect'in kaydırma çubukları HARİÇ: onlar görünüm kardeşi
            // değil, ScrollRect'in parçası; ölçüleri kasıtlı olarak ince ve
            // "kardeş ortalaması" karşılaştırması anlamsız. 144 alt sınırı
            // onlara da uygulanıyor (yukarıdaki kural).
            foreach (var grup in hepsi.Where(s => s.transform.parent != null
                                                  && !(s is Scrollbar))
                                      .GroupBy(s => s.transform.parent))
            {
                List<RectTransform> kardes = grup
                    .Select(s => s.transform as RectTransform)
                    .Where(r => r != null).ToList();
                if (kardes.Count < 2) continue;

                // EKSEN BAŞINA değil, EN KÜÇÜK KENARIN MEDYANINA göre.
                // Eksen başına ortalama, simge butonunu (100x100) metin
                // butonlarının (250x100) yanında haksız yere düşürüyordu:
                // farklı en-boy oranı çökme değildir. Medyan ayrıca tek bir
                // büyük kardeşin eşiği şişirmesini de engeller.
                List<float> kucukKenar = kardes.Select(r => Mathf.Min(r.rect.width, r.rect.height))
                                               .OrderBy(x => x).ToList();
                float medyan = kucukKenar[kucukKenar.Count / 2];

                foreach (RectTransform r in kardes)
                {
                    float k = Mathf.Min(r.rect.width, r.rect.height);
                    if (k < medyan * SiblingFloor)
                    {
                        hatalar.Add($"{ekran}: {PixelProbe.Path(r)} GRAFİK " +
                                    $"{r.rect.width:F0}x{r.rect.height:F0} (en küçük kenar {k:F0}) " +
                                    $"kardeş medyanının ({medyan:F0}) " +
                                    $"%{SiblingFloor * 100f:F0}'inin altında");
                    }
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: {sahne} ölçülen {olculen}, en küçük kenar {enKucuk:F0} " +
                  $"({enKucukAd}), sorun {hatalar.Count}");
        foreach (string h in hatalar.Distinct().Take(40)) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar.Distinct().ToList(),
            string.Join(" | ", hatalar.Distinct().Take(10)));
    }
}
