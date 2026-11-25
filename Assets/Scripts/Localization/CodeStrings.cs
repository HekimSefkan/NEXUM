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

    // --- Geçersiz birleştirme geri bildirimi (1.3) -----------------------
    // "Tepkime yok" sembolü de (✕, U+2715) fontlarda yok; ok ile AYNI
    // sayfadan gelen bir TMP sprite'ı. Sprite yoksa düz metne düşülür.
    public const string ReactionNoProduct = "reaction.no_product";
    public const string ReactionNoProductFallback = "reaction.no_product_fallback";
    public const string FeedbackNoReaction = "feedback.no_reaction";

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

    // --- Ana menü hub (1.7) ----------------------------------------------
    // Durum satırları GERÇEK KAYITTAN gelir; sahte değer yok.
    public const string HubHero = "hub.hero";
    public const string HubHeroStatus = "hub.hero_status";
    public const string HubLevelStatus = "hub.level_status";
    public const string HubRecipeStatus = "hub.recipe_status";
    public const string HubRecipeBook = "hub.recipe_book";
    public const string HubEncyclopedia = "hub.encyclopedia";
    public const string HubSettings = "hub.settings";
    public const string HubHowToPlay = "hub.how_to_play";
    public const string HubMentor = "hub.mentor";
    public const string HubSound = "hub.sound";

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

    // --- Bölüm sonu sonuç kartı (1.6b) ------------------------------------
    public const string ResultLevelLine = "result.level";
    public const string ResultLevelOnly = "result.level_only";
    public const string ResultScoreLabel = "result.score_label";
    public const string ResultMovesLabel = "result.moves_label";
    public const string ResultGoalsLabel = "result.goals_label";
    public const string ResultGoalAmount = "result.goal_amount";
    public const string ResultReactionsLabel = "result.reactions_label";
    public const string ResultNoReactions = "result.no_reactions";
    public const string ResultMoreReactions = "result.more_reactions";
    public const string ResultNext = "result.next";
    public const string ResultRestart = "result.restart";
    public const string ResultShare = "result.share";
    public const string ResultMenu = "result.menu";

    // Paylaşım metni. KİŞİSEL VERİ TAŞIMAZ: oyuncu adı, e-posta, bağlantı yok.
    public const string ShareSubject = "share.subject";
    public const string ShareText = "share.text";

    // Paylaşım başarısız olursa oyuncuya gösterilen satır. Teknik ayrıntı
    // VERMEZ (paket adı, yığın izi); oyuncunun yapabileceği tek şey
    // tekrar denemek.
    public const string ShareFailed = "share.failed";

    // --- Quiz -------------------------------------------------------------
    public const string QuizCorrect = "quiz.correct";
    public const string QuizWrong = "quiz.wrong";

    // Doğru cevap panelinin açıklamalı hâli (1.5). Kalıp sözlükte: başlık,
    // açıklama ve "dokun" ipucu farklı puntolarda dizilir, böylece çeviri
    // sırasında dizilim de ayarlanabilir. Açıklama METNİ soru başına ayrı
    // anahtardadır (QuizStrings, "quiz.soru_7.explain").
    public const string QuizCorrectExplained = "quiz.correct_explained";
    public const string QuizContinueHint = "quiz.continue_hint";

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

    // --- Rütbe (Faz 2 / 2.1) ---------------------------------------------
    // Yedi rütbenin adı. Sıra ve eşikler ProgressionTable'da; metin yalnızca
    // burada. İsim DEĞİŞTİRİLECEKSE tek yer burasıdır.
    public const string Rank0 = "rank.apprentice";
    public const string Rank1 = "rank.trainee";
    public const string Rank2 = "rank.technician";
    public const string Rank3 = "rank.researcher";
    public const string Rank4 = "rank.chemist";
    public const string Rank5 = "rank.chief";
    public const string Rank6 = "rank.nobel";

    public const string RankUpTitle = "rank.up_title";
    public const string RankUpHint = "rank.up_hint";

    /// <summary>Kutlamada gösterilen toplam XP.</summary>
    public const string RankXpTotal = "rank.xp_total";

    /// <summary>Sonuç kartındaki deneyim satırı.</summary>
    public const string ResultXpLabel = "result.xp_label";
    public const string ResultXpValue = "result.xp_value";

    // --- Yıldızlar (Faz 2 / 2.2) -----------------------------------------
    /// <summary>Hamle kutusu: "37/33" (sınır varsa).</summary>
    public const string ResultMovesWithLimit = "result.moves_with_limit";

    /// <summary>Sonuç kartında yıldız durumu: tek satır, TEK sonraki adım.</summary>
    public const string ResultStarPerfect = "result.star_perfect";
    public const string ResultStarNeedMoves = "result.star_need_moves";
    public const string ResultStarNeedNoHelp = "result.star_need_nohelp";

    // --- Rozetler (Faz 2 / 2.1) ------------------------------------------
    // Verisi bugün OLMAYAN rozetlerin adı da burada: rozet tablosu tek
    // kaynak olsun, veri geldiğinde yalnızca bayrak açılsın.
    public const string BadgeFirstSynthesis = "badge.first_synthesis";
    public const string BadgeAllReactions = "badge.all_reactions";
    public const string BadgeFlawlessLevel = "badge.flawless_level";
    public const string BadgeStreak7 = "badge.streak_7";
    public const string BadgeStreak30 = "badge.streak_30";
    public const string BadgeStreak100 = "badge.streak_100";
    public const string BadgeEncyclopedia = "badge.encyclopedia";
    public const string BadgeThreeStars = "badge.three_stars";
    public const string BadgeDailyPuzzle10 = "badge.daily_puzzle_10";

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
        [ReactionNoProduct] = "<sprite=\"NexumSymbols\" name=\"nope\">",
        [ReactionNoProductFallback] = "X",
        [FeedbackNoReaction] = "Bu ikisi tepkimeye girmiyor.",

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

        [HubHero] = "DENEYİ BAŞLAT",
        [HubHeroStatus] = "Bölüm {0} · kaldığın yerden",
        [HubLevelStatus] = "{0} / {1} açık",
        [HubRecipeStatus] = "{0} / {1} tepkime",
        [HubRecipeBook] = "TARİF DEFTERİ",
        [HubEncyclopedia] = "ANSİKLOPEDİ",
        [HubSettings] = "Ayarlar",
        [HubHowToPlay] = "Nasıl Oynanır",
        [HubMentor] = "Mentor",
        [HubSound] = "Ses",
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

        [ResultLevelLine] = "BÖLÜM {0} · {1}",
        [ResultLevelOnly] = "BÖLÜM {0}",
        [ResultScoreLabel] = "KAZANILAN PUAN",
        [ResultMovesLabel] = "HAMLE",
        [ResultGoalsLabel] = "ÜRETİLEN",
        [ResultGoalAmount] = "x{0}",
        [ResultReactionsLabel] = "YENİ TEPKİMELER",
        [ResultNoReactions] = "Bu bölümde yeni tepkime keşfetmedin.",
        [ResultMoreReactions] = "+{0} tepkime daha",
        [ResultNext] = "SONRAKİ BÖLÜM",
        [ResultRestart] = "TEKRAR OYNA",
        [ResultShare] = "PAYLAŞ",
        [ResultMenu] = "ANA MENÜ",
        [ShareSubject] = "NEXUM",
        [ShareText] = "NEXUM laboratuvarında bir bölümü tamamladım!",
        [ShareFailed] = "Paylaşım açılamadı. Daha sonra tekrar dene.",

        [QuizCorrect] = "<color=green>TEBRİKLER! DOĞRU CEVAP.</color>\nMatris temizleniyor, laboratuvara geri dönüyorsun!",
        [QuizWrong] = "<color=red>MAALESEF YANLIŞ CEVAP!</color>\nLaboratuvar tamamen kilitlendi.",
        [QuizCorrectExplained] = "{0}\n\n<size=72%>{1}</size>\n\n<size=58%><color=#ADB5BD>{2}</color></size>",
        [QuizContinueHint] = "Devam etmek için ekrana dokun",

        [CatalystReadyLabel] = "<color=#2ECC71>BEDAVA PARÇALAMA ×{0}</color>",
        [CatalystChargeLabel] = "KATALİZÖR ŞARJI {0}/{1}",
        [CatalystReadyMessage] = "<color=#2ECC71>KATALİZÖR HAZIR! Joker'i bir kez bedava kullanabilirsin.</color>",

        [Combo1] = "BAŞARILI SENTEZ!",
        [Combo2] = "ÇİFTE BAĞ!",
        [Combo3] = "ZİNCİRLEME REAKSİYON!",

        [BackPressAgain] = "Çıkmak için tekrar basın",

        // Rütbe adları — ONAYLANDI (25 Kasım 2026; bkz. docs/DESIGN_TOKENS.md 12.2).
        // "Stajyer" bilerek kısa: uzun hâli ("Stajyer Kimyager") profil
        // şeridindeki 300 birimlik kutuyu %99 dolduruyor (sınır %85).
        [Rank0] = "Çırak",
        [Rank1] = "Stajyer",
        [Rank2] = "Laborant",
        [Rank3] = "Araştırmacı",
        [Rank4] = "Kimyager",
        [Rank5] = "Baş Kimyager",
        [Rank6] = "Nobel Adayı",

        [RankUpTitle] = "RÜTBE ATLADIN",
        [RankUpHint] = "Devam etmek için dokun",

        [RankXpTotal] = "{0} XP",

        [ResultXpLabel] = "DENEYİM",
        [ResultXpValue] = "+{0} XP",

        [ResultMovesWithLimit] = "{0}/{1}",
        [ResultStarPerfect] = "Üç yıldız! Kusursuz sentez.",
        [ResultStarNeedMoves] = "Hamle sınırı {0} — sen {1} hamlede bitirdin",
        [ResultStarNeedNoHelp] = "Yardımsız bitir: ipucu ve ücretli Joker yok",

        [BadgeFirstSynthesis] = "İlk Sentez",
        [BadgeAllReactions] = "Tüm Tepkimeler",
        [BadgeFlawlessLevel] = "Kazasız Bölüm",
        [BadgeStreak7] = "7 Gün Seri",
        [BadgeStreak30] = "30 Gün Seri",
        [BadgeStreak100] = "100 Gün Seri",
        [BadgeEncyclopedia] = "Ansiklopedi Kurdu",
        [BadgeThreeStars] = "Üç Yıldızlı Bölüm",
        [BadgeDailyPuzzle10] = "Günlük Bulmaca x10",
    };
}
