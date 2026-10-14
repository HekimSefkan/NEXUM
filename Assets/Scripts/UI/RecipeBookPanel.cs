using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarif defteri paneli: 21 tepkimenin listesi, keşif sayacı ve ilerleme halkası.
///
/// Keşfedilen tarif "A + B [ok] C" biçiminde, denklem gösterimiyle AYNI
/// kalıptan ve AYNI ok sprite'ından gelir (<see cref="ReactionText"/>).
/// Keşfedilmeyen satır "? + ? [ok] ?" yazar, sönük görünür ve kilit ikonu taşır.
/// Keşfedilene dokununca o bileşiğin ansiklopedi kartı açılır.
///
/// Satırlar sahnede önceden kurulur (UIBuilder); panel açılınca yalnızca
/// metinleri güncellenir.
/// </summary>
public class RecipeBookPanel : MonoBehaviour
{
    [Tooltip("Renkler buradan gelir (UIBuilder bağlar).")]
    public NexumTheme theme;

    public ScrollRect scrollRect;
    public TextMeshProUGUI counterText;
    public Image progressRing;
    public Transform rowContainer;

    [Tooltip("Keşfedilen tarife dokununca açılacak ansiklopedi (UIBuilder bağlar).")]
    public EncyclopediaManager encyclopedia;

    private readonly List<TextMeshProUGUI> rowTexts = new List<TextMeshProUGUI>();
    private readonly List<Button> rowButtons = new List<Button>();
    private readonly List<GameObject> rowLocks = new List<GameObject>();

    private void Awake()
    {
        CacheRows();
    }

    private void CacheRows()
    {
        if (rowTexts.Count > 0 || rowContainer == null) return;

        foreach (Transform row in rowContainer)
        {
            Transform label = row.Find("Label");
            TextMeshProUGUI text = label != null
                ? label.GetComponent<TextMeshProUGUI>()
                : row.GetComponentInChildren<TextMeshProUGUI>(true);

            Transform lock_ = row.Find("LockIcon");

            if (text == null) continue;

            rowTexts.Add(text);
            rowButtons.Add(row.GetComponent<Button>());
            rowLocks.Add(lock_ != null ? lock_.gameObject : null);
        }
    }

    private void OnEnable()
    {
        CacheRows();
        RecipeBook.RunBackfillOnce();
        Refresh();
        StartCoroutine(ResetScrollNextFrames());
    }

    /// <summary>
    /// Panel her açılışta en üstten başlar. ForceRebuildLayoutImmediate KULLANILMAZ
    /// (ContentSizeFitter ile sürülen içerikte yasak); panel etkin olduğu karede
    /// ScrollRect'in sınırları güncel olmadığı için iki kare boyunca sıfırlanır.
    /// </summary>
    private IEnumerator ResetScrollNextFrames()
    {
        for (int i = 0; i < 2; i++)
        {
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                if (scrollRect.content != null) scrollRect.content.anchoredPosition = Vector2.zero;
            }
            yield return null;
        }
    }

    /// <summary>Listeyi, sayacı ve halkayı güncel kayda göre yazar.</summary>
    public void Refresh()
    {
        RecipeCatalog catalog = RecipeCatalog.Load();
        if (catalog == null) return;

        Color dim = theme != null ? theme.palette.textSecondary : Color.gray;
        Color bright = theme != null ? theme.palette.primary : Color.cyan;
        string kilit = Loc.Get(CodeStrings.RecipeBookLockedSymbol);

        int kesfedilen = 0;

        for (int i = 0; i < rowTexts.Count; i++)
        {
            bool varMi = i < catalog.entries.Count;
            rowTexts[i].transform.parent.gameObject.SetActive(varMi);
            if (!varMi) continue;

            RecipeCatalog.Entry entry = catalog.entries[i];
            bool acik = RecipeBook.IsDiscovered(entry.key);
            if (acik) kesfedilen++;

            rowTexts[i].text = acik
                ? ReactionText.Colored(entry.inputASymbol, entry.inputBSymbol, entry.productSymbol, dim, bright)
                : ReactionText.Colored(kilit, kilit, kilit, dim, dim);

            if (rowLocks[i] != null) rowLocks[i].SetActive(!acik);

            if (rowButtons[i] != null)
            {
                rowButtons[i].interactable = acik;
                int index = i;
                rowButtons[i].onClick.RemoveAllListeners();
                if (acik) rowButtons[i].onClick.AddListener(() => OpenCard(index));
            }
        }

        int toplam = catalog.entries.Count;
        if (counterText != null) counterText.text = Loc.Format(CodeStrings.RecipeBookCounter, kesfedilen, toplam);
        if (progressRing != null) progressRing.fillAmount = toplam > 0 ? (float)kesfedilen / toplam : 0f;
    }

    private void OpenCard(int index)
    {
        RecipeCatalog catalog = RecipeCatalog.Load();
        if (catalog == null || index >= catalog.entries.Count) return;

        ElementData data = catalog.entries[index].productData;
        if (data == null || encyclopedia == null) return;

        encyclopedia.OpenDetailPanel(data);
    }

    // ------------------------------------------------------------ test erişimi

    /// <summary>Test için: satır metni.</summary>
    public string RowText(int index)
    {
        CacheRows();
        return index >= 0 && index < rowTexts.Count ? rowTexts[index].text : null;
    }

    /// <summary>Test için: satırdaki kilit ikonu görünür mü?</summary>
    public bool RowLocked(int index)
    {
        CacheRows();
        if (index < 0 || index >= rowLocks.Count || rowLocks[index] == null) return false;
        return rowLocks[index].activeSelf;
    }

    /// <summary>Test için: görünen satır sayısı.</summary>
    public int RowCount
    {
        get
        {
            CacheRows();
            return rowTexts.Count;
        }
    }
}
