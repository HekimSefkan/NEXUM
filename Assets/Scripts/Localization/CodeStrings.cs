using System.Collections.Generic;

/// <summary>
/// Koddan gösterilen kullanıcı metinlerinin anahtarları ve Türkçe kaynak metni.
///
/// Kod <c>Loc.Get(CodeStrings.X)</c> çağırır; buradaki Türkçe metin yalnızca
/// sözlük derlenirken (NexumLocalize) tohum olarak kullanılır. Sözlükte anahtar
/// zaten varsa EZİLMEZ — yani çeviri/düzeltme JSON'da yapılır.
///
/// Biçimlendirme gereken metinlerde <c>{0}</c>, <c>{1}</c> kullanılır ve
/// <c>Loc.Format</c> çağrılır.
/// </summary>
public static class CodeStrings
{
    // --- İpucu asistanı ---------------------------------------------------
    public const string HintExamMode = "hint.exam_mode";
    public const string HintLimitReached = "hint.limit_reached";
    public const string HintNoBudget = "hint.no_budget";
    public const string HintNoMerge = "hint.no_merge";
    public const string HintGeneric = "hint.generic";
    public const string HintFooterFree = "hint.footer_free";
    public const string HintFooter = "hint.footer";

    // --- Sistem bildirimleri ----------------------------------------------
    public const string SystemMatrixReshuffled = "system.matrix_reshuffled";

    // --- Tepkime denklemi -------------------------------------------------
    // Ok karakteri "→" (U+2192) PROJE FONTLARININ HİÇBİRİNDE YOK ve atlaslar
    // statik. Bu yüzden ok bir TMP SPRITE olarak gömülü (NexumSymbolSprites).
    // Sprite yüklenemezse sessizce değil, loglayarak düz metin kalıbına düşülür.
    public const string ReactionInputs = "reaction.inputs";
    public const string ReactionEquation = "reaction.equation";
    public const string ReactionEquationFallback = "reaction.equation_fallback";

    // --- Tarif defteri ----------------------------------------------------
    public const string RecipeDiscovered = "recipebook.discovered";
    public const string RecipeBookTitle = "recipebook.title";
    public const string RecipeBookCounter = "recipebook.counter";
    public const string RecipeBookLockedSymbol = "recipebook.locked_symbol";
    public const string RecipeBookRingValue = "recipebook.ring_value";
    public const string RecipeBookButtonLabel = "recipebook.button";

    // --- Ana menü butonları (3b: metin görselden TMP etiketine taşındı) ---
    public const string MenuPlay = "menu.play";
    public const string MenuHowToPlay = "menu.how_to_play";
    public const string MenuEncyclopedia = "menu.encyclopedia";
    public const string MenuSettings = "menu.settings";

    // --- Oyun HUD'u ------------------------------------------------------
    public const string HudTipButton = "hud.tip_button";

    // --- Ana menü hub (3b'de ekranlara girecek) ---------------------------
    // TYT/AYT soru modunun adı "KAYNAMA NOKTASI". Menüde "sınav", "deneme"
    // ve "simülasyon" kelimeleri KULLANILMAZ; Ayarlar'daki oyun modu olan
    // "SINAV" adını korur ve ikisi artık çakışmaz.
    public const string MenuBoilingPoint = "menu.boiling_point";
    public const string MenuBoilingPointStatus = "menu.boiling_point_status";
    public const string MenuDailyPuzzle = "menu.daily_puzzle";
    public const string MenuDailyPuzzleUndone = "menu.daily_puzzle_undone";
    public const string MenuEndlessLab = "menu.endless_lab";
    public const string MenuEndlessLabBest = "menu.endless_lab_best";
    public const string MenuWeeklyExperiment = "menu.weekly_experiment";
    public const string MenuWeeklyExperimentThis = "menu.weekly_experiment_this";
    public const string MenuLevelSelect = "menu.level_select";
    public const string MenuComingSoon = "menu.coming_soon";

    // --- Ansiklopedi -------------------------------------------------------
    public const string EncyclopediaSynthesis = "encyclopedia.synthesis";
    public const string EncyclopediaBasicElement = "encyclopedia.basic_element";

    // --- Profil -----------------------------------------------------------
    public const string ProfileDefaultName = "profile.default_name";
    public const string ProfileNoEmail = "profile.no_email";
    public const string ProfileInvalidEmail = "profile.invalid_email";

    // --- Kayıt ------------------------------------------------------------
    public const string RegNoMentorChosen = "registration.no_mentor_chosen";
    public const string RegNameRequired = "registration.name_required";
    public const string RegMentorRequired = "registration.mentor_required";
    public const string RegInvalidEmail = "registration.invalid_email";
    public const string RegEmailOptional = "registration.email_optional";
    public const string RegAnonymous = "registration.anonymous";

    // --- Geri alma --------------------------------------------------------
    public const string UndoExplain = "undo.explain";

    // --- Quiz -------------------------------------------------------------
    public const string QuizCorrect = "quiz.correct";
    public const string QuizWrong = "quiz.wrong";

    // --- Katalizör --------------------------------------------------------
    public const string CatalystReadyLabel = "catalyst.ready_label";
    public const string CatalystChargeLabel = "catalyst.charge_label";
    public const string CatalystReadyMessage = "catalyst.ready_message";

    // --- Kombo ------------------------------------------------------------
    public const string Combo1 = "combo.single";
    public const string Combo2 = "combo.double";
    public const string Combo3 = "combo.chain";

    // --- Android geri tuşu ------------------------------------------------
    public const string BackPressAgain = "back.press_again";

    /// <summary>Sözlük tohumu: anahtar → Türkçe metin.</summary>
    public static readonly Dictionary<string, string> All = new Dictionary<string, string>
    {
        [HintExamMode] = "<color=red>SINAV MODU AKTİF!</color>\nSınav modunda laboratuvar asistanından yardım alamazsın. Kendi bilgine güvenmelisin Baş Kimyager!",
        [HintLimitReached] = "Bu laboratuvar seansındaki tüm asistan haklarını (3/3) tükettin Baş Kimyager! Artık kendi kimya bilgine güvenmelisin.",
        [HintNoBudget] = "Laboratuvar bütçemiz yetersiz! Asistanın {0}. ipucunu verebilmesi için <color=red>{1} puana</color> ihtiyacın var.",
        [HintNoMerge] = "Şu an matriste yapılabilecek hiçbir kimyasal sentez göremiyorum! Parçalamayı veya Geri Almayı denemelisin.",
        [HintGeneric] = "Bu iki elementi birleştirmek harika bir fikir olabilir!",
        [HintFooterFree] = "\n\n<size=80%><color=#FFD700>Serbest Mod: ipuçları bedava ve sınırsız</color></size>",
        [HintFooter] = "\n\n<size=80%><color=#FFD700>Kalan İpucu Hakkın: {0} | Sonraki Bedel: {1} Puan</color></size>",

        [SystemMatrixReshuffled] = "MATRİS YENİDEN DÜZENLENDİ",
        [ReactionInputs] = "{0} + {1}",
        [ReactionEquation] = "{0} <sprite=\"NexumSymbols\" name=\"arrow\"> {1}",
        [ReactionEquationFallback] = "{0} -> {1}",

        [RecipeDiscovered] = "YENİ TEPKİME KAYDEDİLDİ",
        [RecipeBookTitle] = "TARİF DEFTERİ",
        [RecipeBookCounter] = "{0} / {1} tepkime keşfedildi",
        [RecipeBookLockedSymbol] = "?",
        [RecipeBookRingValue] = "{0}/{1}",
        [RecipeBookButtonLabel] = "TARİF DEFTERİ",

        [MenuPlay] = "OYNA",
        [MenuHowToPlay] = "NASIL OYNANIR",
        [MenuEncyclopedia] = "ANSİKLOPEDİ",
        [MenuSettings] = "AYARLAR",

        [HudTipButton] = "İPUCU",

        [MenuBoilingPoint] = "KAYNAMA NOKTASI",
        [MenuBoilingPointStatus] = "En iyi: {0}/{1} doğru",
        [MenuDailyPuzzle] = "GÜNLÜK BULMACA",
        [MenuDailyPuzzleUndone] = "Bugün çözülmedi",
        [MenuEndlessLab] = "SONSUZ LAB",
        [MenuEndlessLabBest] = "En iyi: {0} birleşme",
        [MenuWeeklyExperiment] = "HAFTANIN DENEYİ",
        [MenuWeeklyExperimentThis] = "Bu hafta: {0}",
        [MenuLevelSelect] = "BÖLÜM SEÇİMİ",
        [MenuComingSoon] = "Yakında",

        [EncyclopediaSynthesis] = "Sentez: {0}",
        [EncyclopediaBasicElement] = "Temel element",

        [ProfileDefaultName] = "Baş Kimyager",
        [ProfileNoEmail] = "E-posta bağlanmadı",
        [ProfileInvalidEmail] = "<color=red>Kabul edilmeyen E-Posta formatı!</color>",

        [RegNoMentorChosen] = "<color=#ADB5BD>Mentor seçilmedi...</color>",
        [RegNameRequired] = "Kimyager adını yazmalısın",
        [RegMentorRequired] = "Önce bir mentor seç",
        [RegInvalidEmail] = "<color=red>Kabul edilmeyen E-Posta formatı!</color>",
        [RegEmailOptional] = "<color=#ADB5BD>Laboratuvar Kaydı (İsteğe Bağlı) - İlerlemeni farklı cihazlarda taşı</color>",
        [RegAnonymous] = "Gözlemci Kaydı (İsimsiz Ağ)",

        [UndoExplain] = "Kimyada bazı reaksiyonlar geri döndürülebilir. Bu işlem matrisi bir önceki hamleye geri alır.\n\nBedeli: <color={0}>{1} Puan</color>\nKalan Hakkın: <color={2}>{3}</color>",

        [QuizCorrect] = "<color=green>TEBRİKLER! DOĞRU CEVAP.</color>\nMatris temizleniyor, laboratuvara geri dönüyorsun!",
        [QuizWrong] = "<color=red>MAALESEF YANLIŞ CEVAP!</color>\nLaboratuvar tamamen kilitlendi.",

        [CatalystReadyLabel] = "<color=#2ECC71>BEDAVA PARÇALAMA ×{0}</color>",
        [CatalystChargeLabel] = "KATALİZÖR ŞARJI {0}/{1}",
        [CatalystReadyMessage] = "<color=#2ECC71>KATALİZÖR HAZIR! Joker'i bir kez bedava kullanabilirsin.</color>",

        [Combo1] = "BAŞARILI SENTEZ!",
        [Combo2] = "ÇİFTE BAĞ!",
        [Combo3] = "ZİNCİRLEME REAKSİYON!",

        [BackPressAgain] = "Çıkmak için tekrar basın",
    };
}
