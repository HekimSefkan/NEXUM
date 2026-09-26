using UnityEngine;
using TMPro;
using UnityEngine.UI; // Buton tıklamaları için şart

public class EncyclopediaManager : MonoBehaviour
{
    [Header("UI Bağlantıları")]
    public GameObject cardPrefab; 
    public Transform contentArea; 

    [Header("Detay Paneli (Pop-up) Bağlantıları")]
    public GameObject detailPanel; // Ortada açılacak dev panel
    public TextMeshProUGUI detailNameText; // Başlık (Örn: Karbon (C))
    public TextMeshProUGUI detailRecipeText;
    public TextMeshProUGUI detailScoreText;
    public TextMeshProUGUI detailDescText; // O efsanevi mizahi açıklamaların

    void Start()
    {
        // Oyun başladığında pop-up paneli kapalı dursun
        if(detailPanel != null) detailPanel.SetActive(false); 
        
        LoadEncyclopedia();
    }

    public void LoadEncyclopedia()
    {
        // Idempotent olsun: kartlar normalde Start'ta bir kez üretilir ve panel kapanıp açılınca korunur.
        // Bu metot ileride bir butona bağlanırsa kartlar kopyalanmasın diye eskiler önce temizlenir.
        for (int i = contentArea.childCount - 1; i >= 0; i--)
        {
            Transform oldCard = contentArea.GetChild(i);
            oldCard.SetParent(null);   // Destroy ertelendiği için hemen listeden çıkar
            Destroy(oldCard.gameObject);
        }

        ElementData[] allElements = Resources.LoadAll<ElementData>("Elements");

        foreach (ElementData element in allElements)
        {
            GameObject newCard = Instantiate(cardPrefab, contentArea);

            // Sadece Sembol ve İsmi karta basıyoruz (Çünkü diğerlerini prefab'dan sildik)
            newCard.transform.Find("SymbolText").GetComponent<TextMeshProUGUI>().text = element.symbol;
            newCard.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = element.elementName;

            // KARTA TIKLAMA ÖZELLİĞİ EKLİYORUZ (Sihir burada!)
            Button cardButton = newCard.GetComponent<Button>();
            if (cardButton != null)
            {
                // Butona tıklandığında OpenDetailPanel fonksiyonuna o anki 'element' verisini yolla
                cardButton.onClick.AddListener(() => OpenDetailPanel(element));
            }
        }
    }

    // Herhangi bir element kartına tıklanınca bu fonksiyon çalışır
    public void OpenDetailPanel(ElementData clickedElement)
    {
        // Seçilen elementin verilerini Pop-up paneline aktar
        detailNameText.text = clickedElement.elementName + " (" + clickedElement.symbol + ")";
        // Satır formülü değil sentez tarifini gösteriyor; formül zaten başlıkta.
        // Tarifi olmayan temel elementlerde (H, O, C, N, Na, Cl, Fe, Ca) sabit metin yazılır.
        string recipeText = string.IsNullOrWhiteSpace(clickedElement.recipe) ? "Temel element" : clickedElement.recipe;
        detailRecipeText.text = "Sentez: " + recipeText;
        detailScoreText.text = $"Puan: {clickedElement.synthesisScore} | Silme: {clickedElement.jokerCost}";
        detailDescText.text = clickedElement.description;

        // Paneli görünür yap
        detailPanel.SetActive(true);
    }

    // Paneli Kapatmak için kullanılacak fonksiyon
    public void CloseDetailPanel()
    {
        detailPanel.SetActive(false);
    }
}