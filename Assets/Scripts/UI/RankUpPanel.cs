using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RÜTBE ATLAMA KUTLAMASI (Faz 2 / 2.1).
///
/// <b>ÖLÇÜLÜ OLMAK TASARIM KARARI.</b> Oyuncu daha önce katalizörün tam
/// ekran yeşil parlamasından rahatsız olmuştu. Bu panelde:
///  - tam ekran parlama / flaş YOK (zemin sabit alfalı koyu bir perde),
///  - sürekli yanıp sönme YOK (tek seferlik ölçek tween'i, döngü yok),
///  - kendiliğinden kapanma da YOK: oyuncu DOKUNUNCA kapanır.
/// Süre yeni bir sabit değil, temadan geliyor (<c>NexumTheme.motion</c>);
/// kurucu değeri serileştirir.
///
/// <b>OYUNU KESMEZ.</b> Panel yalnızca bölüm bitişinde (sonuç kartı
/// göründükten sonra) ve ana menüde (geriye dönük hesabın ardından)
/// açılır; oynanışın ortasında açılacağı bir yol yok.
///
/// <b>BİR KEZ.</b> Kapanınca <c>ProgressionService.MarkCelebrationShown</c>
/// çağrılır; aynı rütbe için panel bir daha açılmaz. Geriye dönük hesapla
/// rütbe atlayan oyuncu kutlamayı her açılışta değil bir kez görür.
///
/// İki sahnede de kurulu (<c>RankUpPanelBuilder</c>): kutlama hem oyunda hem
/// menüde gerekiyor ve "paneli aç" eylemi asla sahne yüklemesi olarak
/// yazılmaz (CLAUDE.md panel erişimi kuralı).
/// </summary>
public class RankUpPanel : MonoBehaviour
{
    private static RankUpPanel instance;

    /// <summary>
    /// Sahnedeki örnek. İki sahnede de bir tane var.
    ///
    /// <b>TEMBEL ÇÖZÜM ŞART:</b> panel sahnede KAPALI duruyor, yani
    /// <c>Awake</c> çalışmıyor. Yalnızca <c>Awake</c>'te atanan bir
    /// <c>Instance</c> sonsuza kadar null kalırdı ve geri tuşu kutlamayı
    /// görmezdi. Bu yüzden kapalı objeleri de tarayan arama yapılır
    /// (<c>FindObjectOfType(true)</c>) ve sonuç önbelleğe alınır.
    /// </summary>
    public static RankUpPanel Instance
    {
        get
        {
            if (instance == null) instance = FindObjectOfType<RankUpPanel>(true);
            return instance;
        }
    }

    [Header("Parçalar (UIBuilder bağlar)")]
    [Tooltip("Ölçek tween'i uygulanan kart. Panelin kendisi perde olarak kalır.")]
    public GameObject card;

    [Tooltip("Yeni rütbenin adı (koddan yazılır).")]
    public TextMeshProUGUI rankNameText;

    [Tooltip("Yeni çerçeve: rozet madalya çerçevesi.")]
    public Image rankFrame;

    [Tooltip("Rütbe numarası (\"5 / 7\").")]
    public TextMeshProUGUI rankStepText;

    [Header("Süre (temadan)")]
    [Tooltip("Kartın ölçek tween süresi. NexumTheme.motion.durationMedium.")]
    public float appearDuration = 0.3f;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>Panel şu an açık mı? (girdi kilidi ve geri tuşu bunu okur.)</summary>
    public bool IsOpen
    {
        get { return gameObject.activeInHierarchy; }
    }

    /// <summary>
    /// Bekleyen bir rütbe atlaması varsa kutlamayı açar ve true döner.
    /// Yoksa hiçbir şey yapmaz — çağıran taraf koşul yazmaz.
    /// </summary>
    public bool ShowIfPending()
    {
        if (!ProgressionService.CelebrationPending) return false;

        if (rankNameText != null)
        {
            rankNameText.text = Loc.Get(ProgressionService.RankNameKey);
        }

        if (rankStepText != null)
        {
            rankStepText.text = Loc.Format(CodeStrings.RankXpTotal, ProgressionService.Xp);
        }

        gameObject.SetActive(true);

        // Tek seferlik ölçek: döngü yok, parlama yok. DOKill üst üste binen
        // tween'e karşı (CLAUDE.md göreli tween kuralı).
        if (card != null)
        {
            card.transform.DOKill(true);
            card.transform.localScale = Vector3.zero;
            card.transform.DOScale(Vector3.one, appearDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        Debug.Log($"NEXUM_XP: kutlama acildi -> rutbe {ProgressionService.RankIndex} " +
                  $"({ProgressionService.RankNameKey}), XP {ProgressionService.Xp}");
        return true;
    }

    /// <summary>
    /// Dokununca kapanır. Kapanış kutlamayı GÖSTERİLMİŞ olarak işaretler;
    /// aynı rütbe için panel bir daha açılmaz.
    /// </summary>
    public void Close()
    {
        if (!IsOpen) return;

        if (card != null) card.transform.DOKill(true);
        ProgressionService.MarkCelebrationShown();
        gameObject.SetActive(false);

        if (UIManager.Instance != null) UIManager.Instance.PlayButtonSound();
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound();
    }
}
