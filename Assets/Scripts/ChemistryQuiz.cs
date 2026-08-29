using UnityEngine;

[CreateAssetMenu(fileName = "NewQuiz", menuName = "NEXUM/Quiz Question")]
public class ChemistryQuiz : ScriptableObject
{
    [Header("Soru Ayarları")]
    [TextArea(2, 5)]
    public string questionText; // Sorunun kendisi
    
    [Header("Şıklar (A, B, C, D)")]
    public string[] options = new string[4]; // 4 adet şıkkımız olacak
    
    [Header("Doğru Cevap İndeksi (0=A, 1=B, 2=C, 3=D)")]
    [Range(0, 3)]
    public int correctAnswerIndex; // Hangi şıkkın doğru olduğunu tutacağız
}