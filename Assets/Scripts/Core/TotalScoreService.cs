using UnityEngine;

/// <summary>
/// Toplam skorun biriktiği tek yer. Bölüm kazanıldığında o bölümde toplanan
/// skor toplama eklenir.
///
/// Şu an kalıcı depolama <see cref="PlayerPrefs"/>'tir (projedeki mevcut kayıt yolu).
/// İleride bir SaveService gelirse yalnızca bu sınıfın içi değişir; çağıran taraf aynı kalır.
///
/// Gösterim kararı Faz 2'de verileceği için değer hiçbir ekranda kullanılmıyor.
/// </summary>
public static class TotalScoreService
{
    public const string Key = "TotalScore";

    public static int Total
    {
        get { return PlayerPrefs.GetInt(Key, 0); }
    }

    /// <summary>Bölüm kazanıldığında çağrılır; o bölümün skorunu toplama ekler.</summary>
    public static void AddLevelScore(GameManager gameManager)
    {
        if (gameManager == null) return;
        Add(gameManager.currentScore);
    }

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(Key, Total + amount);
        PlayerPrefs.Save();
    }
}
