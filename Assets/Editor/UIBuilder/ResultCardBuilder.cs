using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bölüm sonu SONUÇ KARTI (1.6b).
///
/// Eski hâl: <c>LevelCompletePanel</c> yalnızca "TEBRİKLER!" başlığı ve iki
/// buton. Oyuncu bölümü bitiriyor ama ne kazandığını, kaç hamlede
/// bitirdiğini, hangi tepkimeyi ilk kez gördüğünü öğrenmiyordu.
///
/// Kart mevcut panelin İÇİNE kurulur: panel tam ekran karartma olarak kalır,
/// başlık ve iki buton DEVRALINIR (OnClick bağları ve TouchArea çocukları
/// yerinde kalsın). Eklenen: bölüm satırı, kazanılan puan, hamle sayısı,
/// üretilen hedefler (kendi taş görselleriyle), bu bölümde keşfedilen
/// tepkimeler, "Tekrar oyna" ve "Paylaş" butonları.
///
/// <b>Yıldızlar Faz 2'de.</b> Üç yıldızlık satır ŞİMDİDEN kuruluyor ama
/// <c>ResultCard.starsEnabled</c> false olduğu için gizli. Yer ayrıldı ki
/// Faz 2 kartı yeniden dizmek zorunda kalmasın. Yıldız görselleri kitten.
///
/// Dokunma alanları BURADA kurulmaz: 144 birimlik alanları
/// <c>NexumTouchTargets</c> tamamlar (tek kaynak kuralı). Buton grafikleri
/// 420x120, satır aralığı 160 birim — iki satırın dokunma alanı kesişmez.
///
/// Menüden: NEXUM → Sonuç Kartını Kur
/// Batch:   -executeMethod ResultCardBuilder.Run
/// </summary>
public static class ResultCardBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string PanelPath = "Canvas/LevelCompletePanel";
    private const string CardName = "ResultCard";

    private static string CardPath { get { return PanelPath + "/" + CardName; } }

    private const string FramePath = "Assets/Art/UI/panel_lg_normal.png";
    private const string StatFramePath = "Assets/Art/UI/panel_sm_normal.png";
    private const string StarEmptyPath = "Assets/Art/UI/star_empty_normal.png";
    private const string StarFullPath = "Assets/Art/UI/star_full_normal.png";
    private const string TileSlotPath = "Assets/Art/UI/compound_ring_normal.png";

    /// <summary>
    /// Kart 2.1'de 1500'den 1600'e BÜYÜDÜ: "+N XP" satırı için bant açıldı.
    ///
    /// 1.6'da yalnızca YILDIZ satırının yeri ayrılmıştı (498..402); XP
    /// satırının yeri ayrılmamıştı ve kartın dikey ritminde 56 birimlik boş
    /// bir bant yoktu — bantlar arası en büyük açıklık 18 birim (sayı
    /// kutuları 245 ile "ÜRETİLEN" 227 arası). Üç seçenek vardı: tepkime
    /// kutusunu kısmak (4 satır 173 birim istiyor, kutu 260'tan 206'ya
    /// inerdi ve %85 sınırına dayanırdı), sayı kutularını üçe çıkarmak
    /// ("KAZANILAN PUAN" etiketi 260 birimlik kutuya sığmıyor) ya da kartı
    /// büyütmek. Üçüncüsü seçildi: ÜST YARI HİÇ DEĞİŞMİYOR, yalnızca buton
    /// satırları 60 birim aşağı kaydı.
    ///
    /// Güvenli alan payı: kart ±800, tasarım yüksekliği en az 1920 (±960),
    /// yani her kenarda 160 birim pay (önceki kartta 210).
    /// </summary>
    private static readonly Vector2 CardSize = new Vector2(920f, 1600f);

    // Dikey ritim (kart merkezine göre). Kart yarı yüksekliği 800.
    // Bantlar ÖLÇÜLDÜ, üst üste binmiyor:
    // 2.2'de ÜST BLOK YUKARI ALINDI. Kart 1.6'da 1500, 2.1'de 1600
    // oldu ama üst içerik yerinde kalmıştı: tepede 135 birim ÖLÜ BOŞLUK
    // vardı. Yıldız yönlendirme satırı (46) o boşluktan çıktı, kart
    // bir daha büyümedi ve güvenli alan payı 160 birimde kaldı.
    // Ölçülen yeni ritim (boşluklar 15, üst pay 50):
    //   başlık      750..660      bölüm satırı 645..585
    //   yıldızlar   570..474      yıldız satırı 459..413
    //   sayı kutuları 395..245
    //   "ÜRETİLEN"  227..183      hedef satırı 195..-5
    //   "YENİ TEP." -18..-62      tepkime kutusu -60..-280
    //   "+N XP"     -334..-390      (2.1'de eklendi)
    //   buton sat.1 -418..-562    buton sat.2 -578..-722
    //   paylaşım satırı -732..-778    kart iç kenarı -800
    private const float TitleY = 705f;
    private const float LevelY = 615f;
    private const float StarsY = 522f;

    /// <summary>Yıldız yönlendirme satırı: yıldızlar ile sayı kutuları arası.</summary>
    private const float StarHintY = 436f;
    private static readonly Vector2 StarHintSize = new Vector2(820f, 46f);
    private const float StatsY = 320f;
    private const float GoalsY = 95f;
    private const float GoalsLabelY = 205f;
    private const float ReactionsLabelY = -40f;
    private const float ReactionsY = -170f;

    /// <summary>
    /// Tepkime kutusu 260'tan 220'ye indi (2.1). Kutu 260 iken 4 satırlık
    /// metin (172 birim) kutunun içinde ortalanıyor ve boyalı alt kenarı
    /// -276'ya iniyordu; XP satırının üst kenarı -334, yani ikisi arasında
    /// 58 birim kalıyordu ve cihaz görüntüsünde satırlar birbirine
    /// yapışık okunuyordu. 220'de boyalı alan -256'da bitiyor, açıklık
    /// 78 birim. Doluluk 172/220 = %78 (sınır %85).
    /// </summary>
    private static readonly Vector2 ReactionsSize = new Vector2(820f, 220f);

    /// <summary>Deneyim satırı: tepkime kutusu (-320) ile buton satırı (-418) arası.</summary>
    private const float XpRowY = -362f;
    private static readonly Vector2 XpRowSize = new Vector2(820f, 56f);

    private const float ButtonRow1Y = -490f;
    private const float ButtonRow2Y = -650f;

    /// <summary>
    /// Paylaşım hata satırı: buton satırlarının ALTINDA, kartın içinde.
    /// Sistem bildirimi bandı kullanılamıyor — o bant SafeAreaRoot altında,
    /// LevelCompletePanel ondan sonra çiziliyor, mesaj kartın arkasında
    /// kalırdı (sahne çocuk sırası ölçüldü: SafeAreaRoot 1, kart 9).
    /// Buton dokunma alanı -722'de bitiyor, kartın iç kenarı -800.
    /// </summary>
    private const float ShareMessageY = -755f;
    private static readonly Vector2 ShareMessageSize = new Vector2(820f, 46f);

    private static readonly Vector2 ButtonSize = new Vector2(420f, 120f);
    private const float ButtonX = 225f;

    private static readonly Vector2 StatBoxSize = new Vector2(400f, 150f);
    private const float StatBoxX = 215f;

    private static readonly Vector2 StarSize = new Vector2(96f, 96f);
    private const float StarSpacing = 120f;

    private static readonly Vector2 GoalSlotSize = new Vector2(160f, 200f);
    private const float GoalSpacing = 190f;

    /// <summary>Hedef yuvası sayısı. Bugün en çok 2 hedef var; 3 yuva pay bırakır.</summary>
    private const int GoalSlots = 3;

    [MenuItem("NEXUM/Sonuç Kartını Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("ResultCard", ScenePath, dryRun);
        NexumTheme theme = UITokens.Theme;

        GameObject panel = b.FindObject(PanelPath);
        if (panel == null)
        {
            b.Error("LevelCompletePanel bulunamadı: " + PanelPath);
            b.End();
            return;
        }

        // --- kart çerçevesi
        GameObject card = b.Ensure(PanelPath, CardName);
        if (card == null) { b.End(); return; }

        b.SetFixed(card, UITokens.Center, CardSize, Vector2.zero);
        b.EnsureComponent<Image>(card, image =>
        {
            image.sprite = LoadSprite(FramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = true;
        });

        // --- başlık ve iki buton DEVRALINIR (bağları korunsun)
        GameObject title = Devral(b, "TitleText");
        if (title != null)
        {
            Reparent(b, title, card);
            b.SetFixed(title, UITokens.Center, new Vector2(820f, 90f), new Vector2(0f, TitleY));
            b.EnsureComponent<TextMeshProUGUI>(title, tmp => ApplyText(
                tmp, theme, theme.typography.title, theme.palette.textPrimary));
        }

        GameObject next = Devral(b, "NextLevelButton");
        GameObject menu = Devral(b, "MainMenuButton");
        if (next != null) Reparent(b, next, card);
        if (menu != null) Reparent(b, menu, card);

        // --- bölüm satırı
        GameObject level = Metin(b, theme, card, "LevelText", new Vector2(820f, 60f), LevelY,
                                 theme.typography.subtitle, theme.palette.primaryBright);

        // --- yıldız satırı (2.2'de AÇILDI)
        GameObject stars = b.Ensure(CardPath, "StarsRow");
        b.SetFixed(stars, UITokens.Center, new Vector2(3f * StarSpacing, StarSize.y),
                   new Vector2(0f, StarsY));

        List<Image> starImages = new List<Image>();
        for (int i = 0; i < 3; i++)
        {
            GameObject star = b.Ensure(CardPath + "/StarsRow", "Star" + i);
            b.SetFixed(star, UITokens.Center, StarSize,
                       new Vector2((i - 1) * StarSpacing, 0f));
            Image img = b.EnsureComponent<Image>(star, image =>
            {
                // Sahnede BOŞ yıldız kaydedilir; dolu hâli çalışma
                // zamanında ResultCard yazar. Kaydedilen hâl "henüz
                // kazanılmadı" olmalı ki kart dolmadan bir kare dolu
                // görünmesin.
                image.sprite = LoadSprite(StarEmptyPath);
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
            });

            // dryRun'da da toplanır: liste boş kalırsa denetleyici alanı
            // "değişti" görünür ve sahte sapma üretir.
            if (img != null) starImages.Add(img);
        }

        // --- yıldız yönlendirme satırı: TEK sonraki adım
        GameObject starHint = Metin(b, theme, card, "StarHintText", StarHintSize,
                                    StarHintY, theme.typography.label,
                                    theme.palette.primaryBright);

        // --- iki sayı kutusu: kazanılan puan ve hamle
        GameObject scoreValue, movesValue;
        SayiKutusu(b, theme, "ScoreBox", -StatBoxX, CodeStrings.ResultScoreLabel, out scoreValue);
        SayiKutusu(b, theme, "MovesBox", StatBoxX, CodeStrings.ResultMovesLabel, out movesValue);

        // --- üretilen hedefler
        GameObject goalsLabel = Metin(b, theme, card, "GoalsLabel", new Vector2(820f, 44f),
                                      GoalsLabelY, theme.typography.label,
                                      theme.palette.textSecondary);
        EnsureLocalized(b, goalsLabel, CodeStrings.ResultGoalsLabel);

        GameObject goalsRow = b.Ensure(CardPath, "GoalsRow");
        b.SetFixed(goalsRow, UITokens.Center,
                   new Vector2(GoalSlots * GoalSpacing, GoalSlotSize.y),
                   new Vector2(0f, GoalsY));

        List<Image> goalIcons = new List<Image>();
        List<TextMeshProUGUI> goalSymbols = new List<TextMeshProUGUI>();
        List<TextMeshProUGUI> goalAmounts = new List<TextMeshProUGUI>();
        for (int i = 0; i < GoalSlots; i++)
        {
            string slotPath = CardPath + "/GoalsRow";
            GameObject slot = b.Ensure(slotPath, "Goal" + i);
            b.SetFixed(slot, UITokens.Center, GoalSlotSize,
                       new Vector2((i - (GoalSlots - 1) * 0.5f) * GoalSpacing, 0f));

            GameObject icon = b.Ensure(slotPath + "/Goal" + i, "Icon");
            b.SetFixed(icon, UITokens.Center, new Vector2(132f, 132f), new Vector2(0f, 28f));
            b.EnsureComponent<Image>(icon, image =>
            {
                image.sprite = LoadSprite(TileSlotPath);
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
            });

            // Taşın SEMBOLÜ de görünmeli: taş görseli yalnızca çerçeve,
            // formül çocuk TMP'de duruyor (Tile_* prefab yapısı).
            GameObject symbol = b.Ensure(slotPath + "/Goal" + i + "/Icon", "Symbol");
            b.SetStretch(symbol, UITokens.Zero, UITokens.One);
            b.EnsureComponent<TextMeshProUGUI>(symbol, tmp => ApplyText(
                tmp, theme, theme.typography.label, theme.palette.textPrimary));

            GameObject amount = b.Ensure(slotPath + "/Goal" + i, "AmountText");
            b.SetFixed(amount, UITokens.Center, new Vector2(150f, 48f), new Vector2(0f, -72f));
            b.EnsureComponent<TextMeshProUGUI>(amount, tmp => ApplyText(
                tmp, theme, theme.typography.label, theme.palette.primaryBright));

            // dryRun'da da toplanır: liste boş kalırsa denetleyici alanı
            // "değişti" görünür ve sahte sapma üretir.
            Image img = icon != null ? icon.GetComponent<Image>() : null;
            TextMeshProUGUI sym = symbol != null ? symbol.GetComponent<TextMeshProUGUI>() : null;
            TextMeshProUGUI tmp2 = amount != null ? amount.GetComponent<TextMeshProUGUI>() : null;
            if (img != null) goalIcons.Add(img);
            if (sym != null) goalSymbols.Add(sym);
            if (tmp2 != null) goalAmounts.Add(tmp2);
        }

        // --- bu bölümde keşfedilen tepkimeler
        GameObject reactionsLabel = Metin(b, theme, card, "ReactionsLabel",
                                          new Vector2(820f, 44f), ReactionsLabelY,
                                          theme.typography.label, theme.palette.textSecondary);
        EnsureLocalized(b, reactionsLabel, CodeStrings.ResultReactionsLabel);

        GameObject reactions = Metin(b, theme, card, "ReactionsText", ReactionsSize,
                                     ReactionsY, theme.typography.body,
                                     theme.palette.textPrimary);

        // --- deneyim satırı: "DENEYİM" etiketi + "+N XP" değeri (2.1).
        // Etiket ve değer AYNI bantta yan yana: iki satır olsaydı 112 birim
        // isterdi ve kart 1600'e değil 1660'a çıkardı.
        GameObject xpRow = b.Ensure(CardPath, "XpRow");
        b.SetFixed(xpRow, UITokens.Center, XpRowSize, new Vector2(0f, XpRowY));

        GameObject xpLabel = b.Ensure(CardPath + "/XpRow", "Label");
        b.SetFixed(xpLabel, UITokens.Center, new Vector2(400f, 44f), new Vector2(-110f, 0f));
        b.EnsureComponent<TextMeshProUGUI>(xpLabel, tmp =>
        {
            ApplyText(tmp, theme, theme.typography.label, theme.palette.textSecondary);
            tmp.alignment = TextAlignmentOptions.Right;
        });
        EnsureLocalized(b, xpLabel, CodeStrings.ResultXpLabel);

        GameObject xpValue = b.Ensure(CardPath + "/XpRow", "Value");
        b.SetFixed(xpValue, UITokens.Center, new Vector2(260f, 52f), new Vector2(230f, 0f));
        b.EnsureComponent<TextMeshProUGUI>(xpValue, tmp =>
        {
            ApplyText(tmp, theme, theme.typography.subtitle, theme.palette.warning);
            tmp.alignment = TextAlignmentOptions.Left;
        });

        // --- butonlar: üst satır sonraki/tekrar, alt satır paylaş/menü
        if (next != null) KurButon(b, theme, next, new Vector2(-ButtonX, ButtonRow1Y),
                                   CodeStrings.ResultNext, MenuRoleBuilder.PrimaryPath);

        GameObject restart = b.Ensure(CardPath, "RestartButton");
        KurButon(b, theme, restart, new Vector2(ButtonX, ButtonRow1Y),
                 CodeStrings.ResultRestart, MenuRoleBuilder.SecondaryPath);

        GameObject share = b.Ensure(CardPath, "ShareButton");
        KurButon(b, theme, share, new Vector2(-ButtonX, ButtonRow2Y),
                 CodeStrings.ResultShare, MenuRoleBuilder.SecondaryPath);

        if (menu != null) KurButon(b, theme, menu, new Vector2(ButtonX, ButtonRow2Y),
                                   CodeStrings.ResultMenu, MenuRoleBuilder.SecondaryPath);

        // --- paylaşım hata satırı (normalde gizli)
        GameObject shareMessage = Metin(b, theme, card, "ShareMessage", ShareMessageSize,
                                        ShareMessageY, theme.typography.label,
                                        theme.palette.warning);

        // --- denetleyici
        ResultCard controller = b.EnsureComponent<ResultCard>(card, rc =>
        {
            rc.levelText = Tmp(level);
            rc.scoreValueText = Tmp(scoreValue);
            rc.movesValueText = Tmp(movesValue);
            rc.reactionsText = Tmp(reactions);
            rc.xpValueText = Tmp(xpValue);
            rc.goalIcons = goalIcons.ToArray();
            rc.goalSymbolTexts = goalSymbols.ToArray();
            rc.goalAmountTexts = goalAmounts.ToArray();
            rc.goalSpacing = GoalSpacing;
            rc.starsEnabled = true;           // 2.2
            rc.starsRow = stars;
            rc.starImages = starImages.ToArray();
            rc.starFullSprite = LoadSprite(StarFullPath);
            rc.starEmptySprite = LoadSprite(StarEmptyPath);
            rc.starHintText = Tmp(starHint);
            rc.nextLevelButton = next;
            rc.restartButton = restart;
            rc.restartSoloX = 0f;
            rc.shareMessageText = Tmp(shareMessage);
        });

        if (!dryRun)
        {
            UIManager ui = Object.FindObjectOfType<UIManager>(true);
            if (ui != null)
            {
                Button restartButton = restart != null ? restart.GetComponent<Button>() : null;
                if (restartButton != null) Wire(restartButton, ui, ui.RestartGame);

                Button shareButton = share != null ? share.GetComponent<Button>() : null;
                if (shareButton != null) Wire(shareButton, ui, ui.ShareResultCard);

                if (controller != null) SetReference(ui, "resultCard", controller);
            }

            // Yıldız satırı 2.2'de AÇIK kaydedilir: bayrak açıkken
            // sahnenin kayıtlı hâli ile çalışma zamanı ayrışmasın
            // (hub'daki Gorunurluk kuralıyla aynı gerekçe).
            if (stars != null && !stars.activeSelf) stars.SetActive(true);
            if (shareMessage != null && shareMessage.activeSelf) shareMessage.SetActive(false);
        }

        b.End();
    }

    // --------------------------------------------------------------- parçalar

    private static void SayiKutusu(UIBuilder b, NexumTheme theme, string ad, float x,
                                   string locKey, out GameObject valueObject)
    {
        GameObject box = b.Ensure(CardPath, ad);
        b.SetFixed(box, UITokens.Center, StatBoxSize, new Vector2(x, StatsY));
        b.EnsureComponent<Image>(box, image =>
        {
            image.sprite = LoadSprite(StatFramePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.raycastTarget = false;
        });

        // Etiket textPrimary: sayı kutusunun yüzeyi (panel_sm, #4C4F5A)
        // kartın zemininden AÇIK, orada textSecondary 3,93:1'de kalıyordu
        // (ContrastTests ölçtü). Beyazla 8:1'in üstünde. Hiyerarşi renkle
        // değil punto ve konumla kuruluyor: etiket 30, değer 60 punto.
        GameObject label = b.Ensure(CardPath + "/" + ad, "Label");
        b.SetFixed(label, UITokens.Center, new Vector2(360f, 40f), new Vector2(0f, 44f));
        b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(
            tmp, theme, theme.typography.label, theme.palette.textPrimary));
        EnsureLocalized(b, label, locKey);

        // 60 punto değer 72 birimlik kutuya SIĞMIYOR: ölçüm %105 dedi
        // (ResultCardFitTests). Kutu 92'ye çıkarıldı -> %82.
        valueObject = b.Ensure(CardPath + "/" + ad, "Value");
        b.SetFixed(valueObject, UITokens.Center, new Vector2(360f, 92f), new Vector2(0f, -26f));
        b.EnsureComponent<TextMeshProUGUI>(valueObject, tmp => ApplyText(
            tmp, theme, theme.typography.value, theme.palette.primaryBright));
    }

    private static void KurButon(UIBuilder b, NexumTheme theme, GameObject btn,
                                 Vector2 pos, string locKey, string framePath)
    {
        if (btn == null) return;

        b.SetFixed(btn, UITokens.Center, ButtonSize, pos);
        b.EnsureComponent<Image>(btn, image =>
        {
            image.sprite = LoadSprite(framePath);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = true;
        });
        b.EnsureComponent<Button>(btn, button => button.transition = Selectable.Transition.ColorTint);

        // Devralınan butonların etiketi "Text (TMP)" adında; yenilerde "Label".
        // Eskiler DEVRALINIR: elle yapılmış etiketi kurucu sessizce
        // sahiplenmez, açıkça devralır.
        bool eskiEtiket = b.FindObject(Yol(btn) + "/Text (TMP)") != null;
        string etiketAdi = eskiEtiket ? "Text (TMP)" : "Label";
        GameObject label = b.Ensure(Yol(btn), etiketAdi, adopt: eskiEtiket);
        b.SetFixed(label, UITokens.Center, new Vector2(ButtonSize.x - 40f, 72f), Vector2.zero);
        b.EnsureComponent<TextMeshProUGUI>(label, tmp => ApplyText(
            tmp, theme, theme.typography.label, theme.palette.textPrimary));
        EnsureLocalized(b, label, locKey);
    }

    private static GameObject Metin(UIBuilder b, NexumTheme theme, GameObject parent, string ad,
                                    Vector2 boyut, float y, NexumTheme.TypeStyle stil, Color renk)
    {
        GameObject go = b.Ensure(CardPath, ad);
        b.SetFixed(go, UITokens.Center, boyut, new Vector2(0f, y));
        b.EnsureComponent<TextMeshProUGUI>(go, tmp => ApplyText(tmp, theme, stil, renk));
        return go;
    }

    // -------------------------------------------------------------- yardımcı

    /// <summary>
    /// Panelin eski çocuğunu devralır. İLK koşuda panelin altında, sonraki
    /// koşularda kartın altında bulunur — aranan yer yanlışsa kurucu ikinci
    /// koşuda yenisini OLUŞTURUR ve sapar.
    /// </summary>
    private static GameObject Devral(UIBuilder b, string ad)
    {
        return b.FindObject(CardPath + "/" + ad) != null
            ? b.Ensure(CardPath, ad, adopt: true)
            : b.Ensure(PanelPath, ad, adopt: true);
    }

    private static string Yol(GameObject go)
    {
        return CardPath + "/" + go.name;
    }

    private static TextMeshProUGUI Tmp(GameObject go)
    {
        return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void Reparent(UIBuilder b, GameObject go, GameObject parent)
    {
        if (go == null || parent == null) return;
        if (go.transform.parent == parent.transform) return;
        go.transform.SetParent(parent.transform, false);
    }

    private static void ApplyText(TextMeshProUGUI tmp, NexumTheme theme,
                                  NexumTheme.TypeStyle style, Color color)
    {
        tmp.font = style.font;
        tmp.fontSize = style.size;
        tmp.enableAutoSizing = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.spriteAsset = Resources.Load<TMP_SpriteAsset>(ReactionText.SpriteAssetResource);
    }

    private static void EnsureLocalized(UIBuilder b, GameObject go, string key)
    {
        if (go == null) return;
        b.EnsureComponent<LocalizedText>(go, loc => loc.key = key);
    }

    private static void Wire(Button button, Object target, UnityEngine.Events.UnityAction action)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == action.Method.Name) return;
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void SetReference(Object owner, string field, Object value)
    {
        SerializedObject so = new SerializedObject(owner);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue == value) return;

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(owner);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
