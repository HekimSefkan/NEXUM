using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// GEÇERSİZ BİRLEŞTİRME GERİ BİLDİRİMİ (Faz 1 / 1.3).
///
/// Oyuncu iki taşı birbirine itiyor, tepkime olmuyor ve oyun hiçbir şey
/// söylemiyordu. Bir kimya oyununda bu kaybedilmiş bir öğretme anı: oyuncu
/// "neden olmadı?" sorusunun cevabını tahminle buluyordu.
///
/// <b>TETİKLEME — kesin kural.</b> Yalnızca oyuncunun hamlesi SIFIR birleşme
/// ürettiğinde. Çağrı tek yerden gelir: <c>GridManager.Shift</c> içindeki
/// "birleşme olmadı ama taş kaydı" dalı. Yani:
///   · en az bir birleşme olduysa ÇAĞRILMAZ,
///   · hiç taş hareket etmediyse ÇAĞRILMAZ,
///   · hamle başına EN FAZLA BİR kez çağrılır.
/// Aynı çift arka arkaya gelirse, önceki mesaj daha ekrandayken tekrarlanmaz
/// (bekleme süresi temanın mevcut motion değerlerinden türetilir; yeni
/// zamanlama sabiti yoktur).
///
/// <b>GÖSTERİLEN ÇİFT.</b> Hamle yönünde en öndeki taşın, önünde durduğu
/// taşla oluşturduğu çift. Hamleden sonra tahtada o yönde komşu olan her
/// çift tanım gereği birleşmeyen bir çifttir (birleşenler zaten birleşirdi),
/// bu yüzden duvara en yakın komşu çift seçilir.
///
/// <b>OYUN MANTIĞI DEĞİŞMEZ.</b> Bu sınıf tahtayı yalnızca OKUR; kaydırma,
/// birleşme, doğma, puanlama ve katalizör kurallarına dokunmaz.
/// </summary>
public class InvalidMergeFeedback : MonoBehaviour
{
    public static InvalidMergeFeedback Instance;

    /// <summary>
    /// Denklem ve açıklama TEK SATIRDA: tahtanın altındaki boş bant 60
    /// birim yüksekliğinde (ölçüldü) — iki satır sığmaz, sığdırmak için
    /// katalizörün ya da sistem bildiriminin bandına girmek gerekirdi.
    /// </summary>
    [Tooltip("\"N + Cl ✕  Bu ikisi tepkimeye girmiyor.\" (InvalidMergeBuilder bağlar).")]
    public TMP_Text lineText;

    [Tooltip("Renk ve süreler buradan gelir (InvalidMergeBuilder bağlar).")]
    public NexumTheme theme;

    [Tooltip("Bandın sönümlenmesi (InvalidMergeBuilder bağlar).")]
    public CanvasGroup group;

    /// <summary>Son gösterilen çift ve gösterim zamanı (tekrarı engeller).</summary>
    private string sonCift;
    private float sonZaman = float.NegativeInfinity;

    private Sequence akis;

    private void Awake()
    {
        Instance = this;
        if (group != null) group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------- kurallar

    /// <summary>
    /// Ayarlar ve mod: SINAV modunda yardım yok; "Mentor İpuçları" kapalıysa
    /// oyuncu yardım istemiyordur. Serbest modda açıktır.
    /// </summary>
    public static bool Enabled
    {
        get
        {
            if (SaveService.Data.mentorHintsOn != 1) return false;

            GridManager grid = GridManager.Instance;
            int mod = grid != null ? grid.currentGameMode : SaveService.Data.gameMode;
            return mod != 1;   // 1 = sınav modu
        }
    }

    /// <summary>Mesajın ekranda kalma süresi (temadan; yeni sabit yok).</summary>
    private float Omur
    {
        get
        {
            if (theme == null) return 1.2f;
            return theme.motion.durationLong + theme.motion.durationFade;
        }
    }

    // ------------------------------------------------------------- tetikleme

    /// <summary>
    /// Sıfır birleşme üreten bir hamleden sonra çağrılır.
    /// Tahtayı okur, çifti bulur, mesajı gösterir ve taşları geri tepkitir.
    /// </summary>
    public void Report(Vector2 direction)
    {
        if (!Enabled) return;

        Transform iten, engel;
        if (!CiftBul(direction, out iten, out engel)) return;

        string a = ReactionText.Symbol(iten.gameObject);
        string b = ReactionText.Symbol(engel.gameObject);
        string anahtar = a + "|" + b;

        // Aynı çift, önceki mesaj daha ekrandayken tekrar gelmez
        if (anahtar == sonCift && Time.unscaledTime - sonZaman < Omur) return;

        sonCift = anahtar;
        sonZaman = Time.unscaledTime;

        Goster(a, b);
        GeriTep(iten, direction);
        GeriTep(engel, direction);

        if (AudioManager.Instance != null)
        {
            // Klip atanmamışsa PlaySFX sessizce geçer (ses dosyası aranmadı).
            AudioManager.Instance.PlaySFX(AudioManager.Instance.invalidMergeClip);
        }
    }

    /// <summary>
    /// Hamle yönünde duvara EN YAKIN komşu çift. Bulunamazsa false
    /// (ör. tahtada o yönde hiç komşu taş kalmamışsa).
    /// </summary>
    public bool CiftBul(Vector2 direction, out Transform iten, out Transform engel)
    {
        iten = null;
        engel = null;

        GridManager grid = GridManager.Instance;
        if (grid == null || grid.gridBoard == null) return false;

        Transform board = grid.gridBoard;
        if (board.childCount < 16) return false;

        int step = Adim(direction);
        if (step == 0) return false;

        // Duvar tarafından başlayarak tara: ilk bulunan komşu çift en öndeki.
        foreach (int i in Sira(direction))
        {
            int onIndex = i + step;
            if (onIndex < 0 || onIndex >= 16) continue;
            if (!AyniHat(i, onIndex, step)) continue;

            Transform arka = board.GetChild(i);
            Transform on = board.GetChild(onIndex);
            if (arka.childCount == 0 || on.childCount == 0) continue;

            iten = arka.GetChild(0);
            engel = on.GetChild(0);
            return true;
        }
        return false;
    }

    /// <summary>Hücre taraması: hedef kenara en yakın hücreden başlar.</summary>
    private static int[] Sira(Vector2 direction)
    {
        int[] s = new int[16];
        for (int i = 0; i < 16; i++) s[i] = i;

        if (direction == Vector2.up || direction == Vector2.left) return s;

        // Aşağı/sağa giderken duvar yüksek indekslerde
        System.Array.Reverse(s);
        return s;
    }

    private static int Adim(Vector2 direction)
    {
        if (direction == Vector2.up) return -4;
        if (direction == Vector2.down) return 4;
        if (direction == Vector2.left) return -1;
        if (direction == Vector2.right) return 1;
        return 0;
    }

    /// <summary>Yatay hareket satır atlamamalı.</summary>
    private static bool AyniHat(int a, int b, int step)
    {
        if (step == 1 || step == -1) return a / 4 == b / 4;
        return true;
    }

    // ------------------------------------------------------------- gösterim

    private void Goster(string a, string b)
    {
        if (lineText != null)
        {
            ReactionText.Bind(lineText);

            string denklem = theme != null
                ? ReactionText.NoReactionColored(a, b, theme.palette.textSecondary,
                                                 theme.palette.warning)
                : ReactionText.NoReaction(a, b);

            lineText.text = denklem + "   " + Loc.Get(CodeStrings.FeedbackNoReaction);
        }

        if (group == null) return;

        float appear = theme != null ? theme.motion.durationShort : 0.15f;
        float hold = theme != null ? theme.motion.durationLong : 0.5f;
        float fade = theme != null ? theme.motion.durationFade : 0.35f;

        if (akis != null && akis.IsActive()) akis.Kill();
        group.alpha = 0f;

        akis = DOTween.Sequence().SetUpdate(true);
        akis.Append(group.DOFade(1f, appear));
        akis.AppendInterval(hold);
        akis.Append(group.DOFade(0f, fade));
    }

    /// <summary>
    /// Çarpışan taşlar kısa bir geri tepme yapar. Süre MEVCUT hareket
    /// süresinden, mesafe TAŞIN KENDİ boyutundan gelir — yeni sabit yok.
    ///
    /// Geri tepme KAYMA BİTTİKTEN SONRA başlar: kayma tween'ini ortasında
    /// öldürmek taşı zıplatırdı. Başlangıç değerini yakalayan tween'den
    /// önce <c>DOKill(true)</c> çağrılır (CLAUDE.md kuralı): üst üste binen
    /// punch'ların kalıcı kayma bırakmaması için.
    /// </summary>
    private static void GeriTep(Transform tile, Vector2 direction)
    {
        if (tile == null) return;

        RectTransform rt = tile as RectTransform;
        float mesafe = rt != null ? rt.rect.width * 0.08f : 16f;
        Vector3 yon = new Vector3(-direction.x, -direction.y, 0f) * mesafe;

        DOTween.Sequence()
            .AppendInterval(GridManager.MoveAnimDuration)
            .AppendCallback(() =>
            {
                if (tile == null) return;
                tile.DOKill(true);
                tile.localPosition = Vector3.zero;
                tile.DOPunchPosition(yon, GridManager.MoveAnimDuration, 1, 0.4f);
            });
    }
}
