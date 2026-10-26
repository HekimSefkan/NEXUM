using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Metin / yüzey kontrastını GERÇEK RENDER PİKSELİNDEN ölçer ve 4,5:1 altında
/// kalan her eşleşmeyi düzeltir.
///
/// NEDEN TOKEN TABLOSU YETMEZ: bir metnin okunup okunmadığı rengiyle değil,
/// ARKASINDAKİ YÜZEYLE olan farkıyla belirlenir. Sprite geçişi (2d-3a) bir
/// sürü yüzeyi açıktan koyuya çevirdi; metin renkleri olduğu yerde kaldı.
/// Ana menüdeki TARİF DEFTERİ butonunun etiketi böyle 1,12:1'e düştü — rengi
/// (textOnLight #323232) palette geçerli bir rol, yüzeyi artık değil.
///
/// ÖLÇÜM: ekran, <see cref="NexumDeviceShot"/> ile aynı teknikle hedef
/// boyutta bir RenderTexture'a çizdirilir. Önce BÜTÜN TMP bileşenleri
/// kapatılır ve bir kare alınır: bu, metinlerin ARKASINDAKİ yüzeydir. Her
/// metnin ekran dikdörtgeni o kareden örneklenir (medyan).
///
/// DÜZELTME:
///   - metin rolündeki renkler (textPrimary / textSecondary / textMuted /
///     textOnLight) -> yüzeye göre en yüksek kontrastlı metin rolü
///   - anlam taşıyan renkler (danger / success / warning / primary ...) ->
///     HUE KORUNUR, yalnızca parlaklık 4,5:1'e ulaşana kadar siyaha/beyaza
///     çekilir. Kırmızı uyarı kırmızı kalır.
///
/// IDEMPOTENT: renk zaten geçiyorsa dosyaya dokunulmaz.
///
/// Menüden: NEXUM → Kontrast Taraması
/// Batch:   -executeMethod NexumContrastSweep.Run
///          (-dryRun verilirse yalnızca rapor; hiçbir şey yazılmaz)
/// </summary>
public static class NexumContrastSweep
{
    private const string Tag = "NEXUM_KONTRAST";
    private const float Esik = 4.5f;
    private static readonly Vector2Int Boyut = new Vector2Int(1080, 1920);

    /// <summary>Hedefe çekerken bırakılan pay: 4,50 tam sınırda kalıyordu.</summary>
    private const float Hedef = 4.7f;

    /// <summary>Metin rolleri: yüzeye göre serbestçe seçilebilir.</summary>
    private static readonly string[] MetinRolleri =
    {
        "textPrimary", "textSecondary", "textMuted", "textOnLight",
    };

    /// <summary>
    /// ANLAM taşıyan roller: hue korunur, yalnızca parlaklık çekilir.
    /// Kırmızı uyarı kırmızı kalmalı. Zemin rolleri (panelBackground,
    /// cardBackground) bu listede YOK: bir metnin zemin rengini taşıması
    /// anlam değil, hatadır — serbestçe değiştirilir.
    /// </summary>
    private static readonly string[] AnlamRolleri =
    {
        "primary", "primaryBright", "secondary", "danger", "success", "warning",
    };

    private struct Bulgu
    {
        public string ekran;
        public string yol;
        public Color eski;
        public Color yeni;
        public Color yuzey;
        public float onceki;
        public float sonraki;
        public string gerekce;
    }

    [MenuItem("NEXUM/Kontrast Taraması")]
    public static void RunFromMenu()
    {
        Calistir(false);
    }

    public static void Run()
    {
        Calistir(UIBuilder.DryRunRequested);
    }

    private struct Ekran
    {
        public string ad;
        public string scene;
        public string panel;      // acik birakilacak panel yolu (null = HUD)
    }

    /// <summary>
    /// Panel-benzeri obje: adi "Panel" ile biten, Canvas ya da SafeAreaRoot'un
    /// dogrudan cocugu olan obje. Tarama bunlari TEK TEK acar; HUD ve diger
    /// nesneler sahnedeki hallerinde kalir.
    /// </summary>
    private static List<Transform> Panelleri(Transform canvas)
    {
        List<Transform> p = new List<Transform>();
        foreach (string kokAd in new[] { "", "SafeAreaRoot" })
        {
            Transform kok = string.IsNullOrEmpty(kokAd) ? canvas : canvas.Find(kokAd);
            if (kok == null) continue;
            foreach (Transform t in kok)
            {
                if (!t.name.EndsWith("Panel")) continue;
                p.Add(t);
            }
        }
        return p;
    }

    /// <summary>
    /// Her panel KENDI ekrani olarak taranir — ELLE YAZILMIS LISTE YOK.
    /// 2d-3a teshisinde gorulen desen: elle tutulan liste, adini anmadigi
    /// nesnedeki kusuru goremiyor.
    /// </summary>
    private static List<Ekran> Ekranlar()
    {
        List<Ekran> hepsi = new List<Ekran>();

        foreach (string yol in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" })
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            GameObject canvasGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
            if (canvasGo == null) continue;

            // Panel acik degilken: HUD ve menu kromu
            hepsi.Add(new Ekran { ad = Path.GetFileNameWithoutExtension(yol) + "-HUD",
                                  scene = yol, panel = null });

            foreach (Transform t in Panelleri(canvasGo.transform))
            {
                if (t.GetComponentsInChildren<TMP_Text>(true).Length == 0) continue;
                hepsi.Add(new Ekran { ad = t.name, scene = yol,
                                      panel = PanelYolu(canvasGo.transform, t) });
            }
        }

        return hepsi;
    }

    private static string PanelYolu(Transform canvas, Transform t)
    {
        string s = t.name;
        while (t.parent != null && t.parent != canvas) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    /// <summary>
    /// Istenen paneli acar, digerlerini kapatir ve ONCEKI DURUMU dondurur.
    /// Tarama sahneyi KAYDEDIYOR; acik/kapali durumlari geri yazilmazsa
    /// tarama kendi yan etkisini diske yazar.
    /// </summary>
    private static Dictionary<Transform, bool> Hazirla(Transform canvas, Ekran e)
    {
        Dictionary<Transform, bool> onceki = new Dictionary<Transform, bool>();

        foreach (Transform t in Panelleri(canvas))
        {
            onceki[t] = t.gameObject.activeSelf;
            t.gameObject.SetActive(false);
        }

        if (string.IsNullOrEmpty(e.panel)) return onceki;

        Transform hedef = canvas.Find(e.panel);
        if (hedef == null) return onceki;

        hedef.gameObject.SetActive(true);
        hedef.localScale = Vector3.one;     // panel gecisi tween'i 0'da birakmis olabilir
        return onceki;
    }

    private static void Geri(Dictionary<Transform, bool> onceki)
    {
        foreach (var kv in onceki)
        {
            if (kv.Key != null) kv.Key.gameObject.SetActive(kv.Value);
        }
    }

    /// <summary>
    /// Metin rengiyle çözülemeyen tek durum: yüzeyin KENDİSİ düzeltilir.
    ///
    /// <c>AssistantPanel/CloseHintButton</c>'ın zemini tema dışı bir yeşil
    /// (#248F00, L=0,233). Beyaz metin orada 4,20:1'de kalıyor, koyu metin
    /// daha da kötü (3,45:1). Hiçbir metin rolü eşiği geçmiyor, yani kusur
    /// metinde değil zeminde. Zemin palet rolüne (<c>success</c>) alınıp
    /// beyaz metin eşiği geçene kadar karartılıyor.
    /// </summary>
    private static readonly Dictionary<string, string> YuzeyDuzeltmesi =
        new Dictionary<string, string>
        {
            // Anahtar OBJE ADI; yol değil. Yol anahtarı sahne ağacı
            // değişince sessizce eşleşmiyordu.
            { "CloseHintButton", "success" },

            // Duraklat butonunun zemini tema dışı bir haki (#AAA98D).
            // Ölçüm: beyaz 3,56 · textOnLight 3,60 · siyah 5,90 — hiçbir
            // metin rolü 4,5'i geçmiyor, yani kusur metinde değil zeminde.
            { "PauseButton", "cardBackground" },
        };

    /// <summary>
    /// ÇALIŞMA ZAMANINDA ÜRETİLEN kartların metinleri. Sahne taraması
    /// yalnızca sahnedeki nesneleri görür; ansiklopedi kartları
    /// <c>EncyclopediaManager</c> tarafından prefab'dan klonlanıyor ve
    /// taramaya hiç girmiyor. Değer <c>ContrastTests</c>'in play mode'da
    /// ölçtüğü yüzeye (#1D4F6F) göre hesaplandı: <c>success</c> orada
    /// 4,16:1'de kalıyordu, %20 beyaza çekilince 4,76:1.
    /// Hue korunuyor — yeşil hâlâ "başarı".
    /// </summary>
    private static readonly string PrefabYolu = "Assets/Prefabs/ElementCard.prefab";
    private static readonly string[] PrefabMetinleri = { "NameText", "SymbolText" };
    private static readonly Color PrefabRengi = new Color32(0x58, 0xD6, 0x8D, 0xFF);

    private static void PrefabMetinleriniDuzelt(bool dryRun, List<string> dokum)
    {
        GameObject kok = PrefabUtility.LoadPrefabContents(PrefabYolu);
        bool degisti = false;

        foreach (string ad in PrefabMetinleri)
        {
            Transform t = kok.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(x => x.name == ad);
            if (t == null) continue;

            TMP_Text tmp = t.GetComponent<TMP_Text>();
            if (tmp == null) continue;

            Color hedef = PrefabRengi;
            hedef.a = tmp.color.a;
            if (((Color32)tmp.color).Equals((Color32)hedef)) continue;

            dokum.Add($"PREFAB {Path.GetFileName(PrefabYolu)}/{ad}: " +
                      $"{Hex(tmp.color)} -> {Hex(hedef)}");
            if (!dryRun) { tmp.color = hedef; degisti = true; }
        }

        if (degisti) PrefabUtility.SaveAsPrefabAsset(kok, PrefabYolu);
        PrefabUtility.UnloadPrefabContents(kok);
    }

    private static void YuzeyleriDuzelt(GameObject canvasGo, bool dryRun, List<string> dokum)
    {
        foreach (var kv in YuzeyDuzeltmesi)
        {
            Transform t = canvasGo.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(x => x.name == kv.Key);
            if (t == null) continue;

            Image img = t.GetComponent<Image>();
            if (img == null) continue;

            Color taban = NexumPalette.ByRole(kv.Value);
            Color hedef = taban;
            for (int i = 0; i <= 40; i++)
            {
                hedef = Color.Lerp(taban, Color.black, i / 40f);
                if (Kontrast(Color.white, hedef) >= Hedef) break;
            }
            hedef.a = img.color.a;

            if (((Color32)img.color).Equals((Color32)hedef)) continue;

            dokum.Add($"YÜZEY {kv.Key}: {Hex(img.color)} -> {Hex(hedef)} " +
                      $"(beyaz {Kontrast(Color.white, hedef):F2}:1)");
            if (!dryRun)
            {
                img.color = hedef;
                EditorUtility.SetDirty(img);
            }
        }
    }

    private static void Calistir(bool dryRun)
    {
        List<Bulgu> bulgular = new List<Bulgu>();
        List<string> yuzeyDokum = new List<string>();
        int olculen = 0, atlanan = 0;

        foreach (Ekran e in Ekranlar())
        {
            Scene scene = EditorSceneManager.OpenScene(e.scene, OpenSceneMode.Single);
            GameObject canvasGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
            if (canvasGo == null) continue;

            Dictionary<Transform, bool> onceki = Hazirla(canvasGo.transform, e);
            YuzeyleriDuzelt(canvasGo, dryRun, yuzeyDokum);

            // Yalnızca O EKRANA ait metinler ölçülür.
            // Bir modal açıkken arkadaki HUD de karartma katmanının ALTINDA
            // kalıyor: metin de yüzey de aynı oranda kararır, yani oranları
            // değişmez. Karartılmış yüzeyi karartılmamış metin rengiyle
            // karşılaştırmak yanlış bulgu üretiyordu (aynı HUD butonu sekiz
            // ekranda sekiz kez rapor ediliyordu). HUD kendi ekranında ölçülür.
            Transform kapsam = string.IsNullOrEmpty(e.panel)
                ? canvasGo.transform
                : canvasGo.transform.Find(e.panel);
            if (kapsam == null) { Geri(onceki); continue; }

            List<TMP_Text> metinler = kapsam.GetComponentsInChildren<TMP_Text>(false)
                .Where(t => t != null && t.rectTransform.rect.width > 1f
                            && t.rectTransform.rect.height > 1f)
                .ToList();

            Debug.Log($"{Tag}_EKRAN: {e.ad,-20} metin {metinler.Count}");

            Texture2D zemin = ZeminiCek(canvasGo, metinler);
            if (zemin == null) continue;
            if (BosKare(zemin))
            {
                Object.DestroyImmediate(zemin);
                Geri(onceki);
                Debug.LogError($"{Tag}: BOŞ KARE — render bağlamı yok. Bu komut " +
                               "-nographics İLE ÇALIŞTIRILMAZ (kamera hiçbir şey " +
                               "çizemez ve her yüzey #CDCDCD ölçülür).");
                Debug.Log($"{Tag}: FAIL");
                return;
            }

            foreach (TMP_Text tmp in metinler)
            {
                Color? yuzey = Ornekle(zemin, canvasGo, tmp);
                if (yuzey == null) { atlanan++; continue; }

                olculen++;
                float once = Kontrast(tmp.color, yuzey.Value);
                if (once >= Esik) continue;

                string gerekce;
                Color yeni = Duzelt(tmp.color, yuzey.Value, out gerekce);
                if (ColorUtility.ToHtmlStringRGB(yeni) == ColorUtility.ToHtmlStringRGB(tmp.color)) continue;
                float sonra = Kontrast(yeni, yuzey.Value);

                bulgular.Add(new Bulgu
                {
                    ekran = e.ad,
                    yol = Yol(tmp.transform),
                    eski = tmp.color,
                    yeni = yeni,
                    yuzey = yuzey.Value,
                    onceki = once,
                    sonraki = sonra,
                    gerekce = gerekce,
                });

                if (!dryRun)
                {
                    Color hedef = yeni;
                    hedef.a = tmp.color.a;
                    tmp.color = hedef;
                    EditorUtility.SetDirty(tmp);
                }
            }

            Object.DestroyImmediate(zemin);
            Geri(onceki);

            if (!dryRun && (bulgular.Any(x => x.ekran == e.ad) || yuzeyDokum.Count > 0))
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        foreach (Bulgu b in bulgular.OrderBy(x => x.onceki))
        {
            Debug.Log($"{Tag}_BULGU: {b.ekran,-14} {Kisa(b.yol),-46} " +
                      $"yüzey {Hex(b.yuzey)}  {Hex(b.eski)} {b.onceki:F2}:1 -> " +
                      $"{Hex(b.yeni)} {b.sonraki:F2}:1   ({b.gerekce})");
        }

        PrefabMetinleriniDuzelt(dryRun, yuzeyDokum);
        foreach (string d in yuzeyDokum.Distinct()) Debug.Log($"{Tag}_YUZEY: {d}");

        // --- HİYERARŞİ DÖKÜMÜ: ne kadarı hangi role gitti?
        Dictionary<string, int> hedefSayim = new Dictionary<string, int>();
        Dictionary<string, int> kaynakSayim = new Dictionary<string, int>();
        foreach (Bulgu b in bulgular)
        {
            string h = RolBul(b.yeni) ?? "(palet dışı: anlam rengi çekildi)";
            string k = RolBul(b.eski) ?? "(palet dışı)";
            hedefSayim[h] = hedefSayim.TryGetValue(h, out int n) ? n + 1 : 1;
            kaynakSayim[k] = kaynakSayim.TryGetValue(k, out int m) ? m + 1 : 1;
        }
        foreach (var kv in kaynakSayim.OrderByDescending(x => x.Value))
        {
            Debug.Log($"{Tag}_KAYNAK_ROL: {kv.Key,-34} {kv.Value}");
        }
        foreach (var kv in hedefSayim.OrderByDescending(x => x.Value))
        {
            Debug.Log($"{Tag}_HEDEF_ROL: {kv.Key,-34} {kv.Value}");
        }

        int kalan = bulgular.Count(b => b.sonraki < Esik);
        Debug.Log($"{Tag}: ölçülen {olculen}, atlanan {atlanan}, " +
                  $"eşik altı {bulgular.Count}, düzeltilemeyen {kalan}" +
                  (dryRun ? "  (dryRun: yazılmadı)" : ""));
        Debug.Log($"{Tag}: {(kalan == 0 ? "OK" : "FAIL")}");
    }

    // ------------------------------------------------------------------ render

    /// <summary>Tüm TMP'ler kapalıyken bir kare: metinlerin ARKASINDAKİ yüzey.</summary>
    private static Texture2D ZeminiCek(GameObject canvasGo, List<TMP_Text> metinler)
    {
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null) return null;

        List<bool> eskiDurum = metinler.Select(t => t.enabled).ToList();
        for (int i = 0; i < metinler.Count; i++) metinler[i].enabled = false;

        RenderMode eskiMod = canvas.renderMode;
        Camera eskiKamera = canvas.worldCamera;
        float eskiMesafe = canvas.planeDistance;

        GameObject kameraGo = new GameObject("NexumContrastCamera");
        Camera kamera = kameraGo.AddComponent<Camera>();
        kamera.orthographic = true;
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = new Color(0.02f, 0.05f, 0.08f, 1f);
        kamera.nearClipPlane = 0.1f;
        kamera.farClipPlane = 1000f;
        kamera.transform.position = new Vector3(0f, 0f, -500f);

        RenderTexture rt = new RenderTexture(Boyut.x, Boyut.y, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        kamera.targetTexture = rt;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = kamera;
        canvas.planeDistance = 100f;

        Transform safe = canvasGo.transform.Find("SafeAreaRoot");
        MonoBehaviour safeArea = safe != null
            ? safe.GetComponents<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea")
            : null;
        if (safeArea != null) safeArea.enabled = false;

        Canvas.ForceUpdateCanvases();
        kamera.Render();

        RenderTexture onceki = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D png = new Texture2D(Boyut.x, Boyut.y, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, Boyut.x, Boyut.y), 0, 0);
        png.Apply();
        RenderTexture.active = onceki;

        kamera.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(kameraGo);

        canvas.renderMode = eskiMod;
        canvas.worldCamera = eskiKamera;
        canvas.planeDistance = eskiMesafe;

        for (int i = 0; i < metinler.Count; i++) metinler[i].enabled = eskiDurum[i];
        Canvas.ForceUpdateCanvases();
        return png;
    }

    /// <summary>
    /// Kare tek renk mi? <c>-nographics</c> ile çalıştırıldığında kamera
    /// hiçbir şey çizemiyor ve tüm örnekler aynı griye (#CDCDCD) düşüyor;
    /// tarama o zaman sahnedeki her metni sessizce yanlış düzeltiyor.
    /// </summary>
    private static bool BosKare(Texture2D kare)
    {
        Color a = kare.GetPixel(4, 4);
        Color b = kare.GetPixel(kare.width - 5, 4);
        Color c = kare.GetPixel(4, kare.height - 5);
        Color d = kare.GetPixel(kare.width / 2, kare.height / 2);
        return Kontrast(a, b) < 1.01f && Kontrast(a, c) < 1.01f && Kontrast(a, d) < 1.01f;
    }

    /// <summary>Metnin ekran dikdörtgenindeki MEDYAN yüzey rengi.</summary>
    private static Color? Ornekle(Texture2D zemin, GameObject canvasGo, TMP_Text tmp)
    {
        Vector3[] kose = new Vector3[4];
        tmp.rectTransform.GetWorldCorners(kose);

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        float cw = canvasRect.rect.width, ch = canvasRect.rect.height;
        if (cw <= 0f || ch <= 0f) return null;

        // Dünya -> canvas yerel -> piksel
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Vector3 k in kose)
        {
            Vector3 yerel = canvasRect.InverseTransformPoint(k);
            x0 = Mathf.Min(x0, yerel.x); x1 = Mathf.Max(x1, yerel.x);
            y0 = Mathf.Min(y0, yerel.y); y1 = Mathf.Max(y1, yerel.y);
        }

        int px0 = Mathf.RoundToInt((x0 / cw + 0.5f) * Boyut.x);
        int px1 = Mathf.RoundToInt((x1 / cw + 0.5f) * Boyut.x);
        int py0 = Mathf.RoundToInt((y0 / ch + 0.5f) * Boyut.y);
        int py1 = Mathf.RoundToInt((y1 / ch + 0.5f) * Boyut.y);

        // KARENİN TAMAMEN İÇİNDE DEĞİLSE ÖLÇME. Kırpmak, kare dışına taşan
        // bir nesneyi kenar pikseline eşitliyor: tarif defteri satırları
        // 144'e çıkınca içerik 3200 birime uzadı, görünmeyen satırlar boş
        // kare rengini (#CDCDCD) yüzey sanıp dokuz etiketi yanlış düzeltti.
        if (px0 < 0 || py0 < 0 || px1 >= Boyut.x || py1 >= Boyut.y) return null;

        px0 = Mathf.Clamp(px0, 0, Boyut.x - 1); px1 = Mathf.Clamp(px1, 0, Boyut.x - 1);
        py0 = Mathf.Clamp(py0, 0, Boyut.y - 1); py1 = Mathf.Clamp(py1, 0, Boyut.y - 1);
        if (px1 - px0 < 2 || py1 - py0 < 2) return null;

        List<float> lum = new List<float>();
        List<Color> renk = new List<Color>();
        int adimX = Mathf.Max(1, (px1 - px0) / 24);
        int adimY = Mathf.Max(1, (py1 - py0) / 24);
        for (int y = py0; y <= py1; y += adimY)
        {
            for (int x = px0; x <= px1; x += adimX)
            {
                Color c = zemin.GetPixel(x, y);
                renk.Add(c);
                lum.Add(Luminans(c));
            }
        }
        if (renk.Count == 0) return null;

        // Medyan luminanslı pikselin rengi: ortalama almak farklı yüzeylerin
        // sınırında var olmayan bir ara renk uydururdu.
        int[] sira = Enumerable.Range(0, lum.Count).OrderBy(i => lum[i]).ToArray();
        return renk[sira[sira.Length / 2]];
    }

    // ------------------------------------------------------------------ karar

    /// <summary>
    /// HİYERARŞİ KORUNUR. İlk sürüm eşiği geçen rollerin EN YÜKSEK
    /// kontrastlısını seçiyordu; sonuç her şeyin beyaza çıkması, yani
    /// okunaklı ama düz bir ekran oluyordu. Artık:
    ///
    ///   - mevcut rol zaten eşiği geçiyorsa DOKUNULMAZ,
    ///   - mevcut rol SÖNÜK bir rolse (textSecondary / textMuted /
    ///     textOnLight) eşiği geçen EN SÖNÜK rol seçilir — ikincil metin
    ///     ikincil kalır,
    ///   - mevcut rol textPrimary ise eşiği geçen en parlak rol seçilir.
    /// </summary>
    private static Color Duzelt(Color mevcut, Color yuzey, out string gerekce)
    {
        string rol = RolBul(mevcut);

        if (rol != null && AnlamRolleri.Contains(rol))
        {
            // Anlam taşıyan renk: hue korunur, parlaklık eşiğe kadar çekilir.
            gerekce = $"anlam rengi ({rol}) korundu, parlaklık çekildi";
            return EsigeCek(mevcut, yuzey);
        }

        List<string> uygun = MetinRolleri
            .Where(r => Kontrast(NexumPalette.ByRole(r), yuzey) >= Hedef)
            .OrderBy(r => Kontrast(NexumPalette.ByRole(r), yuzey))
            .ToList();

        if (uygun.Count == 0)
        {
            // Hiçbiri yetmiyor: en iyisini al, yüzey düzeltmesi ayrıca raporlanır.
            string enIyiRol = MetinRolleri
                .OrderByDescending(r => Kontrast(NexumPalette.ByRole(r), yuzey)).First();
            gerekce = $"hiçbir metin rolü eşiği geçmiyor -> en yüksek ({enIyiRol})";
            return NexumPalette.ByRole(enIyiRol);
        }

        bool birincilydi = rol == "textPrimary";
        string secim = birincilydi ? uygun[uygun.Count - 1] : uygun[0];
        gerekce = birincilydi
            ? $"birincil metin korundu -> {secim}"
            : $"hiyerarşi korundu: eşiği geçen en sönük rol -> {secim}";
        return NexumPalette.ByRole(secim);
    }

    /// <summary>Rengi hue'sunu koruyarak siyaha ya da beyaza doğru çeker.</summary>
    private static Color EsigeCek(Color renk, Color yuzey)
    {
        Color kutup = Luminans(yuzey) > 0.18f ? Color.black : Color.white;
        Color enIyi = renk;
        float enIyiK = Kontrast(renk, yuzey);

        for (int i = 1; i <= 40; i++)
        {
            float t = i / 40f;
            Color c = Color.Lerp(renk, kutup, t);
            float k = Kontrast(c, yuzey);
            if (k > enIyiK) { enIyi = c; enIyiK = k; }
            if (k >= Hedef) return c;
        }
        return enIyi;
    }

    private static string RolBul(Color c)
    {
        Color32 a = c;
        foreach (string r in NexumPalette.Roles)
        {
            Color32 b = NexumPalette.ByRole(r);
            if (a.r == b.r && a.g == b.g && a.b == b.b) return r;
        }
        return null;
    }

    // ------------------------------------------------------------------ ölçü

    public static float Luminans(Color c)
    {
        return 0.2126f * Kanal(c.r) + 0.7152f * Kanal(c.g) + 0.0722f * Kanal(c.b);
    }

    private static float Kanal(float v)
    {
        return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    public static float Kontrast(Color a, Color b)
    {
        float la = Luminans(a), lb = Luminans(b);
        if (la < lb) { float t = la; la = lb; lb = t; }
        return (la + 0.05f) / (lb + 0.05f);
    }

    private static string Hex(Color c)
    {
        Color32 x = c;
        return $"#{x.r:X2}{x.g:X2}{x.b:X2}";
    }

    private static string Yol(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    private static string Kisa(string yol)
    {
        return yol.Replace("Canvas/SafeAreaRoot/", "").Replace("Canvas/", "");
    }
}
