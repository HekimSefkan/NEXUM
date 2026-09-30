using System;

/// <summary>
/// ESKİ SERVİS — yerini <see cref="SaveService"/> aldı (Faz 0 / Adım 3).
///
/// Kayıt katmanı tek noktaya toplandığı için toplam skor artık
/// <c>SaveService.Data.totalScore</c> alanında tutuluyor. Bu sınıf yalnızca
/// geriye dönük çağrılar için duruyor ve hepsini SaveService'e iletiyor.
///
/// Yeni kod bunu KULLANMAMALI. Çağrısı kalmadığı doğrulandıktan sonra dosya
/// silinebilir (silme kullanıcı işi; bkz. tur raporu).
/// </summary>
[Obsolete("SaveService kullanın: SaveService.Data.totalScore / SaveService.AddLevelScore(int)")]
public static class TotalScoreService
{
    public const string Key = "TotalScore";

    public static int Total
    {
        get { return SaveService.Data.totalScore; }
    }

    /// <summary>Bölüm kazanıldığında çağrılır; o bölümün skorunu toplama ekler.</summary>
    public static void AddLevelScore(GameManager gameManager)
    {
        if (gameManager == null) return;
        Add(gameManager.currentScore);
    }

    public static void Add(int amount)
    {
        SaveService.AddLevelScore(amount);
    }
}
