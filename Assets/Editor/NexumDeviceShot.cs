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
/// GERÇEK ekran görüntüsü: Unity'nin kendi render'ı, istenen canvas
/// boyutunda, batch mode'da.
///
/// NEDEN GEREKLİ: batch mode her zaman 640×480 yüzeyle açılır ve
/// <c>-screen-width</c> işlemez. <c>LayoutCheck</c> bu yüzden hedef boyutu
/// SafeAreaRoot'a DAYATIYOR — ama Canvas 640×480 kalıyor, yani
/// CanvasScaler'ın ölçeği ve canvas'a bağlı her hesap gerçek cihazdakinden
/// FARKLI çıkıyor. Burada Canvas bir kameraya bağlanıp kamera hedef boyutta
/// bir RenderTexture'a çizdiriliyor; böylece CanvasScaler cihazdaki gibi
/// çalışıyor ve gördüğümüz şey oyunda görülecek şey oluyor.
///
/// Sahneler DEĞİŞTİRİLMEZ: açılır, bellekte düzenlenir, render alınır,
/// kaydedilmeden kapatılır.
///
/// Batch: Unity.exe -batchmode -quit -executeMethod NexumDeviceShot.Run
///        (-nographics VERİLMEZ; render bağlamı gerekir)
/// </summary>
public static class NexumDeviceShot
{
    private const string Tag = "NEXUM_DEVICESHOT";
    private const string OutDir = "docs/device-shots";

    private static readonly Vector2Int[] Boyutlar =
    {
        new Vector2Int(1080, 1920),
        new Vector2Int(1440, 1920),
    };

    public struct Ekran
    {
        public string ad;
        public string scene;
        public string[] ac;      // açılacak obje yolları (Canvas altından)
        public string[] kapa;    // kapatılacak obje yolları
        public bool tahtayiDoldur;
        public bool ansiklopediyiDoldur;
        public bool geriBildirim;

        // 1.5: quiz paneli iki hâlde gösterilir (soru / açıklamalı cevap) ve
        // duraklat kartı bilgi kartları kapalıyken de alınır.
        public int quiz;                  // 0 yok, 1 soru, 2 açıklamalı cevap
        public bool bilgiKartlariKapali;

        // 1.6b: bölüm sonu sonuç kartı. 0 yok, 1 yeni tepkimeli bölüm,
        // 2 yeni tepkime keşfedilmemiş bölüm (ikisi farklı metin gösterir).
        public int sonucKarti;

        // 1.7: ana menü hub'ı. 0 yok, 1 bugünkü bayraklar, 2 hepsi açık.
        public int hub;

        // 2.1: rütbe atlama kutlaması (sonuç kartının üstünde).
        public bool kutlama;
    }

    public static readonly Ekran[] Ekranlar =
    {
        new Ekran { ad = "ana-menu", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/MainMenuPanel" },
                    kapa = new[] { "SafeAreaRoot/SettingsPanel", "SafeAreaRoot/HowToPlayPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" },
                    hub = 1 },

        // Faz 2/3/5 bayrakları açıldığında hub'ın nasıl duracağı: yerleşim
        // o gün yeniden ölçülmesin diye bugünden görüntüleniyor.
        new Ekran { ad = "ana-menu-tum-bayraklar", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/MainMenuPanel" },
                    kapa = new[] { "SafeAreaRoot/SettingsPanel", "SafeAreaRoot/HowToPlayPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" },
                    hub = 2 },

        new Ekran { ad = "ayarlar", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/SettingsPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/HowToPlayPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "nasil-oynanir", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/HowToPlayPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/RecipeBookPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "tarif-defteri", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/RecipeBookPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/HowToPlayPanel", "SafeAreaRoot/ProfilePanel",
                                   "SafeAreaRoot/EncyclopediaPanel", "SafeAreaRoot/LevelSelectionPanel" } },

        new Ekran { ad = "oyun", scene = "Assets/Scenes/Game.unity",
                    ac = new string[0],
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true },

        new Ekran { ad = "gecersiz-birlestirme", scene = "Assets/Scenes/Game.unity",
                    ac = new string[0],
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true, geriBildirim = true },

        new Ekran { ad = "ansiklopedi", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/EncyclopediaPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/HowToPlayPanel", "SafeAreaRoot/RecipeBookPanel",
                                   "SafeAreaRoot/ProfilePanel", "SafeAreaRoot/LevelSelectionPanel",
                                   "SafeAreaRoot/EncyclopediaPanel/ElementDetailPanel" },
                    ansiklopediyiDoldur = true },

        new Ekran { ad = "mentor-karti", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/RegistrationPanel",
                                 "SafeAreaRoot/RegistrationPanel/MentorDetailPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/HowToPlayPanel", "SafeAreaRoot/RecipeBookPanel",
                                   "SafeAreaRoot/ProfilePanel", "SafeAreaRoot/EncyclopediaPanel",
                                   "SafeAreaRoot/LevelSelectionPanel",
                                   "SafeAreaRoot/RegistrationPanel/IDCardPanel" } },

        new Ekran { ad = "quiz", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "QuizPanel" },
                    kapa = new[] { "PausePanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true, quiz = 1 },

        new Ekran { ad = "quiz-aciklama", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "QuizPanel" },
                    kapa = new[] { "PausePanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true, quiz = 2 },

        new Ekran { ad = "duraklat", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "PausePanel" },
                    kapa = new[] { "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true },

        // 1.6a: kurucu sapması düzeltilirken "görünür sonuç değişmedi"
        // iddiası bu üç ekranın piksel karşılaştırmasıyla kanıtlanıyor.
        // Sapan kurucuların dokunduğu her obje bu karelerde görünüyor:
        // Backdrop + 21 LockIcon + BackButton (tarif defteri, iki sahne) ve
        // CloseButton (ansiklopedi ayrıntı kartı, iki sahne).
        new Ekran { ad = "tarif-defteri-oyun", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "SafeAreaRoot/RecipeBookPanel" },
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel", "SafeAreaRoot/ElementDetailPanel" },
                    tahtayiDoldur = true },

        new Ekran { ad = "ansiklopedi-detay", scene = "Assets/Scenes/MainMenu.unity",
                    ac = new[] { "SafeAreaRoot/EncyclopediaPanel",
                                 "SafeAreaRoot/EncyclopediaPanel/ElementDetailPanel" },
                    kapa = new[] { "SafeAreaRoot/MainMenuPanel", "SafeAreaRoot/SettingsPanel",
                                   "SafeAreaRoot/HowToPlayPanel", "SafeAreaRoot/RecipeBookPanel",
                                   "SafeAreaRoot/ProfilePanel", "SafeAreaRoot/LevelSelectionPanel" },
                    ansiklopediyiDoldur = true },

        new Ekran { ad = "ansiklopedi-detay-oyun", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "SafeAreaRoot/ElementDetailPanel" },
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel", "SafeAreaRoot/RecipeBookPanel" },
                    tahtayiDoldur = true },

        new Ekran { ad = "sonuc-karti", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "LevelCompletePanel" },
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel", "SafeAreaRoot/RecipeBookPanel",
                                   "SafeAreaRoot/ElementDetailPanel" },
                    tahtayiDoldur = true, sonucKarti = 1 },

        new Ekran { ad = "sonuc-karti-tepkimesiz", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "LevelCompletePanel" },
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel", "SafeAreaRoot/RecipeBookPanel",
                                   "SafeAreaRoot/ElementDetailPanel" },
                    tahtayiDoldur = true, sonucKarti = 2 },

        // 2.1: rütbe atlama anı. Kutlama sonuç kartının ÜSTÜNDE duruyor;
        // görüntü ikisini birlikte gösteriyor ki perdenin kartı nasıl
        // örttüğü görülebilsin.
        new Ekran { ad = "rutbe-atlama", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "LevelCompletePanel", "RankUpPanel" },
                    kapa = new[] { "PausePanel", "QuizPanel", "GameOverPanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel", "SafeAreaRoot/RecipeBookPanel",
                                   "SafeAreaRoot/ElementDetailPanel" },
                    tahtayiDoldur = true, sonucKarti = 1, kutlama = true },

        new Ekran { ad = "duraklat-bilgi-kapali", scene = "Assets/Scenes/Game.unity",
                    ac = new[] { "PausePanel" },
                    kapa = new[] { "QuizPanel", "GameOverPanel", "LevelCompletePanel",
                                   "TutorialPanel", "HypothesisPanel", "UndoPanel", "FocusPanel",
                                   "AssistantPanel" },
                    tahtayiDoldur = true, bilgiKartlariKapali = true },
    };

    [MenuItem("NEXUM/Gerçek Ekran Görüntüsü Al")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        int alinan = 0;

        foreach (Ekran e in Ekranlar)
        {
            foreach (Vector2Int boy in Boyutlar)
            {
                Scene scene = EditorSceneManager.OpenScene(e.scene, OpenSceneMode.Single);
                GameObject canvasGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
                if (canvasGo == null)
                {
                    Debug.LogError($"{Tag}: Canvas yok -> {e.scene}");
                    continue;
                }

                Hazirla(canvasGo.transform, e);
                if (e.tahtayiDoldur) { TahtayiDoldur(scene); HedefleriDoldur(); }
                if (e.ansiklopediyiDoldur) AnsiklopediyiDoldur(canvasGo.transform);
                if (e.geriBildirim) GeriBildirimiDoldur(canvasGo.transform);
                if (e.quiz > 0) QuiziDoldur(canvasGo.transform, e.quiz == 2);
                if (e.sonucKarti > 0) SonucKartiniDoldur(e.sonucKarti == 1);
                if (e.hub > 0) HubuDoldur(e.hub == 2);
                if (e.kutlama) KutlamayiDoldur();
                BilgiKartiniAyarla(canvasGo.transform, e.bilgiKartlariKapali);

                string yol = $"{OutDir}/{e.ad}_{boy.x}x{boy.y}.png";
                if (Cek(canvasGo, boy, yol)) alinan++;
            }
        }

        TahtaKesiti();
        PaylasimGorseli();
        Debug.Log($"{Tag}: {alinan} görüntü -> {OutDir}");
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>
    /// PAYLAŞIM GÖRSELİ (1.6b): oyuncunun paylaşacağı dosyanın ta kendisi.
    ///
    /// Ayrı bir çizim yolu DEĞİL — <c>ResultShare.Render</c> çağrılır, yani
    /// PR'a eklenen görsel ile cihazda üretilen görsel aynı koddan çıkar.
    /// Kart edit mode'da <see cref="SonucKartiniDoldur"/> ile gerçek
    /// değerlerle dolduruluyor.
    /// </summary>
    private static void PaylasimGorseli()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        GameObject canvasGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Canvas");
        if (canvasGo == null) { Debug.LogError($"{Tag}: Canvas yok"); return; }

        Ekran e = Ekranlar.First(x => x.ad == "sonuc-karti");
        Hazirla(canvasGo.transform, e);
        TahtayiDoldur(scene);
        HedefleriDoldur();
        SonucKartiniDoldur(true);

        Texture2D png = ResultShare.Render();
        if (png == null) { Debug.LogError($"{Tag}: paylaşım görseli üretilemedi"); return; }

        string yol = $"{OutDir}/paylasim_{png.width}x{png.height}.png";
        File.WriteAllBytes(yol, png.EncodeToPNG());
        Object.DestroyImmediate(png);

        Debug.Log($"{Tag}: {yol}  {ResultShare.Width}x{ResultShare.Height} (ResultShare.Render)");
    }

    /// <summary>
    /// 4x4 tahtanın 1:1 (birim = piksel) kesiti. Seviye merdiveni ÇERÇEVEDE
    /// taşınıyor ve çerçeve ince bir eleman: ölçüm sayısının vaat ettiği
    /// ayrımın gözle de okunduğu buradan görülür. Dört seviye + boş hücre
    /// aynı karede.
    /// </summary>
    private static void TahtaKesiti()
    {
        string kaynak = $"{OutDir}/oyun_1080x1920.png";
        if (!File.Exists(kaynak))
        {
            Debug.LogError($"{Tag}: {kaynak} yok");
            return;
        }

        byte[] ham = File.ReadAllBytes(kaynak);
        Texture2D tam = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!tam.LoadImage(ham)) { Object.DestroyImmediate(tam); return; }

        // GridBoard: 900x900, anchor (0.5, 0.41666666) -> merkez ekranın
        // ALTINDAN 800 birim. Texture2D'de de y=0 alttadır, yani kesit
        // doğrudan 350..1250 aralığı; "üstten" hesaplamak kareyi kaydırıyordu.
        const int x0 = 90, w = 900, h = 900, y0 = 350;

        Texture2D kes = new Texture2D(w, h, TextureFormat.RGB24, false);
        kes.SetPixels(tam.GetPixels(x0, y0, w, h));
        kes.Apply();

        string yol = $"{OutDir}/detay_tahta-1-1.png";
        File.WriteAllBytes(yol, kes.EncodeToPNG());
        Debug.Log($"{Tag}: {yol}  {w}x{h} (1:1)");

        Object.DestroyImmediate(kes);
        Object.DestroyImmediate(tam);
    }

    public static void Hazirla(Transform canvas, Ekran e)
    {
        foreach (string p in e.kapa)
        {
            Transform t = canvas.Find(p);
            if (t != null) t.gameObject.SetActive(false);
        }
        foreach (string p in e.ac)
        {
            Transform t = canvas.Find(p);
            if (t != null)
            {
                t.gameObject.SetActive(true);
                t.localScale = Vector3.one;      // panel geçişi tween'i ölçeği 0'da bırakmış olabilir
            }
        }

        MetinleriDoldur(canvas);
    }

    /// <summary>
    /// Yerelleştirilmiş etiketler çalışma zamanında yazılıyor; edit mode'da
    /// BOŞ görünüyorlardı, yani cihaz görüntüleri buton yazılarını hiç
    /// göstermiyordu. Sözlük burada da okunur — görüntü ne gösteriyorsa
    /// oyuncunun göreceği metin odur.
    /// </summary>
    public static void MetinleriDoldur(Transform canvas)
    {
        foreach (LocalizedText loc in canvas.GetComponentsInChildren<LocalizedText>(true))
        {
            if (loc == null || string.IsNullOrEmpty(loc.key)) continue;

            TMP_Text tmp = loc.GetComponent<TMP_Text>();
            if (tmp == null) continue;

            string metin = Loc.Get(loc.key);
            if (!string.IsNullOrEmpty(metin)) tmp.text = metin;
        }
    }

    /// <summary>
    /// Geçersiz birleştirme mesajı çalışma zamanında doluyor; görüntüde
    /// görünsün diye METNİ AYNI KAYNAKTAN (ReactionText + Loc) kurulur.
    /// </summary>
    public static void GeriBildirimiDoldur(Transform canvas)
    {
        Transform panel = canvas.Find("SafeAreaRoot/InvalidMergePanel");
        if (panel == null) return;

        CanvasGroup grup = panel.GetComponent<CanvasGroup>();
        if (grup != null) grup.alpha = 1f;

        TMP_Text satir = panel.GetComponentInChildren<TMP_Text>(true);
        if (satir == null) return;

        NexumTheme tema = AssetDatabase.LoadAssetAtPath<NexumTheme>(NexumTheme.AssetPath);
        satir.spriteAsset = Resources.Load<TMP_SpriteAsset>(ReactionText.SpriteAssetResource);
        string denklem = tema != null
            ? ReactionText.NoReactionColored("N", "Cl", tema.palette.textSecondary, tema.palette.warning)
            : ReactionText.NoReaction("N", "Cl");
        satir.text = denklem + "   " + Loc.Get(CodeStrings.FeedbackNoReaction);
    }

    /// <summary>
    /// Quiz paneli çalışma zamanında doluyor; edit mode'da soru ve şıklar
    /// "New Text" görünüyordu. İki hâl de GERÇEK veriden kurulur:
    ///  - soru hâli: bankadaki EN UZUN soru (en kötü durum)
    ///  - cevap hâli: EN UZUN açıklama + şıklar gizli + kutu 800x700
    ///    (UIManager.ProvideFeedbackRoutine ile aynı düzen, metin
    ///     UIManager.BuildCorrectFeedback ile aynı kaynak)
    /// </summary>
    public static void QuiziDoldur(Transform canvas, bool cevapli)
    {
        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        if (ui == null) { Debug.LogError($"{Tag}: UIManager yok"); return; }

        TMP_Text soru = ui.questionTextUI;
        if (soru == null) { Debug.LogError($"{Tag}: questionTextUI atanmamış"); return; }

        List<ChemistryQuiz> banka = AssetDatabase.FindAssets("t:ChemistryQuiz")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ChemistryQuiz>)
            .Where(q => q != null)
            .ToList();
        if (banka.Count == 0) { Debug.LogError($"{Tag}: quiz bankası boş"); return; }

        soru.spriteAsset = Resources.Load<TMP_SpriteAsset>(ReactionText.SpriteAssetResource);

        if (!cevapli)
        {
            ChemistryQuiz q = banka.OrderByDescending(x => x.questionText.Length).First();
            soru.text = q.questionText;

            for (int i = 0; i < ui.optionTexts.Length && i < q.options.Length; i++)
            {
                if (ui.optionTexts[i] != null) ui.optionTexts[i].text = q.options[i];
            }
            Debug.Log($"{Tag}_QUIZ: soru hâli -> {q.name} ({q.questionText.Length} krk)");
            return;
        }

        // --- açıklamalı cevap hâli
        ChemistryQuiz enUzun = banka
            .OrderByDescending(x => Loc.Get(x.ExplanationKey).Length)
            .First();

        foreach (GameObject btn in ui.optionButtonObjects)
        {
            if (btn != null) btn.SetActive(false);
        }

        RectTransform rect = soru.rectTransform;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 700f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);

        string metin = Loc.Format(CodeStrings.QuizCorrectExplained,
                                  Loc.Get(CodeStrings.QuizCorrect),
                                  Loc.Get(enUzun.ExplanationKey),
                                  Loc.Get(CodeStrings.QuizContinueHint));
        soru.text = metin;

        Debug.Log($"{Tag}_QUIZ: cevap hâli -> {enUzun.name} " +
                  $"(açıklama {Loc.Get(enUzun.ExplanationKey).Length} krk)");
    }

    /// <summary>
    /// Ana menü hub'ı: görünürlük ve durum satırları çalışma zamanında
    /// doluyor (<c>MenuHub.Apply</c>, OnEnable'da). Edit mode'da kart
    /// boş ve yıldızlar açık görünüyordu. GERÇEK KOD YOLU çağrılır;
    /// burada yalnızca bayraklar kurulur.
    /// </summary>
    public static void HubuDoldur(bool tumBayraklar)
    {
        MenuHub hub = Object.FindObjectOfType<MenuHub>(true);
        if (hub == null) { Debug.LogError($"{Tag}: MenuHub yok"); return; }

        if (tumBayraklar) HubFeatures.TumunuAc();
        else HubFeatures.Varsayilana();

        // 2.1: şerit artık rütbe ve XP çubuğu gösteriyor. Sıfır XP'li bir
        // kayıtta çubuk boş çıkar ve görüntü hiçbir şey anlatmaz; bu yüzden
        // TEMSİLİ bir ilerleme kuruluyor (tahta ve sonuç kartı da aynı
        // şekilde dolduruluyor). KAYIT GERİ YÜKLENİR.
        string yedek = SaveService.ExportRaw();
        try
        {
            SaveService.Data.xp = 620;      // 3. rütbe (Laborant) ortası
            hub.Apply();
            Debug.Log($"{Tag}_HUB: tumBayraklar={tumBayraklar}, " +
                      $"XP={ProgressionService.Xp} rutbe={ProgressionService.RankIndex} " +
                      $"cubuk=%{ProgressionService.RankProgress01 * 100f:F0}");
        }
        finally
        {
            HubFeatures.Varsayilana();   // sonraki ekranı etkilemesin
        }

        // Kayıt geri yüklenir AMA şerit ekranda dolu kalır: Apply bir daha
        // çağrılmıyor, yani metin ve çubuk render anına kadar duruyor.
        SaveService.ImportRaw(yedek);
    }

    /// <summary>
    /// Sonuç kartı: değerleri ÇALIŞMA ZAMANINDA doluyor, edit mode'da kart
    /// boş görünürdü. Görüntü GERÇEK KOD YOLUNDAN üretilir —
    /// <c>ResultCard.Fill</c> çağrılır, yalnızca girdileri (bölüm, sayaçlar)
    /// burada kurulur. Böylece görüntü, oyuncunun göreceğinin ta kendisidir.
    /// </summary>
    public static void SonucKartiniDoldur(bool tepkimeli)
    {
        ResultCard card = Object.FindObjectOfType<ResultCard>(true);
        if (card == null) { Debug.LogError($"{Tag}: ResultCard yok"); return; }

        // Fill() LevelManager.Instance'ı okuyor; edit mode'da Awake koşmadığı
        // için elle bağlanır.
        LevelManager lm = Object.FindObjectOfType<LevelManager>(true);
        if (lm == null) { Debug.LogError($"{Tag}: LevelManager yok"); return; }

        LevelManager.Instance = lm;
        lm.currentLevelIndex = Mathf.Clamp(2, 0, Mathf.Max(0, lm.levels.Count - 1));

        LevelRun.Reset();
        LevelRun.AddScore(1240);
        for (int i = 0; i < 37; i++) LevelRun.CountMove();

        if (tepkimeli)
        {
            RecipeCatalog catalog = RecipeCatalog.Load();
            int eklenen = 0;
            if (catalog != null)
            {
                foreach (RecipeCatalog.Entry entry in catalog.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                    LevelRun.NoteDiscovery(entry.key);
                    if (++eklenen >= 5) break;      // 4 satır + "+1 daha"
                }
            }
        }

        // 2.1: "+N XP" satırı. Tutar UYDURULMAZ, tablodan hesaplanır —
        // kazanma ekranında ödenecek olanın aynısı (ProgressionService
        // çağrılmıyor ki görüntü alma kaydı değiştirmesin).
        LevelRun.NoteXp(ProgressionTable.LevelClearXp +
                        ProgressionService.ScoreXp(LevelRun.EarnedScore));

        card.Fill();
        Debug.Log($"{Tag}_SONUC: bölüm={lm.currentLevelIndex + 1} puan={LevelRun.EarnedScore} " +
                  $"hamle={LevelRun.Moves} tepkime={LevelRun.NewRecipes.Count} " +
                  $"xp={LevelRun.XpAwarded}");
    }

    /// <summary>
    /// Rütbe kutlaması: GERÇEK KOD YOLU çağrılır
    /// (<c>RankUpPanel.ShowIfPending</c>), yalnızca girdisi (bekleyen bir
    /// rütbe atlaması) kurulur. Kayıt yedeklenip geri yüklenir —
    /// görüntü alma oyuncunun kaydını değiştirmez.
    ///
    /// Kartın ölçek tween'i edit mode'da İŞLEMEZ, yani kart 0 ölçekte
    /// kalıp görünmez olurdu. Tween öldürülüp BİTİŞ durumu yazılıyor:
    /// görüntü oyuncunun kutlamanın sonunda gördüğü karedir.
    /// </summary>
    public static void KutlamayiDoldur()
    {
        RankUpPanel panel = Object.FindObjectOfType<RankUpPanel>(true);
        if (panel == null) { Debug.LogError($"{Tag}: RankUpPanel yok"); return; }

        string yedek = SaveService.ExportRaw();
        try
        {
            SaveService.Data.xp = ProgressionTable.Ranks[4].threshold;   // 5. rütbe
            SaveService.Data.rankCelebrated = 3;

            panel.gameObject.SetActive(true);
            bool acildi = panel.ShowIfPending();

            if (panel.card != null)
            {
                DG.Tweening.DOTween.Kill(panel.card.transform);
                panel.card.transform.localScale = Vector3.one;
            }

            Debug.Log($"{Tag}_KUTLAMA: acildi={acildi} rutbe={ProgressionService.RankIndex + 1}" +
                      $"/{ProgressionTable.Ranks.Length} xp={ProgressionService.Xp}");
        }
        finally
        {
            SaveService.ImportRaw(yedek);
        }
    }

    /// <summary>
    /// Bilgi kartı: bilgi metni de çalışma zamanında doluyor. Ayar kapalı
    /// istendiğinde kart parçaları UIManager'ın yaptığı gibi gizlenir.
    /// </summary>
    public static void BilgiKartiniAyarla(Transform canvas, bool kapali)
    {
        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        if (ui == null) return;

        if (ui.factTextUI != null && ui.allFacts != null && ui.allFacts.Count > 0)
        {
            ChemistryFact f = ui.allFacts.FirstOrDefault(x => x != null);
            if (f != null) ui.factTextUI.text = f.factText;
        }

        if (ui.flashcardObjects == null) return;
        foreach (GameObject go in ui.flashcardObjects)
        {
            if (go != null) go.SetActive(!kapali);
        }
    }

    /// <summary>
    /// Hedef satırı: beherler çalışma zamanında doğuyor, edit mode'da
    /// görünmüyorlardı — bu yüzden 3c'ye kadar hiçbir cihaz görüntüsünde
    /// hedef kutusu YOKTU. Artık görüntü de hedefi gösteriyor.
    /// </summary>
    public static void HedefleriDoldur()
    {
        LevelManager lm = Object.FindObjectOfType<LevelManager>(true);
        Transform goals = Object.FindObjectsOfType<RectTransform>(true)
            .FirstOrDefault(r => r.name == "GoalsContainer");
        GameObject kutuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/GoalTilePrefab.prefab");
        if (lm == null || goals == null || kutuPrefab == null || lm.levels.Count == 0) return;

        for (int i = goals.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(goals.GetChild(i).gameObject);
        }

        foreach (var hedef in lm.levels[0].levelGoals)
        {
            if (hedef.targetPrefab == null) continue;

            GameObject kutu = (GameObject)PrefabUtility.InstantiatePrefab(kutuPrefab, goals);
            RectTransform slot = kutu.transform.Find("TileSlot") as RectTransform;
            if (slot == null) continue;

            GameObject tas = (GameObject)PrefabUtility.InstantiatePrefab(hedef.targetPrefab, slot);
            RectTransform rt = tas.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(200f, 200f);
                rt.anchoredPosition = Vector2.zero;
                float olcek = slot.rect.width / 200f;
                rt.localScale = new Vector3(olcek, olcek, 1f);
            }

            TMPro.TMP_Text sayac = kutu.transform.Find("CountText").GetComponent<TMPro.TMP_Text>();
            if (sayac != null) sayac.text = "0 / " + hedef.targetAmount;
        }
    }

    /// <summary>
    /// Ansiklopedi kartları da çalışma zamanında doğuyor; liste edit mode'da
    /// boştu. Görüntü gerçek kart sayısıyla alınsın diye doldurulur.
    /// </summary>
    public static void AnsiklopediyiDoldur(Transform canvas)
    {
        Transform content = canvas.Find("SafeAreaRoot/EncyclopediaPanel/Scroll View/Viewport/Content");
        GameObject kart = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ElementCard.prefab");
        if (content == null || kart == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(content.GetChild(i).gameObject);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:ElementData"))
        {
            ElementData veri = AssetDatabase.LoadAssetAtPath<ElementData>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (veri == null) continue;

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(kart, content);
            TMPro.TMP_Text sembol = go.transform.Find("SymbolText").GetComponent<TMPro.TMP_Text>();
            TMPro.TMP_Text isim = go.transform.Find("NameText").GetComponent<TMPro.TMP_Text>();
            if (sembol != null) sembol.text = veri.symbol;
            if (isim != null) isim.text = veri.elementName;
        }
    }

    /// <summary>Oyun ekranı için tahtaya dört seviyeden taş yerleştirir.</summary>
    public static void TahtayiDoldur(Scene scene)
    {
        GridManager grid = Object.FindObjectOfType<GridManager>(true);
        if (grid == null) return;

        var cellsField = typeof(GridManager).GetField("cells",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cells = cellsField != null ? cellsField.GetValue(grid) as List<Transform> : null;

        // Hücreler Start'ta kuruluyor; edit mode'da yoklar. GridBoard'un
        // çocukları doğrudan kullanılır.
        Transform board = Object.FindObjectsOfType<RectTransform>(true)
            .FirstOrDefault(r => r.name == "GridBoard");
        if (board == null) return;

        string[] dizilim =
        {
            "Tile_H", "Tile_O", "Tile_H2O", "Tile_C",
            "Tile_Na", "Tile_Cl", "Tile_O2", "Tile_N",
            "Tile_Fe2O3", "Tile_CaO", "Tile_H", "Tile_O",
            "Tile_CO2", "Tile_N2", "Tile_Ca", "Tile_CaCO3",
        };

        for (int i = 0; i < board.childCount && i < dizilim.Length; i++)
        {
            Transform hucre = board.GetChild(i);
            for (int j = hucre.childCount - 1; j >= 0; j--) Object.DestroyImmediate(hucre.GetChild(j).gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + dizilim[i] + ".prefab");
            if (prefab == null) continue;

            GameObject tas = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hucre);
            tas.transform.localScale = Vector3.one;
            tas.transform.localPosition = Vector3.zero;
        }

        // Tahtanın bir hücresi boş kalsın: dolu/boş ayrımı görülebilsin
        if (board.childCount > 7)
        {
            Transform bos = board.GetChild(7);
            for (int j = bos.childCount - 1; j >= 0; j--) Object.DestroyImmediate(bos.GetChild(j).gameObject);
        }
    }

    /// <summary>
    /// Canvas'ı geçici olarak bir kameraya bağlar, hedef boyutta
    /// RenderTexture'a çizdirir ve PNG yazar.
    /// </summary>
    private static bool Cek(GameObject canvasGo, Vector2Int boy, string yol)
    {
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (canvas == null) return false;

        RenderMode eskiMod = canvas.renderMode;
        Camera eskiKamera = canvas.worldCamera;
        float eskiMesafe = canvas.planeDistance;

        GameObject kameraGo = new GameObject("NexumShotCamera");
        Camera kamera = kameraGo.AddComponent<Camera>();
        kamera.orthographic = true;
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = new Color(0.02f, 0.05f, 0.08f, 1f);
        kamera.nearClipPlane = 0.1f;
        kamera.farClipPlane = 1000f;
        kamera.transform.position = new Vector3(0f, 0f, -500f);

        RenderTexture rt = new RenderTexture(boy.x, boy.y, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        kamera.targetTexture = rt;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = kamera;
        canvas.planeDistance = 100f;

        // SafeArea scripti kendi hesabını dayatmasın: gerçek cihazda güvenli
        // alan tam ekran olabilir, burada ölçüm netliği için kapatılıyor.
        Transform safe = canvasGo.transform.Find("SafeAreaRoot");
        MonoBehaviour safeArea = safe != null
            ? safe.GetComponents<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea")
            : null;
        if (safeArea != null) safeArea.enabled = false;

        Canvas.ForceUpdateCanvases();
        foreach (TMP_Text t in canvasGo.GetComponentsInChildren<TMP_Text>(true)) t.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        kamera.Render();

        RenderTexture onceki = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D png = new Texture2D(boy.x, boy.y, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, boy.x, boy.y), 0, 0);
        png.Apply();
        RenderTexture.active = onceki;

        File.WriteAllBytes(yol, png.EncodeToPNG());
        Debug.Log($"{Tag}: {yol}  canvas {canvas.pixelRect.width:F0}x{canvas.pixelRect.height:F0}  " +
                  $"scaleFactor {canvas.scaleFactor:F3}" +
                  (scaler != null ? $"  referans {scaler.referenceResolution.x:F0}x{scaler.referenceResolution.y:F0}" : ""));

        Object.DestroyImmediate(png);
        kamera.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(kameraGo);

        canvas.renderMode = eskiMod;
        canvas.worldCamera = eskiKamera;
        canvas.planeDistance = eskiMesafe;
        return true;
    }
}
