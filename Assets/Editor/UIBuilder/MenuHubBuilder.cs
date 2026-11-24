using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ANA MENÜ HUB'I (1.7).
///
/// Eski hâl: ortada dikey sıralanmış beş eşit buton. Hangisinin birincil
/// eylem olduğu belli değildi, oyuncunun kaldığı yer hiçbir yerde
/// yazmıyordu ve yeni bir giriş eklemek beş butonun y değerlerini elle
/// yeniden hesaplamak demekti.
///
/// Yeni düzen yukarıdan aşağı: logo · profil şeridi · kahraman kart ·
/// ikili kart · mod satırı · bilgi satırı · ikon şeridi.
///
/// <b>Yerleşim akar.</b> Satırlar <c>VerticalLayoutGroup</c>, satır içi
/// kartlar <c>HorizontalLayoutGroup</c> ile dizilir; her çocukta AÇIK
/// <c>LayoutElement</c> var. Açık LayoutElement şart: sliced bir Image'in
/// preferred değeri dokunun genişliği değil KENARLIK TOPLAMIDIR, layout
/// grubu onu okursa kart çöker (CLAUDE.md, SlicedCollapseTests).
/// Gizlenen giriş layout'tan düşer, satır kendiliğinden yeniden akar.
///
/// <b>Mevcut beş butonun bağına DOKUNULMAZ.</b> PlayButton,
/// HowToPlayButton, EncyclopediaButton, RecipeBookButton ve SettingButton
/// devralınır (adopt) ve yeni yerlerine taşınır; OnClick listeleri olduğu
/// gibi kalır. Kurucu hiçbir OnClick silmez. Bunu
/// <c>MenuHubBehaviourTests</c> ölçüyor.
///
/// Menüden: NEXUM → Ana Menü Hub'ını Kur
/// Batch:   -executeMethod MenuHubBuilder.Run
/// </summary>
public static class MenuHubBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PanelPath = "Canvas/SafeAreaRoot/MainMenuPanel";
    private const string RootName = "HubRoot";

    private static string RootPath { get { return PanelPath + "/" + RootName; } }

    private const string IconDir = "Assets/Art/Icons/UI";
    private const string CardFramePath = "Assets/Art/UI/panel_md_normal.png";
    private const string HeroFramePath = "Assets/Art/UI/panel_lg_normal.png";
    private const string StripFramePath = "Assets/Art/UI/panel_sm_normal.png";
    private const string RingPath = "Assets/Art/UI/compound_ring_normal.png";
    private const string StarEmptyPath = "Assets/Art/UI/star_empty_normal.png";
    private const string BarPath = "Assets/Art/UI/progress_fill_selected.png";

    // --- dikey ritim ------------------------------------------------------
    // Hepsi açıkken toplam: 130+150+300+230+180+190+190 = 1370, 6 aralık
    // x24 = 144, dolgu 2x60 = 120 -> 1634 < 1920. Kaydırma GEREKMEZ.
    // Bugünkü bayraklarla mod satırı düşer: 1634-180-24 = 1430.
    private const float LogoH = 130f;
    private const float ProfileH = 150f;
    private const float HeroH = 300f;
    private const float DualH = 230f;
    private const float ModeH = 180f;
    private const float InfoH = 190f;
    private const float IconsH = 190f;
    private const float RowSpacing = 24f;
    private const float SidePad = 40f;
    private const float TopPad = 60f;

    private static readonly Vector2 IconSize = new Vector2(132f, 132f);

    [MenuItem("NEXUM/Ana Menü Hub'ını Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("MenuHub", ScenePath, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject panel = b.FindObject(PanelPath);
        if (panel == null) { b.Error("MainMenuPanel yok: " + PanelPath); b.End(); return; }

        // --- kök: dikey akış
        GameObject root = b.Ensure(PanelPath, RootName);
        if (root == null) { b.End(); return; }
        b.SetStretch(root, UITokens.Zero, UITokens.One);
        b.EnsureComponent<VerticalLayoutGroup>(root, v =>
        {
            v.padding = new RectOffset((int)SidePad, (int)SidePad, (int)TopPad, (int)TopPad);
            v.spacing = RowSpacing;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childScaleWidth = false;
            v.childScaleHeight = false;
        });

        // --- 1) logo (mevcut varlık devralınır)
        GameObject logo = Devral(b, "Logo");
        if (logo != null)
        {
            Tasi(logo, root);
            Yukseklik(b, logo, LogoH);
            b.EnsureComponent<Image>(logo, i => i.preserveAspect = true);
        }

        // --- 2) profil şeridi
        GameObject profile = Satir(b, theme, "ProfileStrip", ProfileH, StripFramePath);
        GameObject avatar = Devral(b, "ProfileButton");
        if (avatar != null)
        {
            Tasi(avatar, profile);
            b.SetFixed(avatar, new Vector2(0f, 0.5f), new Vector2(120f, 120f), new Vector2(84f, 0f));
            LayoutDisi(b, avatar);   // şeritte elle konumlanır, layout sürmesin
        }

        GameObject nameText = Metin(b, theme, profile, "PlayerNameText", new Vector2(420f, 52f),
                                    new Vector2(-70f, 28f), theme.typography.subtitle,
                                    theme.palette.textPrimary, TextAlignmentOptions.Left);
        LayoutDisi(b, nameText);

        GameObject rankXp = b.Ensure(RootPath + "/ProfileStrip", "RankXpGroup");
        // Şerit 920 geniş (-460..460). Seri rozeti sağda 244..444'ü tutuyor;
        // grup ve çubuk onun soluna sığacak şekilde ölçüldü.
        b.SetFixed(rankXp, UITokens.Center, new Vector2(460f, 56f), new Vector2(-30f, -30f));
        Gizle(b, rankXp);
        GameObject rankText = Metin(b, theme, rankXp, "RankText", new Vector2(200f, 40f),
                                    new Vector2(-150f, 0f), theme.typography.label,
                                    theme.palette.textSecondary, TextAlignmentOptions.Left);
        GameObject xpBar = b.Ensure(RootPath + "/ProfileStrip/RankXpGroup", "XpBar");
        // 20 birim yükseklik progress_fill_selected'ın 17+17 kenarlığına
        // SIĞMIYOR: orta alan negatife düşüyor, 9-dilim çöküyor
        // (SlicedCollapseTests ölçtü). 56 birimde orta alan 22.
        b.SetFixed(xpBar, UITokens.Center, new Vector2(240f, 56f), new Vector2(60f, 0f));
        b.EnsureComponent<Image>(xpBar, i =>
        {
            i.sprite = LoadSprite(BarPath);
            i.type = Image.Type.Sliced;
            i.color = Color.white;
            i.raycastTarget = false;
        });

        GameObject streak = b.Ensure(RootPath + "/ProfileStrip", "StreakBadge");
        b.SetFixed(streak, new Vector2(1f, 0.5f), new Vector2(200f, 72f), new Vector2(-116f, 0f));
        Gizle(b, streak);
        b.EnsureComponent<Image>(streak, i =>
        {
            i.sprite = LoadSprite(StripFramePath);
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = Color.white;
            i.raycastTarget = false;
        });
        Metin(b, theme, streak, "StreakText", new Vector2(180f, 44f), Vector2.zero,
              theme.typography.label, theme.palette.warning, TextAlignmentOptions.Center);

        // --- 3) kahraman kart: mevcut OYNA eylemi
        GameObject hero = Devral(b, "PlayButton");
        GameObject heroStatus = null, heroStars = null;
        if (hero != null)
        {
            Tasi(hero, root);
            Yukseklik(b, hero, HeroH);
            Cerceve(b, hero, HeroFramePath);

            GameObject heroLabel = DevralCocuk(b, hero, "Label");
            if (heroLabel != null)
            {
                // 50 punto 70 birimlik kutuda %90 dolduruyordu (ölçüldü)
                // Yıldızlar 130..430'u tutuyor; metin kutuları onlara
                // giriyordu (ölçüldü: 1500 ve 7200 birim² çakışma).
                // Kart 1000 geniş (-500..500). Yıldızlar sağda 220..460'ı
                // tutuyor; metin kutuları 200'de bitiyor, 20 birim pay var.
                // (Ölçüm: 570x-145 kutusu 140'a taşıp yıldızlara giriyordu.)
                b.SetFixed(heroLabel, UITokens.Center, new Vector2(640f, 90f), new Vector2(-120f, 70f));
                Yazi(b, theme, heroLabel, theme.typography.title, theme.palette.textPrimary,
                     TextAlignmentOptions.Left);
                Yerellestir(b, heroLabel, CodeStrings.HubHero);
            }

            heroStatus = Metin(b, theme, hero, "HeroStatusText", new Vector2(640f, 48f),
                               new Vector2(-120f, -10f), theme.typography.body,
                               theme.palette.primaryBright, TextAlignmentOptions.Left);

            heroStars = Yildizlar(b, hero, "HeroStars", new Vector2(340f, -10f));
            Gizle(b, heroStars);
        }

        // --- 4) ikili kart
        GameObject dual = Satir(b, theme, "DualRow", DualH, null);
        Yatay(b, dual, 24f);

        GameObject daily = Kart(b, theme, dual, "DailyPuzzleCard", CodeStrings.MenuDailyPuzzle, null);
        // Satır 920 geniş, iki kart + 24 aralık -> 448x230
        Gizle(b, daily, new Vector2(448f, 230f));

        GameObject levelCard = Kart(b, theme, dual, "LevelSelectCard",
                                    CodeStrings.MenuLevelSelect, CodeStrings.HubLevelStatus);
        GameObject levelStatus = b.FindObject(Yol(dual) + "/LevelSelectCard/StatusText");

        // --- 5) mod satırı (Faz 5, üçü de gizli)
        GameObject modes = Satir(b, theme, "ModeRow", ModeH, null);
        Yatay(b, modes, 20f);
        // Üç kart 920'yi paylaşıyor: her biri ~293 geniş. Etiket kutusu
        // kartın içinde kalmalı, uzun adlar iki satıra kırılır
        // ("KAYNAMA NOKTASI" tek satırda kartı 380'de taşırıyordu).
        // 40 puntoda "HAFTANIN DENEYİ" iki satırda %146 dolduruyordu
        // (ölçüldü). Mod kartlarının etiketi 30 punto, kutu 250x120.
        Vector2 modEtiket = new Vector2(250f, 120f);
        GameObject boiling = Kart(b, theme, modes, "BoilingPointCard",
                                  CodeStrings.MenuBoilingPoint, null, modEtiket, true);
        GameObject endless = Kart(b, theme, modes, "EndlessLabCard",
                                  CodeStrings.MenuEndlessLab, null, modEtiket, true);
        GameObject weekly = Kart(b, theme, modes, "WeeklyExperimentCard",
                                 CodeStrings.MenuWeeklyExperiment, null, modEtiket, true);
        // Üç kart + iki 20 aralık -> 293x180
        Vector2 modOlcu = new Vector2(293f, 180f);
        Gizle(b, boiling, modOlcu); Gizle(b, endless, modOlcu); Gizle(b, weekly, modOlcu);
        Gizle(b, modes);

        // --- 6) bilgi satırı: ansiklopedi + tarif defteri (eşit ağırlık)
        GameObject info = Satir(b, theme, "InfoRow", InfoH, null);
        Yatay(b, info, 24f);

        GameObject enc = Devral(b, "EncyclopediaButton");
        if (enc != null)
        {
            Tasi(enc, info);
            Esnek(b, enc);
            Cerceve(b, enc, CardFramePath);
            GameObject encLabel = DevralCocuk(b, enc, "Label");
            if (encLabel != null)
            {
                b.SetStretch(encLabel, UITokens.Zero, UITokens.One);
                Yazi(b, theme, encLabel, theme.typography.subtitle, theme.palette.textPrimary,
                     TextAlignmentOptions.Center);
                Yerellestir(b, encLabel, CodeStrings.HubEncyclopedia);
            }
        }

        GameObject book = Devral(b, "RecipeBookButton");
        GameObject bookStatus = null;
        if (book != null)
        {
            Tasi(book, info);
            Esnek(b, book);
            Cerceve(b, book, CardFramePath);
            GameObject bookLabel = DevralCocuk(b, book, "Label");
            if (bookLabel != null)
            {
                b.SetFixed(bookLabel, UITokens.Center, new Vector2(400f, 56f), new Vector2(0f, 26f));
                Yazi(b, theme, bookLabel, theme.typography.subtitle, theme.palette.textPrimary,
                     TextAlignmentOptions.Center);
                Yerellestir(b, bookLabel, CodeStrings.HubRecipeBook);
            }
            bookStatus = Metin(b, theme, book, "StatusText", new Vector2(400f, 42f),
                               new Vector2(0f, -32f), theme.typography.label,
                               theme.palette.primaryBright, TextAlignmentOptions.Center);
        }

        // --- 7) ikon şeridi
        GameObject icons = Satir(b, theme, "IconStrip", IconsH, null);
        Yatay(b, icons, 16f);

        GameObject settings = Devral(b, "SettingButton");
        Simge(b, theme, settings, icons, "icon_settings", CodeStrings.HubSettings);

        GameObject howTo = Devral(b, "HowToPlayButton");
        Simge(b, theme, howTo, icons, "icon_hint", CodeStrings.HubHowToPlay);

        GameObject mentor = b.Ensure(Yol(icons), "MentorButton");
        Simge(b, theme, mentor, icons, "icon_person", CodeStrings.HubMentor);
        Gizle(b, mentor, IconSize);

        GameObject sound = b.Ensure(Yol(icons), "SoundButton");
        Simge(b, theme, sound, icons, "icon_sound_on", CodeStrings.HubSound);

        // --- denetleyici
        MenuHub hub = b.EnsureComponent<MenuHub>(root, h =>
        {
            h.playerNameText = Tmp(nameText);
            h.rankXpGroup = rankXp;
            h.streakBadge = streak;
            h.heroStatusText = Tmp(heroStatus);
            h.heroStars = heroStars;
            h.levelSelectCard = levelCard;
            h.levelSelectStatusText = Tmp(levelStatus);
            h.dailyPuzzleCard = daily;
            h.modeRow = modes;
            h.boilingPointCard = boiling;
            h.endlessLabCard = endless;
            h.weeklyExperimentCard = weekly;
            h.recipeBookCard = book;
            h.recipeBookStatusText = Tmp(bookStatus);
            h.mentorButton = mentor;
            h.soundIconImage = sound != null ? sound.GetComponent<Image>() : null;
            h.soundOnSprite = LoadSprite($"{IconDir}/icon_sound_on_normal.png");
            h.soundOffSprite = LoadSprite($"{IconDir}/icon_sound_off_normal.png");
        });

        if (!dryRun)
        {
            MainMenuManager menu = Object.FindObjectOfType<MainMenuManager>(true);

            // YENİ girişler bağlanır; devralınanların bağına DOKUNULMAZ.
            Button levelButton = levelCard != null ? levelCard.GetComponent<Button>() : null;
            if (menu != null && levelButton != null) Bagla(levelButton, menu, menu.OpenLevelSelection);

            Button soundButton = sound != null ? sound.GetComponent<Button>() : null;
            if (hub != null && soundButton != null) Bagla(soundButton, hub, hub.ToggleMute);
        }

        b.End();
    }

    // ------------------------------------------------------------- parçalar

    /// <summary>Dikey akıştaki bir satır.</summary>
    private static GameObject Satir(UIBuilder b, NexumTheme theme, string ad, float yukseklik,
                                    string cerceve)
    {
        GameObject row = b.Ensure(RootPath, ad);
        if (row == null) return null;

        Yukseklik(b, row, yukseklik);
        if (!string.IsNullOrEmpty(cerceve)) Cerceve(b, row, cerceve);
        return row;
    }

    private static void Yatay(UIBuilder b, GameObject row, float spacing)
    {
        if (row == null) return;
        b.EnsureComponent<HorizontalLayoutGroup>(row, h =>
        {
            h.padding = new RectOffset(0, 0, 0, 0);
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            h.childScaleWidth = false;
            h.childScaleHeight = false;
        });
    }

    /// <summary>Başlık + (varsa) durum satırı taşıyan kart.</summary>
    private static GameObject Kart(UIBuilder b, NexumTheme theme, GameObject row, string ad,
                                   string baslikKey, string durumKey, Vector2 etiketOlcusu = default,
                                   bool kucukPunto = false)
    {
        if (row == null) return null;

        GameObject card = b.Ensure(Yol(row), ad);
        if (card == null) return null;

        Esnek(b, card);
        Cerceve(b, card, CardFramePath);
        b.EnsureComponent<Button>(card, btn => btn.transition = Selectable.Transition.ColorTint);

        bool durumVar = !string.IsNullOrEmpty(durumKey);
        Vector2 olcu = etiketOlcusu == default(Vector2) ? new Vector2(380f, 56f) : etiketOlcusu;

        GameObject label = b.Ensure(Yol(row) + "/" + ad, "Label");
        b.SetFixed(label, UITokens.Center, olcu, new Vector2(0f, durumVar ? 26f : 0f));
        Yazi(b, theme, label,
             kucukPunto ? theme.typography.label : theme.typography.subtitle,
             theme.palette.textPrimary, TextAlignmentOptions.Center);
        Yerellestir(b, label, baslikKey);

        if (durumVar)
        {
            GameObject status = b.Ensure(Yol(row) + "/" + ad, "StatusText");
            b.SetFixed(status, UITokens.Center, new Vector2(380f, 42f), new Vector2(0f, -32f));
            Yazi(b, theme, status, theme.typography.label, theme.palette.primaryBright,
                 TextAlignmentOptions.Center);
        }

        return card;
    }

    /// <summary>İkon şeridindeki dairesel buton + altında etiket.</summary>
    private static void Simge(UIBuilder b, NexumTheme theme, GameObject go, GameObject strip,
                              string icon, string locKey)
    {
        if (go == null || strip == null) return;

        Tasi(go, strip);
        Esnek(b, go);

        // Grafik 132, dokunma alanını NexumTouchTargets 144'e tamamlar.
        b.EnsureComponent<Image>(go, i =>
        {
            i.sprite = LoadSprite($"{IconDir}/{icon}_normal.png");
            i.type = Image.Type.Simple;
            i.color = Color.white;
            i.preserveAspect = true;
            i.raycastTarget = true;
        });
        b.EnsureComponent<Button>(go, btn => btn.transition = Selectable.Transition.ColorTint);

        GameObject label = DevralCocuk(b, go, "Label");
        if (label == null) label = b.Ensure(Yol(strip) + "/" + go.name, "Label");
        b.SetFixed(label, new Vector2(0.5f, 0f), new Vector2(220f, 40f), new Vector2(0f, -14f));
        Yazi(b, theme, label, theme.typography.label, theme.palette.textSecondary,
             TextAlignmentOptions.Center);
        Yerellestir(b, label, locKey);
    }

    private static GameObject Yildizlar(UIBuilder b, GameObject parent, string ad, Vector2 pos)
    {
        GameObject row = b.Ensure(Yol(parent), ad);
        if (row == null) return null;

        b.SetFixed(row, UITokens.Center, new Vector2(240f, 90f), pos);
        for (int i = 0; i < 3; i++)
        {
            GameObject star = b.Ensure(Yol(parent) + "/" + ad, "Star" + i);
            b.SetFixed(star, UITokens.Center, new Vector2(72f, 72f), new Vector2((i - 1) * 84f, 0f));
            b.EnsureComponent<Image>(star, img =>
            {
                img.sprite = LoadSprite(StarEmptyPath);
                img.type = Image.Type.Simple;
                img.color = Color.white;
                img.preserveAspect = true;
                img.raycastTarget = false;
            });
        }
        return row;
    }

    // -------------------------------------------------------------- yardımcı

    /// <summary>
    /// Paneldeki eski çocuğu devralır. İLK koşuda panelin altında, sonraki
    /// koşularda hub ağacının içinde bulunur; aranan yer yanlışsa kurucu
    /// ikinci koşuda YENİSİNİ oluşturur ve sapar (1.6b'de yaşandı).
    /// </summary>
    private static GameObject Devral(UIBuilder b, string ad)
    {
        GameObject mevcut = Ara(b, ad);
        if (mevcut != null)
        {
            string ebeveyn = Yol(mevcut.transform.parent.gameObject);
            return b.Ensure(ebeveyn, ad, adopt: true);
        }
        return b.Ensure(PanelPath, ad, adopt: true);
    }

    /// <summary>Hub ağacında ya da panelin altında ada göre arar.</summary>
    private static GameObject Ara(UIBuilder b, string ad)
    {
        GameObject panel = b.FindObject(PanelPath);
        if (panel == null) return null;

        foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == ad) return t.gameObject;
        }
        return null;
    }

    private static GameObject DevralCocuk(UIBuilder b, GameObject parent, string ad)
    {
        if (parent == null) return null;
        Transform t = parent.transform.Find(ad);
        return t != null ? b.Ensure(Yol(parent), ad, adopt: true) : b.Ensure(Yol(parent), ad);
    }

    private static void Tasi(GameObject go, GameObject parent)
    {
        if (go == null || parent == null) return;
        if (go.transform.parent == parent.transform) return;
        go.transform.SetParent(parent.transform, false);
    }

    private static void Yukseklik(UIBuilder b, GameObject go, float h)
    {
        b.EnsureComponent<LayoutElement>(go, le =>
        {
            le.ignoreLayout = false;
            le.preferredHeight = h;
            le.minHeight = h;
            le.flexibleHeight = 0f;
            le.preferredWidth = -1f;
            le.flexibleWidth = 1f;
        });
    }

    private static void Esnek(UIBuilder b, GameObject go)
    {
        b.EnsureComponent<LayoutElement>(go, le =>
        {
            le.ignoreLayout = false;
            le.preferredWidth = -1f;
            le.minWidth = 0f;
            le.flexibleWidth = 1f;
            le.preferredHeight = -1f;
            le.flexibleHeight = 1f;
        });
    }

    /// <summary>
    /// Layout grubunun sürmediği, elle konumlanan çocuk. GÖRÜNÜR KALIR.
    /// </summary>
    private static void LayoutDisi(UIBuilder b, GameObject go)
    {
        if (go == null) return;
        if (go.transform.parent != null &&
            go.transform.parent.GetComponent<LayoutGroup>() != null)
        {
            b.EnsureComponent<LayoutElement>(go, le => le.ignoreLayout = true);
        }

        // AÇIK OLDUĞU SÖYLENİR: bayrakla yönetilmeyen bir giriş, önceki bir
        // koşudan kapalı kalmış olabilir (1.7'de yaşandı: profil avatarı ve
        // kimyager adı boş şeritte görünmüyordu).
        if (!go.activeSelf) go.SetActive(true);
    }

    /// <summary>
    /// Bayrakla yönetilen giriş: sahnede KAPALI kaydedilir. Görünürlüğün
    /// tek karar yeri <c>MenuHub.Apply</c>; burada kapalı başlatılmasının
    /// nedeni, bayrak kapalıyken ilk karede görünüp kaybolmaması.
    /// </summary>
    private static void Gizle(UIBuilder b, GameObject go, Vector2 etkinOlcu = default)
    {
        if (go == null) return;

        // ignoreLayout KURULMAZ: bayrak açılınca giriş layout akışına
        // girmeli. 1.7'de ignoreLayout kalınca üç mod kartı da açıldıkları
        // anda sıfır boyutta ekranın ortasında üst üste bindi (tüm
        // bayraklar açık görüntüsünde ölçüldü).

        // Kapalı objeyi layout SÜRMEZ, yani rect'i oluşturulduğu hâlde
        // (100x100) kalır. Dokunma hedefi taraması kapalı objeleri de
        // ölçtüğü için bu "grafik çok küçük" diye rapor ediliyordu.
        // Etkinleşince alacağı ölçü şimdiden yazılır; layout zaten üzerine
        // yazacak, yani çakışma olmaz.
        if (etkinOlcu != default(Vector2))
        {
            RectTransform rt = go.transform as RectTransform;
            if (rt != null && rt.sizeDelta != etkinOlcu) rt.sizeDelta = etkinOlcu;
        }

        if (go.activeSelf) go.SetActive(false);
    }

    private static void Cerceve(UIBuilder b, GameObject go, string path)
    {
        b.EnsureComponent<Image>(go, i =>
        {
            i.sprite = LoadSprite(path);
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = Color.white;
            i.preserveAspect = false;
            i.raycastTarget = true;
        });
    }

    private static GameObject Metin(UIBuilder b, NexumTheme theme, GameObject parent, string ad,
                                    Vector2 boyut, Vector2 pos, NexumTheme.TypeStyle stil,
                                    Color renk, TextAlignmentOptions hiza)
    {
        if (parent == null) return null;
        GameObject go = b.Ensure(Yol(parent), ad);
        if (go == null) return null;

        b.SetFixed(go, UITokens.Center, boyut, pos);
        Yazi(b, theme, go, stil, renk, hiza);
        return go;
    }

    private static void Yazi(UIBuilder b, NexumTheme theme, GameObject go,
                             NexumTheme.TypeStyle stil, Color renk, TextAlignmentOptions hiza)
    {
        b.EnsureComponent<TextMeshProUGUI>(go, tmp =>
        {
            tmp.font = stil.font;
            tmp.fontSize = stil.size;
            tmp.enableAutoSizing = false;
            tmp.alignment = hiza;
            tmp.color = renk;
            tmp.raycastTarget = false;
            tmp.spriteAsset = Resources.Load<TMP_SpriteAsset>(ReactionText.SpriteAssetResource);
        });
    }

    private static void Yerellestir(UIBuilder b, GameObject go, string key)
    {
        if (go == null) return;
        b.EnsureComponent<LocalizedText>(go, loc => loc.key = key);
    }

    /// <summary>Sahnedeki tam yol (Canvas'tan itibaren).</summary>
    private static string Yol(GameObject go)
    {
        string yol = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            yol = t.name + "/" + yol;
        }
        return yol;
    }

    private static TextMeshProUGUI Tmp(GameObject go)
    {
        return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void Bagla(Button button, Object target, UnityEngine.Events.UnityAction action)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == action.Method.Name) return;
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
