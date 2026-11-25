using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// XP KURALLARINI OYUN İÇERİĞİNE BAĞLAYAN KATMAN (Faz 2 / 2.1).
///
/// <c>ProgressionService</c> ve <c>Badges</c> kendi derlemesinde
/// (<c>Nexum.Progression</c>) ve hiçbir oyun tipine bağlı DEĞİL: bölüm
/// sayısı, tarif sayısı ve quiz banka boyutu onlara parametre olarak
/// verilir. O sayıları bilen taraf burası (<c>RecipeCatalog</c> iki
/// sahneden de okunabiliyor).
///
/// Sahneye EKLENMEZ: <c>AppBootstrap</c> ve <c>StatsIntegrity</c> ile aynı
/// desen, <c>RuntimeInitializeOnLoadMethod</c>.
///
/// <b>SIRA ÖNEMLİ.</b> Tarif defterinin geriye dönük doldurması XP
/// hesabından ÖNCE koşmalı: XP, keşfedilen tarif SAYISINDAN besleniyor ve
/// o sayı doldurma yapılmadan 0'dır. Defterin doldurması 1.x'te yalnızca
/// defter paneli açılınca çalışıyordu (<c>RecipeBookPanel.Start</c>), yani
/// defteri hiç açmamış bir oyuncunun XP'si eksik hesaplanırdı. Doldurma
/// kendi bayrağıyla korunduğu için (<c>recipeBackfillDone</c>) burada
/// çağırmak onu TEKRARLAMAZ, yalnızca ÖNE ALIR.
/// </summary>
public static class ProgressionBootstrap
{
    private const string Tag = "NEXUM_XP_BOOT";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnLaunch()
    {
        Run();
    }

    /// <summary>
    /// Göç + geriye dönük hesap + rozet değerlendirmesi. Test de bunu
    /// çağırır (sahne gerekmez).
    /// </summary>
    public static void Run()
    {
        // 1) Defter doldurması (kendi bayrağıyla korunur)
        RecipeBook.RunBackfillOnce();

        // 2) XP'nin geriye dönük hesabı (kendi bayrağıyla korunur)
        ProgressionService.RunBackfillOnce(LevelCount(), RecipeCount(), QuizCount());

        // 3) Bugün kazanılabilir rozetler: eski kayıtta "İlk Sentez" ve
        //    "Tüm Tepkimeler" çoktan hak edilmiş olabilir. "Kazasız Bölüm"
        //    geriye dönük verilemez — hangi bölümün kazasız bitirildiği
        //    kaydedilmedi, uydurulmaz.
        List<string> yeni = Badges.Evaluate(RecipeCount(), false);
        if (yeni.Count > 0) Debug.Log($"{Tag}: acilista {yeni.Count} rozet -> " +
                                      string.Join(", ", yeni));
    }

    public static int LevelCount()
    {
        RecipeCatalog c = RecipeCatalog.Load();
        return c != null && c.levels != null ? c.levels.Count : 0;
    }

    public static int RecipeCount()
    {
        RecipeCatalog c = RecipeCatalog.Load();
        return c != null && c.entries != null ? c.entries.Count : 0;
    }

    /// <summary>
    /// Quiz banka boyutu. Game sahnesindeyken <c>UIManager.allQuizzes</c>
    /// TEK KAYNAK; sahne yoksa katalogdaki kopya okunur.
    /// </summary>
    public static int QuizCount()
    {
        UIManager ui = UIManager.Instance;
        if (ui != null && ui.allQuizzes != null)
        {
            int n = 0;
            foreach (ChemistryQuiz q in ui.allQuizzes)
            {
                if (q != null) n++;
            }
            if (n > 0) return n;
        }

        RecipeCatalog c = RecipeCatalog.Load();
        return c != null ? c.quizCount : 0;
    }
}
