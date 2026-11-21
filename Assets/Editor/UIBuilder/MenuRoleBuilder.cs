using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ana menüde HİYERARŞİ: bir birincil eylem, dört ikincil (3c/8).
///
/// Beş buton da aynı ikincil çerçeveyi taşıyordu; ekranda "önce şuna bas"
/// diyen hiçbir şey yoktu. OYNA artık birincil rolü (camgöbeği dolgu)
/// taşıyor, diğer dördü ikincil kalıyor.
///
/// YALNIZCA ROL DEĞİŞİR. Bu kurucu butonların boyutuna, konumuna, yazısına
/// ve bağlantısına DOKUNMAZ — tek yazdığı alan <c>Image.sprite</c>. Ölçüm
/// (<c>MenuHierarchyTests</c>) rect'lerin değişmediğini de doğrular.
///
/// <b>1.3/0.1 — ÜST BANT.</b> Ana menüde NEXUM logosu YOKTU; kimlik yalnızca
/// alt panellerde görünüyordu. Logo üst banda, yatayda ortalı olarak kuruldu.
/// Varlık ÇOĞALTILMADI: diğer panellerdeki sprite'ın aynısı, kırpılmış tuval
/// ve <c>preserveAspect</c> kuralıyla (bkz. 3c/4).
///
/// <b>1.3/0.2 — YUVARLAK AVATAR.</b> Çerçeve kare kit panelindeydi
/// (<c>avatar_frame_normal</c>); simge ailesinin yuvarlak metal panosuna
/// geçti (<c>compound_ring_normal</c> — AYNI üreteç, yeni görsel yok).
/// Portre dairesel kırpılıyor: <c>mask_circle</c> + Unity <c>Mask</c>
/// (maske çizilmez, yalnızca kırpar).
///
/// Üst bandın yerleşimi ÖLÇÜLDÜ: logo 789..947, avatar 540..740, ilk buton
/// (OYNA) 274..516 — üçü de çakışmıyor, avatarın sol kenarı buton bandının
/// sol kenarıyla (-300) aynı hizada.
///
/// Menüden: NEXUM → Menü Hiyerarşisini Kur
/// Batch:   -executeMethod MenuRoleBuilder.Run
/// </summary>
public static class MenuRoleBuilder
{
    private const string Tag = "NEXUM_MENUROL";

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PanelPath = "Canvas/SafeAreaRoot/MainMenuPanel";

    private const string PrimaryPath = "Assets/Art/UI/btn_primary_normal.png";
    private const string SecondaryPath = "Assets/Art/UI/btn_secondary_normal.png";

    /// <summary>Birincil eylem: tek.</summary>
    private const string Primary = "PlayButton";

    private static readonly string[] Secondary =
    {
        "HowToPlayButton", "EncyclopediaButton", "RecipeBookButton", "SettingButton",
    };

    private const string LogoPath = "Assets/Art/Figures/NEXUM_logo_metal.png";
    private const string RingPath = "Assets/Art/UI/compound_ring_normal.png";
    private const string MaskPath = "Assets/Art/UI/mask_circle.png";

    /// <summary>Logo: üst bant, yatayda ortalı. Boyut sprite oranından gelir.</summary>
    private static readonly Vector2 LogoSize = new Vector2(500f, 157f);
    private static readonly Vector2 LogoPos = new Vector2(0f, -92f);

    /// <summary>
    /// Avatar logonun ALTINDA: 200x200, sol kenarı buton bandının sol
    /// kenarında. 300'den 200'e indi — logo üst bandı aldı; 200 dokunma
    /// hedefi alt sınırının (144) üstünde kalıyor.
    /// </summary>
    private static readonly Vector2 AvatarSize = new Vector2(200f, 200f);
    private static readonly float AvatarCenterY = -320f;

    /// <summary>Portrenin maskesi çerçevenin kenarlığının içinde kalır.</summary>
    private const float RingInset = 0.08f;

    [MenuItem("NEXUM/Menü Hiyerarşisini Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("MenuRole", ScenePath, dryRun);

        Sprite birincil = AssetDatabase.LoadAssetAtPath<Sprite>(PrimaryPath);
        Sprite ikincil = AssetDatabase.LoadAssetAtPath<Sprite>(SecondaryPath);
        if (birincil == null || ikincil == null)
        {
            b.Error("kit buton sprite'ları bulunamadı");
            b.End();
            return;
        }

        Rol(b, Primary, birincil);
        foreach (string ad in Secondary) Rol(b, ad, ikincil);

        Logo(b);
        Avatar(b);

        b.End();
    }

    private static void Rol(UIBuilder b, string ad, Sprite sprite)
    {
        GameObject go = b.FindObject(PanelPath + "/" + ad);
        if (go == null) { b.Error($"buton yok: {ad}"); return; }

        Image img = go.GetComponent<Image>();
        if (img == null) { b.Error($"buton Image taşımıyor: {ad}"); return; }

        if (img.sprite != sprite)
        {
            Debug.Log($"{Tag}_ROL: {ad}: {(img.sprite != null ? img.sprite.name : "yok")} " +
                      $"-> {sprite.name}");
        }

        // SADECE sprite: rect'e, metne, bağlantıya dokunulmaz
        b.Apply(go, img, i =>
        {
            i.sprite = sprite;
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = Color.white;
        });
    }

    /// <summary>
    /// Logo: üst bantta, yatayda ORTALI. Varlık çoğaltılmaz — diğer
    /// panellerdeki sprite'ın aynısı. Kutu sprite'ın oranından kurulur,
    /// <c>preserveAspect</c> açık (3c/4 kuralı).
    /// </summary>
    private static void Logo(UIBuilder b)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        if (sprite == null) { b.Error($"logo sprite'ı yok: {LogoPath}"); return; }

        GameObject logo = b.Ensure(PanelPath, "Logo");
        if (logo == null) return;

        b.SetFixed(logo, new Vector2(0.5f, 1f), LogoSize, LogoPos);
        b.EnsureComponent<Image>(logo, img =>
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;
        });

        float oran = sprite.rect.width / sprite.rect.height;
        Debug.Log($"{Tag}_LOGO: kutu {LogoSize.x:F0}x{LogoSize.y:F0} " +
                  $"(sprite oranı {oran:F4} -> {LogoSize.x / oran:F0} birim yükseklik), " +
                  $"merkez x=0, üst bant {-LogoPos.y - LogoSize.y * 0.5f:F0}..{-LogoPos.y + LogoSize.y * 0.5f:F0}");
    }

    /// <summary>
    /// Avatar: YUVARLAK metal çerçeve, portre dairesel kırpılmış, sol kenarı
    /// buton bandının sol kenarında ve logonun ALTINDA.
    /// </summary>
    private static void Avatar(UIBuilder b)
    {
        GameObject avatar = b.FindObject(PanelPath + "/ProfileButton");
        if (avatar == null) { b.Error("ProfileButton yok"); return; }

        // Bandın sol kenarı: butonların en solu (ölçülür, sabit yazılmaz)
        List<string> butonlar = new List<string>(Secondary) { Primary };
        float solKenar = butonlar
            .Select(a => b.FindObject(PanelPath + "/" + a))
            .Where(g => g != null)
            .Select(g => g.GetComponent<RectTransform>())
            .Where(r => r != null)
            .Select(r => r.anchoredPosition.x - r.sizeDelta.x * 0.5f)
            .DefaultIfEmpty(0f)
            .Min();

        // ÜST-ORTA anchor: band ekran merkezine göre dizili, avatar da öyle
        // olmalı — sol kenara anchor'lanırsa 1440 genişlikte band yerinde
        // kalır, avatar 30 birim kayardı.
        Vector2 hedef = new Vector2(solKenar + AvatarSize.x * 0.5f, AvatarCenterY);
        b.SetFixed(avatar, new Vector2(0.5f, 1f), AvatarSize, hedef);

        Sprite halka = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
        if (halka == null) { b.Error($"çerçeve sprite'ı yok: {RingPath}"); return; }

        b.EnsureComponent<Image>(avatar, img =>
        {
            img.sprite = halka;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;
        });

        // Dairesel kırpma: maske ÇİZİLMEZ, yalnızca kırpar
        GameObject maske = b.Ensure(PanelPath + "/ProfileButton", "PortraitMask");
        if (maske == null) return;

        b.SetStretch(maske, new Vector2(RingInset, RingInset),
                     new Vector2(1f - RingInset, 1f - RingInset));
        b.EnsureComponent<Image>(maske, img =>
        {
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MaskPath);
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
            img.raycastTarget = false;
        });
        b.EnsureComponent<Mask>(maske, m => m.showMaskGraphic = false);

        // Portre maskenin altına taşınır; ProfileManager referansı korunur
        GameObject portre = b.FindObject(PanelPath + "/ProfileButton/ProfileAvatarImage")
                            ?? b.FindObject(PanelPath + "/ProfileButton/PortraitMask/ProfileAvatarImage");
        if (portre != null)
        {
            b.Reparent(portre, maske);
            b.SetStretch(portre, UITokens.Zero, UITokens.One);
            Image pimg = portre.GetComponent<Image>();
            if (pimg != null)
            {
                b.Apply(portre, pimg, img =>
                {
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                });
            }
        }

        Debug.Log($"{Tag}_AVATAR: band sol kenarı {solKenar:F0}, avatar " +
                  $"{AvatarSize.x:F0}x{AvatarSize.y:F0} -> {hedef.x:F0},{hedef.y:F0}; " +
                  $"yuvarlak çerçeve {halka.name}, portre maskeli");
    }
}
