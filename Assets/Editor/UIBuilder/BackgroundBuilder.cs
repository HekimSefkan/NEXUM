using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TEK ARKA PLAN + ÜZERİNDE PROSEDÜREL İŞLEME KATMANI (3c/5).
///
/// Oyunda altı ayrı arka plan görseli dolaşıyordu (menü, kayıt, bölüm
/// haritası, nasıl oynanır, oyun). Hepsi aynı işi yapıyordu ama hiçbiri
/// diğeriyle aynı değildi: ekrandan ekrana geçerken zemin değişiyordu.
/// Artık TEK kaynak var — <c>Assets/Art/Backgrounds/nexum_background.png</c>
/// (941x1672 kaynaktan LANCZOS ile 2161x3840'a büyütülmüş, oran korunmuş).
///
/// İŞLEME GÖRSELİN İÇİNE PİŞMEZ. Karartma, ölçü ızgarası, panel şeridi ve
/// vinyet sahnede ayrı Image katmanlarıdır; parametreleri aşağıda, tek
/// yerde. Bir ekranın zemini koyulaşsın isteniyorsa görsel yeniden
/// üretilmez, <see cref="KarartmaAlfa"/> değişir.
///
/// Katman sırası (alttan üste): arka plan görseli · karartma · ızgara ·
/// panel şeridi · vinyet. Hepsi raycast'e kapalı; alfası 0 olan katman
/// kapatılır (boş kare için draw call harcanmaz).
///
/// Menüden: NEXUM → Arka Planı Kur
/// Batch:   -executeMethod BackgroundBuilder.Run
/// </summary>
public static class BackgroundBuilder
{
    private const string Tag = "NEXUM_ARKAPLAN";

    private const string BackgroundPath = "Assets/Art/Backgrounds/nexum_background.png";
    private const string VignettePath = "Assets/Art/UI/bg_vignette.png";
    private const string GridPath = "Assets/Art/UI/bg_grid.png";
    private const string StripPath = "Assets/Art/UI/panel_md_normal.png";

    private const string TreatmentName = "BackgroundTreatment";

    /// <summary>
    /// 1440 genişlikte arka plan 1440x2560 piksel kaplar; import kuralının
    /// 1,5 katı 3840 -> 4096. Tek dokudur ve altı dokunun yerini alır.
    /// </summary>
    private const int MaxTextureSize = 4096;

    // --- İŞLEME PARAMETRELERİ (ölçümle belirlenir, görsele pişmez) --------

    /// <summary>Düz karartma örtüsü. 0 = kapalı. Ekran parlaklığı bununla tutulur.</summary>
    public const float KarartmaAlfa = 0f;

    /// <summary>Ölçü ızgarası: 192 birimlik karo, kaynağın kendi ızgarasından bağımsız.</summary>
    private const float IzgaraAlfa = 0.055f;

    /// <summary>İçeriğin oturduğu dikey şerit (kit paneli).</summary>
    private const float SeritAlfa = 0.16f;
    private const float SeritGenislik = 1000f;

    /// <summary>Kenar koyulaştırma: merkez açık kalır, köşeler kapanır.</summary>
    private const float VinyetAlfa = 0.55f;

    /// <summary>Yerini alacağı eski arka planlar.</summary>
    private static readonly string[] EskiArkaPlanlar =
    {
        "Assets/Art/Figures/background_c_main.png",
        "Assets/Art/Figures/arkaplan.png",
        "Assets/Art/Figures/Registration_arkaplan.png",
        "Assets/Art/Figures/Nasiloyanir_arkaplan1.png",
        "Assets/Art/Figures/Nasiloyanir_arkaplan2.png",
        "Assets/Art/Figures/level_selection_arkaplan.png",
        "Assets/Art/Figures/background_c_level.png",
        "Assets/Art/Figures/background_c_register.png",
    };

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    [MenuItem("NEXUM/Arka Planı Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        Sprite yeni = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (yeni == null)
        {
            Debug.LogError($"{Tag}: arka plan yok -> {BackgroundPath} " +
                           "(önce python Tools/upscale_background.py)");
            return;
        }

        if (!dryRun) ImportAyari();

        HashSet<Sprite> eski = new HashSet<Sprite>(
            EskiArkaPlanlar.Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s => s != null));
        eski.Add(yeni);   // zaten geçmiş olanlar da aynı kurala tabi

        float oran = yeni.rect.width / yeni.rect.height;
        NexumTheme tema = UITokens.Theme;
        int zemin = 0, katman = 0;

        foreach (string yol in Scenes)
        {
            UIBuilder b = UIBuilder.Begin("Background", yol, dryRun);

            List<Image> arkaPlanlar = Object.FindObjectsOfType<Image>(true)
                .Where(i => i != null && i.sprite != null && eski.Contains(i.sprite))
                .OrderBy(i => Yol(i.transform))
                .ToList();

            foreach (Image img in arkaPlanlar)
            {
                if (img.sprite != yeni)
                {
                    Debug.Log($"{Tag}_KAYNAK: {Yol(img.transform)}: {img.sprite.name} -> nexum_background");
                }

                b.Apply(img.gameObject, img, i =>
                {
                    i.sprite = yeni;
                    i.type = Image.Type.Simple;
                    i.preserveAspect = false;
                    i.color = Color.white;
                });

                // Envelope Parent: ekranı tamamen kaplar, oran bozulmaz
                b.EnsureComponent<AspectRatioFitter>(img.gameObject, arf =>
                {
                    arf.enabled = true;
                    arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    arf.aspectRatio = oran;
                });

                zemin++;
                if (Katman(b, tema, img, dryRun)) katman++;
            }

            b.End();
        }

        if (!dryRun) AssetDatabase.SaveAssets();

        Debug.Log($"{Tag}: zemin {zemin}, işleme katmanı {katman} " +
                  $"(oran {oran:F6}, karartma {KarartmaAlfa:F2}, vinyet {VinyetAlfa:F2}, " +
                  $"ızgara {IzgaraAlfa:F3}, şerit {SeritAlfa:F2})");
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>
    /// İşleme katmanı zeminin KENDİ ÇOCUĞUDUR, kardeşi değil.
    ///
    /// İlk sürüm kardeş koyuyordu ve bazı panellerde zemin panelin KÖKÜ
    /// oluyordu (ProfilePanel, HowToPlayPanel): katman o zaman panelin
    /// dışına, SafeAreaRoot'un altına düşüyor ve panel kapalıyken bile
    /// ekranda kalıyordu. Çocuk olarak katman panelle birlikte açılıp
    /// kapanır. Çocuk indeksi 0: panelin kendi içeriğinin ALTINDA kalır.
    /// </summary>
    private static bool Katman(UIBuilder b, NexumTheme tema, Image arkaPlan, bool dryRun)
    {
        Transform ebeveyn = arkaPlan.transform.parent;
        if (ebeveyn != null) b.Remove(Yol(ebeveyn), TreatmentName);   // eski kardeş sürüm

        string zeminYolu = Yol(arkaPlan.transform);
        GameObject kok = b.Ensure(zeminYolu, TreatmentName, adopt: true);
        if (kok == null) return false;       // dry-run ya da hata

        b.SetStretch(kok, UITokens.Zero, UITokens.One);
        if (kok.transform.GetSiblingIndex() != 0) kok.transform.SetSiblingIndex(0);

        string kokYolu = zeminYolu + "/" + TreatmentName;

        Katmanci(b, kok, kokYolu, "Darken", null, Image.Type.Simple,
                 Renk(tema.palette.cardBackground, KarartmaAlfa), null);

        Katmanci(b, kok, kokYolu, "Grid", GridPath, Image.Type.Tiled,
                 Renk(tema.palette.primary, IzgaraAlfa), null);

        Katmanci(b, kok, kokYolu, "PanelStrip", StripPath, Image.Type.Sliced,
                 Renk(tema.palette.panelBackground, SeritAlfa), rt =>
                 {
                     rt.anchorMin = new Vector2(0.5f, 0f);
                     rt.anchorMax = new Vector2(0.5f, 1f);
                     rt.pivot = new Vector2(0.5f, 0.5f);
                     rt.sizeDelta = new Vector2(SeritGenislik, 0f);
                     rt.anchoredPosition = Vector2.zero;
                 });

        Katmanci(b, kok, kokYolu, "Vignette", VignettePath, Image.Type.Simple,
                 Renk(tema.palette.cardBackground, VinyetAlfa), null);

        return true;
    }

    private static void Katmanci(UIBuilder b, GameObject kok, string kokYolu, string ad,
                                 string spriteYolu, Image.Type tip, Color renk,
                                 System.Action<RectTransform> yerlesim)
    {
        GameObject go = b.Ensure(kokYolu, ad, adopt: true);
        if (go == null) return;

        if (yerlesim == null) b.SetStretch(go, UITokens.Zero, UITokens.One);
        else yerlesim(go.GetComponent<RectTransform>());

        Sprite s = spriteYolu == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(spriteYolu);
        b.EnsureComponent<Image>(go, img =>
        {
            img.sprite = s;
            img.type = tip;
            img.color = renk;
            img.raycastTarget = false;
            img.preserveAspect = false;
        });

        // Alfası sıfır katman çizilmez: boş kare için draw call harcanmaz
        bool acik = renk.a > 0.001f;
        if (go.activeSelf != acik) go.SetActive(acik);
    }

    private static Color Renk(Color rol, float alfa)
    {
        return new Color(rol.r, rol.g, rol.b, alfa);
    }

    /// <summary>Tek arka plan dokusunun import ayarı.</summary>
    private static void ImportAyari()
    {
        TextureImporter imp = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (imp == null) return;

        bool d = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; d = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; d = true; }
        if (imp.mipmapEnabled) { imp.mipmapEnabled = false; d = true; }
        if (imp.isReadable) { imp.isReadable = false; d = true; }
        if (imp.maxTextureSize != MaxTextureSize) { imp.maxTextureSize = MaxTextureSize; d = true; }

        TextureImporterPlatformSettings varsayilan = imp.GetDefaultPlatformTextureSettings();
        if (varsayilan.maxTextureSize != MaxTextureSize ||
            varsayilan.textureCompression != TextureImporterCompression.Compressed)
        {
            varsayilan.maxTextureSize = MaxTextureSize;
            varsayilan.textureCompression = TextureImporterCompression.Compressed;
            varsayilan.format = TextureImporterFormat.Automatic;
            imp.SetPlatformTextureSettings(varsayilan);
            d = true;
        }

        if (!d) return;
        imp.SaveAndReimport();
        Debug.Log($"{Tag}: import ayarı yazıldı (maxTextureSize {MaxTextureSize})");
    }

    private static string Yol(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
