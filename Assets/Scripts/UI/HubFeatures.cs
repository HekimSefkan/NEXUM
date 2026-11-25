/// <summary>
/// Ana menü hub'ındaki her girişin "UYGULANDI MI" bayrağı — TEK KAYNAK (1.7).
///
/// <b>Kural: uygulanmamış giriş tamamen GİZLİ.</b> Kilitli kutu, "yakında"
/// rozeti, soluk buton YOK. Oyuncuya var olmayan bir şeyin yerini
/// göstermek, oyunu olduğundan eksik gösterir; üstelik "ne zaman açılacak"
/// sorusunun cevabı yok.
///
/// <b>Gizli giriş düzende boşluk bırakmaz.</b> Satırlar yeniden akar
/// (<see cref="MenuHub"/>); bir satırın bütün girişleri gizliyse satır
/// hiç yer kaplamaz. Faz 2/3/5'te bayrak açılınca giriş kendi yerine
/// oturur, yerleşim yeniden kurulmaz.
///
/// Bayraklar <b>kod sabiti</b>, kayıt verisi değil: "özellik var mı"
/// sorusunun cevabı oyuncuya göre değişmez. Testler
/// <see cref="TumunuAc"/> / <see cref="Varsayilana"/> ile iki ucu da ölçer.
/// </summary>
public static class HubFeatures
{
    // --- Faz 1'de UYGULANAN girişler --------------------------------------
    // (bunlar bayrakla gizlenmiyor; listede yoklar çünkü her zaman açıklar:
    //  kahraman kart, bölüm seçimi, ansiklopedi, tarif defteri, ayarlar,
    //  nasıl oynanır, ses, profil)

    // --- Faz 2 ------------------------------------------------------------

    /// <summary>Bölüm yıldızları (kahraman kartta ve sonuç kartında).</summary>
    public static bool Stars { get; set; }

    /// <summary>Rütbe adı ve XP çubuğu (profil şeridi).</summary>
    public static bool RankAndXp { get; set; }

    // --- Faz 3 ------------------------------------------------------------

    /// <summary>Günlük bulmaca kartı.</summary>
    public static bool DailyPuzzle { get; set; }

    /// <summary>Gün serisi rozeti (profil şeridi).</summary>
    public static bool Streak { get; set; }

    // --- Faz 5 ------------------------------------------------------------

    /// <summary>"KAYNAMA NOKTASI" soru modu.</summary>
    public static bool BoilingPoint { get; set; }

    /// <summary>Sonsuz lab modu.</summary>
    public static bool EndlessLab { get; set; }

    /// <summary>Haftanın deneyi.</summary>
    public static bool WeeklyExperiment { get; set; }

    /// <summary>Mentor ekranı (ikon şeridinde).</summary>
    public static bool Mentor { get; set; }

    // ---------------------------------------------------------------- kurulum

    static HubFeatures()
    {
        Varsayilana();
    }

    /// <summary>
    /// Bugün yayına giren hâl. Faz 2/3/5 bir özelliği bitirdiğinde ilgili
    /// satır burada <c>true</c> olur — başka hiçbir yerde değişiklik
    /// gerekmez.
    /// </summary>
    public static void Varsayilana()
    {
        Stars = false;              // Faz 2
        RankAndXp = true;           // 2.1 UYGULANDI
        DailyPuzzle = false;        // Faz 3
        Streak = false;             // Faz 3
        BoilingPoint = false;       // Faz 5
        EndlessLab = false;         // Faz 5
        WeeklyExperiment = false;   // Faz 5
        Mentor = false;             // Faz 5
    }

    /// <summary>
    /// TEST İÇİN: her girişi açar. "Hepsi açıkken de sığıyor mu, çakışma
    /// var mı" ölçümü bununla yapılır; bugünden sonra bayrak açan tur,
    /// yerleşimi yeniden ölçmek zorunda kalmaz.
    /// </summary>
    public static void TumunuAc()
    {
        Stars = true;
        RankAndXp = true;
        DailyPuzzle = true;
        Streak = true;
        BoilingPoint = true;
        EndlessLab = true;
        WeeklyExperiment = true;
        Mentor = true;
    }
}
