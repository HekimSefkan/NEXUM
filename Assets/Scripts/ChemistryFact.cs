using UnityEngine;

// Bu satır, Unity'de sağ tıkladığımızda bu dosyayı üretebilmemizi sağlayacak
[CreateAssetMenu(fileName = "NewChemistryFact", menuName = "NEXUM/Chemistry Fact")]
public class ChemistryFact : ScriptableObject
{
    [Header("Eğitici Bilgi")]
    [TextArea(3, 10)] // Inspector'da yazı kutusunu genişletir (3 ile 10 satır arası)
    public string factText; 
}