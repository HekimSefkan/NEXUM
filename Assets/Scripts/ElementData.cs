using UnityEngine;

// Bu satır, Unity menüsüne "NEXUM -> Yeni Element" butonu eklememizi sağlar!
[CreateAssetMenu(fileName = "New Element", menuName = "NEXUM/Element Verisi")]
public class ElementData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string elementName;      // Örn: Su
    public string symbol;           // Örn: H₂O
    public Sprite elementIcon;      // Elementin görseli (İleride ekleriz)

    [Header("Oyun Ekonomisi")]
    public int synthesisScore;      // Üretince kazanılan puan (Örn: 50)
    public int jokerCost;           // Parçalayıcı ile silme bedeli (Örn: 100)

    [Header("Ansiklopedi Metni")]
    [TextArea(3, 5)] 
    [Header("Sentez Formülü")]
    public string recipe;      // Örn: "H₂ + O" veya "Na + Cl"               // Unity'de rahat yazmak için geniş bir kutu açar
    public string description;      // Örn: "Matrisin hayat pınarı. Fazlası boğar."
}