using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// İPUCU butonunun rozeti ve devre dışı durumu.
///
/// Rozet AYRI KATMAN: Faz 5'teki kademeli ipucu bu rozeti değiştirecek,
/// butonun kendisi değişmeyecek. Bu sınıf yalnızca OKUR — hak sayısını,
/// bedeli ve modu <see cref="GridManager"/> belirler; burada hiçbir oyun
/// kuralı yoktur.
///
/// Durumlar:
///   Sınav modu  -> devre dışı, rozet gizli (ipucu zaten kapalı)
///   Serbest mod -> etkin, rozet gizli (ipucu bedava ve sınırsız)
///   Normal mod  -> rozet kalan hakkı gösterir; hak bitince devre dışı
/// </summary>
[RequireComponent(typeof(Button))]
public class HintButtonState : MonoBehaviour
{
    [Tooltip("Rozet kökü (gizlenip gösterilir).")]
    public GameObject badge;

    [Tooltip("Rozetin sayı metni.")]
    public TMP_Text badgeText;

    private Button button;
    private int sonKalan = int.MinValue;
    private bool sonEtkin = true;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Update()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        bool sinav = grid.currentGameMode == 1;
        bool serbest = grid.IsFreeMode;

        bool rozetGorunur = !sinav && !serbest;
        int kalan = Mathf.Max(0, grid.maxHints - grid.usedHints);
        bool etkin = !sinav && (serbest || kalan > 0);

        if (badge != null && badge.activeSelf != rozetGorunur) badge.SetActive(rozetGorunur);

        if (rozetGorunur && badgeText != null && kalan != sonKalan)
        {
            badgeText.text = kalan.ToString();
            sonKalan = kalan;
        }

        if (button != null && etkin != sonEtkin)
        {
            button.interactable = etkin;
            sonEtkin = etkin;
        }
    }
}
