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

    // ---------------------------------------------------------------- 1.5
    // Etiketler Faz 5'in "KAYNAMA NOKTASI" soru modu için: aynı banka konuya
    // ve zorluğa göre süzülebilsin. Bu adımda o mod KURULMADI, yalnızca veri
    // hazır.
    [Header("Etiketler (konu · zorluk · ilgili bileşik)")]
    [Tooltip("Konu başlığı, ör. \"Asit-baz\", \"Bağlar\", \"Atmosfer kimyası\"")]
    public string topic = "";

    [Range(1, 3)]
    [Tooltip("1 = kolay, 2 = orta, 3 = zor")]
    public int difficulty = 1;

    [Tooltip("Oyundaki ilgili taş/bileşik, ör. \"CaCO3\", \"NH3\", \"Fe2O3\"")]
    public string relatedCompound = "";

    /// <summary>
    /// Açıklama metninin sözlük anahtarı. Metin asset'te DEĞİL sözlükte durur
    /// (tr + en kuralı); anahtar asset adından türetilir, böylece ikisi
    /// ayrışamaz. Karşılığı olmayan anahtarı <c>QuizExplanationTests</c>
    /// yakalar.
    /// </summary>
    public string ExplanationKey
    {
        get { return "quiz." + name.ToLowerInvariant() + ".explain"; }
    }
}
