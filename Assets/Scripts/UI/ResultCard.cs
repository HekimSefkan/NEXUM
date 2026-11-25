using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bölüm sonu SONUÇ KARTI (1.6b).
///
/// Kartı <c>ResultCardBuilder</c> kurar, bu script DOLDURUR. Gösterdiği her
/// değer <see cref="LevelRun"/>'dan ya da bölüm verisinden gelir; hiçbirini
/// kendisi hesaplamaz ve hiçbir oyun kuralına dokunmaz.
///
/// <b>Skor neden "kazanılan":</b> <c>GameManager.currentScore</c> geri alma
/// ve Joker'e harcananlar düşüldükten sonra KALAN tutardır. Kartta onu
/// göstermek yardım kullanan oyuncuyu cezalandırırdı; <see cref="LevelRun"/>
/// yalnızca kazanılanı sayıyor.
///
/// <b>Yıldızlar Faz 2'de:</b> üç yıldızlık satır kuruldu ama
/// <see cref="starsEnabled"/> false olduğu için gizli. Yer şimdiden ayrıldı
/// ki Faz 2'de kartın yerleşimi yeniden kurulmasın.
/// </summary>
public class ResultCard : MonoBehaviour
{
    [Header("Metinler")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI scoreValueText;
    public TextMeshProUGUI movesValueText;
    public TextMeshProUGUI reactionsText;

    [Tooltip("Bu bölümde VERİLEN XP (\"+86 XP\"). Değer LevelRun.XpAwarded'dan gelir.")]
    public TextMeshProUGUI xpValueText;

    [Header("Üretilen hedefler")]
    [Tooltip("Hedef yuvaları: taş görseli + üstünde sembol + altında adet.")]
    public Image[] goalIcons;
    public TextMeshProUGUI[] goalSymbolTexts;
    public TextMeshProUGUI[] goalAmountTexts;

    [Tooltip("Yuvalar arası yatay hatve; kullanılan yuvalar buna göre ortalanır.")]
    public float goalSpacing = 190f;

    [Header("Yıldızlar (2.2)")]
    [Tooltip("2.2'de AÇILDI. Kapatılırsa satır gizlenir, yerleşim değişmez.")]
    public bool starsEnabled;
    public GameObject starsRow;

    [Tooltip("Üç yuvanın görselleri, soldan sağa (StarRules bit sırası).")]
    public Image[] starImages;

    [Tooltip("Kazanılmış yıldız görseli.")]
    public Sprite starFullSprite;

    [Tooltip("Kazanılmamış yıldız görseli.")]
    public Sprite starEmptySprite;

    [Tooltip("Hangi koşulun kaçtığını söyleyen tek satır.")]
    public TextMeshProUGUI starHintText;

    [Header("Butonlar")]
    public GameObject nextLevelButton;
    public GameObject restartButton;

    [Header("Paylaşım geri bildirimi")]
    [Tooltip("Paylaşım açılamazsa gösterilen satır; normalde gizli.")]
    public TextMeshProUGUI shareMessageText;

    [Tooltip("Mesajın ekranda kalma süresi (saniye).")]
    public float shareMessageSeconds = 3.5f;

    /// <summary>"Sonraki bölüm" gizlendiğinde "Tekrar oyna" buraya kayar.</summary>
    [Tooltip("Sonraki bölüm gizliyken Tekrar oyna'nın x konumu (0 = ortada).")]
    public float restartSoloX;

    private float restartPairedX;
    private bool restartXCaptured;

    /// <summary>En çok kaç tepkime satırı yazılır (kutu taşmasın).</summary>
    public const int MaxReactionLines = 3;

    /// <summary>
    /// Kartı doldurur. <c>UIManager.ShowWinScreen</c> çağırır.
    /// </summary>
    public void Fill()
    {
        LevelManager lm = LevelManager.Instance;
        int index = lm != null ? lm.currentLevelIndex : 0;
        int count = lm != null ? lm.levels.Count : 0;
        LevelData data = (lm != null && index >= 0 && index < count) ? lm.levels[index] : null;

        if (levelText != null)
        {
            string ad = data != null && !string.IsNullOrWhiteSpace(data.levelName)
                ? data.levelName.Trim()
                : string.Empty;
            levelText.text = string.IsNullOrEmpty(ad)
                ? Loc.Format(CodeStrings.ResultLevelOnly, index + 1)
                : Loc.Format(CodeStrings.ResultLevelLine, index + 1, ad);
        }

        if (scoreValueText != null) scoreValueText.text = LevelRun.EarnedScore.ToString();
        FillMoves(index);
        FillStars(index);
        FillXp();

        FillGoals(data);
        FillReactions();



        // Eski paylaşım mesajı yeni kartta durmasın
        if (shareMessageText != null) shareMessageText.gameObject.SetActive(false);

        // Son bölümde sonraki yok; tek kalan buton ortaya alınır ki kartta
        // boşluk kalmasın.
        bool sonBolum = count > 0 && index >= count - 1;
        if (nextLevelButton != null) nextLevelButton.SetActive(!sonBolum);

        if (restartButton != null)
        {
            RectTransform rt = restartButton.transform as RectTransform;
            if (rt != null)
            {
                if (!restartXCaptured)
                {
                    restartPairedX = rt.anchoredPosition.x;
                    restartXCaptured = true;
                }
                float x = sonBolum ? restartSoloX : restartPairedX;
                rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            }
        }
    }

    // -------------------------------------------------------------- yıldız

    /// <summary>
    /// Hamle kutusu. Sınır VARSA "37/33" yazılır: ikinci yıldızın
    /// ölçütü tam da bu sayı, ve kutuda göstermek ayrı bir satıra
    /// gerek bırakmıyor. Sınırı olmayan bölümde yalnızca hamle yazılır.
    /// </summary>
    private void FillMoves(int levelIndex)
    {
        if (movesValueText == null) return;

        int limit = starsEnabled ? StarRules.MoveLimit(levelIndex) : -1;
        movesValueText.text = limit > 0
            ? Loc.Format(CodeStrings.ResultMovesWithLimit, LevelRun.Moves, limit)
            : LevelRun.Moves.ToString();
    }

    /// <summary>
    /// Üç yuva + TEK satırlık yönlendirme.
    ///
    /// Yuvalar BAĞIMSIZ doluyor: ikinci yıldız kaçıp üçüncü kazanılmışsa
    /// ortadaki boş, sağdaki dolu olur (<c>StarRules</c> bit maskesi).
    ///
    /// Satır TEK bir sonraki adım söyler. İki koşul birden kaçtıysa
    /// hamle satırı yazılır: ikisini birden yazmak hem kutuyu zorluyor
    /// hem de oyuncuya iki iş veriyor. Kart hesap YAPMAZ; maske
    /// <c>LevelRun.Stars</c>'tan gelir.
    /// </summary>
    private void FillStars(int levelIndex)
    {
        if (starsRow != null) starsRow.SetActive(starsEnabled);
        if (starHintText != null) starHintText.gameObject.SetActive(starsEnabled);
        if (!starsEnabled) return;

        int mask = LevelRun.Stars;

        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                bool dolu = StarRules.Has(mask, StarRules.StarAt(i));
                Sprite hedef = dolu ? starFullSprite : starEmptySprite;
                if (hedef != null) starImages[i].sprite = hedef;
            }
        }

        if (starHintText == null) return;

        if (StarRules.Has(mask, StarRules.UnderMoveLimit) && StarRules.Has(mask, StarRules.NoHelp))
        {
            starHintText.text = Loc.Get(CodeStrings.ResultStarPerfect);
            return;
        }

        if (!StarRules.Has(mask, StarRules.UnderMoveLimit))
        {
            int limit = StarRules.MoveLimit(levelIndex);
            starHintText.text = limit > 0
                ? Loc.Format(CodeStrings.ResultStarNeedMoves, limit, LevelRun.Moves)
                : Loc.Get(CodeStrings.ResultStarNeedNoHelp);
            return;
        }

        starHintText.text = Loc.Get(CodeStrings.ResultStarNeedNoHelp);
    }

    // ------------------------------------------------------------------- XP

    /// <summary>
    /// "+N XP" satırı. Kart HESAP YAPMAZ: tutarı <c>ProgressionService</c>
    /// hesaplar, <c>UIManager.ShowWinScreen</c> ödeyip
    /// <c>LevelRun.NoteXp</c> ile yazar, kart yalnızca okur. İkinci bir
    /// hesap olsaydı kartta yazan ile kayda giren sessizce ayrışabilirdi.
    ///
    /// <b>0 XP de YAZILIR.</b> Aynı bölümü tekrar oynayan oyuncu
    /// (çiftlik koruması) "+0 XP" görür; satırı gizlemek "XP sistemi
    /// bozuldu" izlenimi verirdi.
    /// </summary>
    private void FillXp()
    {
        if (xpValueText == null) return;
        xpValueText.text = Loc.Format(CodeStrings.ResultXpValue, LevelRun.XpAwarded);
    }

    // ------------------------------------------------------ paylaşım mesajı

    private Coroutine messageRoutine;

    /// <summary>
    /// Kartın üstünde kısa bir bilgi satırı gösterir. KART AÇIK KALIR —
    /// paylaşım başarısız olduğunda oyuncu kartı kaybetmemeli, tekrar
    /// deneyebilmeli.
    ///
    /// Sistem bildirimi bandı KULLANILAMAZ: o bant SafeAreaRoot altında ve
    /// LevelCompletePanel ondan SONRA çiziliyor, yani mesaj kartın arkasında
    /// kalırdı (sahne çocuk sırası ölçüldü).
    /// </summary>
    public void ShowMessage(string message)
    {
        if (shareMessageText == null) return;

        shareMessageText.text = message;
        shareMessageText.gameObject.SetActive(true);

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        if (isActiveAndEnabled) messageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        float gecen = 0f;
        while (gecen < shareMessageSeconds)
        {
            gecen += Time.unscaledDeltaTime;
            yield return null;
        }

        if (shareMessageText != null) shareMessageText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    // ----------------------------------------------------------- hedefler

    private void FillGoals(LevelData data)
    {
        if (goalIcons == null) return;

        List<LevelGoal> goals = data != null ? data.levelGoals : null;
        int kullanilan = 0;
        for (int i = 0; i < goalIcons.Length; i++)
        {
            if (goals != null && i < goals.Count && goals[i] != null) kullanilan++;
        }

        for (int i = 0; i < goalIcons.Length; i++)
        {
            bool dolu = goals != null && i < goals.Count && goals[i] != null;
            Image icon = goalIcons[i];
            if (icon == null) continue;

            Transform slot = icon.transform.parent;
            slot.gameObject.SetActive(dolu);
            if (!dolu) continue;

            // Kullanılan yuvalar ORTALANIR: üç yuvanın ikisi boşken hedef
            // sola kayıyordu (1.6b cihaz görüntüsünde görüldü).
            RectTransform slotRect = slot as RectTransform;
            if (slotRect != null)
            {
                float x = (i - (kullanilan - 1) * 0.5f) * goalSpacing;
                slotRect.anchoredPosition = new Vector2(x, slotRect.anchoredPosition.y);
            }

            // Taşın KENDİ görseli: hedef prefab'ının kök Image'ı. Formül
            // prefab'ın çocuk TMP'sinde duruyor (Tile_* yapısı), ikisi birlikte
            // gösterilmezse kartta boş bir çerçeve görünüyor.
            GameObject prefab = goals[i].targetPrefab;
            Image kaynak = prefab != null ? prefab.GetComponent<Image>() : null;
            if (kaynak != null && kaynak.sprite != null)
            {
                icon.sprite = kaynak.sprite;
                icon.color = kaynak.color;
                icon.enabled = true;
            }

            TextMeshProUGUI symbol = (goalSymbolTexts != null && i < goalSymbolTexts.Length)
                ? goalSymbolTexts[i] : null;
            if (symbol != null)
            {
                TMP_Text kaynakMetin = prefab != null
                    ? prefab.GetComponentInChildren<TMP_Text>(true) : null;
                symbol.text = kaynakMetin != null ? kaynakMetin.text : string.Empty;
            }

            TextMeshProUGUI amount = (goalAmountTexts != null && i < goalAmountTexts.Length)
                ? goalAmountTexts[i] : null;
            if (amount != null)
            {
                amount.text = Loc.Format(CodeStrings.ResultGoalAmount, goals[i].targetAmount);
            }
        }
    }

    // ---------------------------------------------------------- tepkimeler

    private void FillReactions()
    {
        if (reactionsText == null) return;

        IReadOnlyList<string> keys = LevelRun.NewRecipes;
        if (keys == null || keys.Count == 0)
        {
            reactionsText.text = Loc.Get(CodeStrings.ResultNoReactions);
            return;
        }

        // Denklem metni TEK KAYNAKTAN (ReactionText); kart kendi kalıbını
        // kurmaz (CLAUDE.md).
        ReactionText.Bind(reactionsText);

        RecipeCatalog catalog = RecipeCatalog.Load();
        StringBuilder sb = new StringBuilder();
        int yazilan = 0;

        for (int i = 0; i < keys.Count && yazilan < MaxReactionLines; i++)
        {
            string satir = EquationFor(catalog, keys[i]);
            if (string.IsNullOrEmpty(satir)) continue;

            if (yazilan > 0) sb.Append('\n');
            sb.Append(satir);
            yazilan++;
        }

        int kalan = keys.Count - yazilan;
        if (kalan > 0)
        {
            sb.Append('\n');
            sb.Append(Loc.Format(CodeStrings.ResultMoreReactions, kalan));
        }

        reactionsText.text = sb.Length > 0
            ? sb.ToString()
            : Loc.Get(CodeStrings.ResultNoReactions);
    }

    /// <summary>
    /// Anahtardan denklem metni. Semboller katalogdan, dizilim
    /// <c>ReactionText</c>'ten — tarif defteriyle AYNI kaynak.
    /// </summary>
    private static string EquationFor(RecipeCatalog catalog, string key)
    {
        if (catalog == null || string.IsNullOrEmpty(key)) return null;

        foreach (RecipeCatalog.Entry entry in catalog.entries)
        {
            if (entry == null || entry.key != key) continue;
            return ReactionText.Equation(entry.inputASymbol, entry.inputBSymbol,
                                         entry.productSymbol);
        }
        return null;
    }
}
