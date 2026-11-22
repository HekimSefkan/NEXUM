using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// ÖLÜ İSTATİSTİKLERİN ONARIMI (Faz 1 / 1.4).
///
/// Profilde gösterilen her sayı için tek soru: İLGİLİ OLAY OLUNCA DEĞİŞİYOR
/// MU? Sayının ekranda durması onu canlı yapmıyor.
///
/// Ölçülenler:
///   · TOPLAM SENTEZ     ← birleşme
///   · LABORATUVAR KAZASI ← oyun sonu
///   · QUİZ BAŞARISI      ← quiz cevabı
///   · AÇILAN BÖLÜM       ← bölüm kazanma, bölüm sayısını AŞMADAN
///
/// Ayrıca <b>geri al istismarı</b>: birleştir → geri al döngüsü ömür boyu
/// sayaçları şişiremez (CLAUDE.md "Ömür boyu istatistikler" kuralı).
/// </summary>
public class StatsTests
{
    private const string Tag = "NEXUM_STATS_TEST";

    private MonoBehaviour grid;
    private MonoBehaviour ui;
    private List<Transform> cells;
    private string yedek;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(grid, "GridManager yok");
        Assert.IsNotNull(ui, "UIManager yok");

        cells = (List<Transform>)Field(grid, "cells").GetValue(grid);
        Assert.AreEqual(16, cells.Count, "grid 16 hücre olmalı");
        Field(grid, "currentGameMode").SetValue(grid, 0);
    }

    [TearDown]
    public void GeriYukle()
    {
        if (!string.IsNullOrEmpty(yedek)) SaveService.ImportRaw(yedek);
    }

    // ------------------------------------------------------------ canlı mı

    [UnityTest]
    public IEnumerator SentezBirlesmeyleArtar()
    {
        int once = SaveService.Data.totalSynthesis;

        TahtaKur("H", "H");            // H + H -> H2
        yield return Kaydir(Vector2.right);

        int sonra = SaveService.Data.totalSynthesis;
        Debug.Log($"{Tag}_SENTEZ: {once} -> {sonra}");
        Assert.AreEqual(once + 1, sonra, "birleşme TOPLAM SENTEZ'i artırmalı");
    }

    [UnityTest]
    public IEnumerator KazaOyunSonuylaArtar()
    {
        int once = SaveService.Data.totalAccidents;

        Reflect.Call(ui, "ShowGameOver");
        yield return null;

        int sonra = SaveService.Data.totalAccidents;
        Debug.Log($"{Tag}_KAZA: {once} -> {sonra}");
        Assert.AreEqual(once + 1, sonra, "oyun sonu LABORATUVAR KAZASI'nı artırmalı");
    }

    [UnityTest]
    public IEnumerator KazaAyniOyundaIkiKezSayilmaz()
    {
        Reflect.Call(ui, "ShowGameOver");
        yield return null;
        int birKez = SaveService.Data.totalAccidents;

        Reflect.Call(ui, "ShowGameOver");
        yield return null;

        Debug.Log($"{Tag}_KAZA_TEK: {birKez} -> {SaveService.Data.totalAccidents}");
        Assert.AreEqual(birKez, SaveService.Data.totalAccidents,
            "aynı oyunda ikinci oyun sonu ekranı kazayı tekrar saymamalı");
    }

    [UnityTest]
    public IEnumerator QuizSayaclariCevaplaArtar()
    {
        int deneme = SaveService.Data.quizAttempts;
        int dogru = SaveService.Data.quizCorrect;

        // Quiz paneli açılmadan doğru şık kurulmaz; akışın kendisi kullanılır
        Reflect.Call(ui, "ShowQuizPanel");
        yield return null;

        // Doğru şıkkı kodun kendisinden al, tahmin etme
        int dogruIndex = (int)Field(ui, "currentCorrectIndex").GetValue(ui);
        Reflect.Call(ui, "CheckAnswer", dogruIndex);
        yield return null;
        yield return new WaitForSeconds(0.2f);

        Debug.Log($"{Tag}_QUIZ: deneme {deneme} -> {SaveService.Data.quizAttempts}, " +
                  $"doğru {dogru} -> {SaveService.Data.quizCorrect}");

        Assert.AreEqual(deneme + 1, SaveService.Data.quizAttempts, "deneme sayacı artmalı");
        Assert.AreEqual(dogru + 1, SaveService.Data.quizCorrect, "doğru sayacı artmalı");
    }

    [UnityTest]
    public IEnumerator AcilanBolumKazanincaArtar()
    {
        MonoBehaviour lm = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "LevelManager");
        Assert.IsNotNull(lm, "LevelManager yok");

        SaveService.Data.unlockedLevel = 0;
        SaveService.Data.maxLevelUnlocked = 1;
        lm.GetType().GetField("currentLevelIndex").SetValue(lm, 0);

        Reflect.Call(ui, "ShowWinScreen");
        yield return null;

        Debug.Log($"{Tag}_BOLUM: ilk bölüm kazanıldı -> açılan {SaveService.Data.maxLevelUnlocked}, " +
                  $"en yüksek indeks {SaveService.Data.unlockedLevel}");

        Assert.AreEqual(2, SaveService.Data.maxLevelUnlocked, "ilk bölüm bitince 2 bölüm açık olmalı");
        Assert.AreEqual(1, SaveService.Data.unlockedLevel);
    }

    /// <summary>
    /// SON bölüm kazanılınca sayaç bölüm sayısını AŞAMAZ. Eski kod
    /// indeks+2 yazıyordu: 7 bölümlük oyunda profil "AÇILAN BÖLÜM 8" diyordu.
    /// </summary>
    [UnityTest]
    public IEnumerator AcilanBolumBolumSayisiniAsmaz()
    {
        MonoBehaviour lm = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "LevelManager");
        Assert.IsNotNull(lm, "LevelManager yok");

        System.Collections.IList levels =
            (System.Collections.IList)lm.GetType().GetField("levels").GetValue(lm);
        int son = levels.Count - 1;

        SaveService.Data.unlockedLevel = son;
        SaveService.Data.maxLevelUnlocked = son;
        lm.GetType().GetField("currentLevelIndex").SetValue(lm, son);

        Reflect.Call(ui, "ShowWinScreen");
        yield return null;

        Debug.Log($"{Tag}_SONBOLUM: {levels.Count} bölümlük oyunda son bölüm kazanıldı -> " +
                  $"açılan {SaveService.Data.maxLevelUnlocked}, " +
                  $"en yüksek indeks {SaveService.Data.unlockedLevel}");

        Assert.AreEqual(levels.Count, SaveService.Data.maxLevelUnlocked,
            "açılan bölüm sayısı, oyundaki bölüm sayısını aşamaz");
        Assert.AreEqual(levels.Count - 1, SaveService.Data.unlockedLevel,
            "en yüksek bölüm indeksi son bölümün indeksidir");
    }

    /// <summary>
    /// Eski kayıttaki şişmiş değer açılışta onarılır (sıfırlanmaz, kırpılır).
    /// </summary>
    [UnityTest]
    public IEnumerator EskiKayittakiSismisDegerOnarilir()
    {
        // Test derlemesi Assembly-CSharp'ı referans alamaz: Reflect köprüsü
        int bolumSayisi = Reflect.Call<int>("StatsIntegrity", "LevelCount");
        Assert.Greater(bolumSayisi, 0, "bölüm sayısı okunamadı (RecipeCatalog)");

        SaveService.Data.maxLevelUnlocked = bolumSayisi + 1;   // eski kodun yazdığı değer
        SaveService.Data.unlockedLevel = bolumSayisi;

        int onarilan = Reflect.Call<int>("StatsIntegrity", "Repair");
        yield return null;

        Debug.Log($"{Tag}_ONARIM: {onarilan} alan onarıldı -> açılan " +
                  $"{SaveService.Data.maxLevelUnlocked}/{bolumSayisi}, " +
                  $"indeks {SaveService.Data.unlockedLevel}");

        Assert.AreEqual(2, onarilan, "iki alan da onarılmalıydı");
        Assert.AreEqual(bolumSayisi, SaveService.Data.maxLevelUnlocked);
        Assert.AreEqual(bolumSayisi - 1, SaveService.Data.unlockedLevel);

        // İkinci koşu hiçbir şeyi değiştirmez (idempotent onarım)
        Assert.AreEqual(0, Reflect.Call<int>("StatsIntegrity", "Repair"),
            "tutarlı kayıtta onarım yapılmamalı");
    }

    // ---------------------------------------------------- geri al istismarı

    /// <summary>
    /// 10 kez birleştir → geri al. Ömür boyu sayaç BAŞLADIĞI YERDE kalmalı.
    /// Kural: ömür boyu istatistikler yalnızca kesinleşmiş eylemleri sayar.
    /// </summary>
    [UnityTest]
    public IEnumerator GeriAlDongusuSayaclariSismez()
    {
        int baslangic = SaveService.Data.totalSynthesis;

        for (int i = 0; i < 10; i++)
        {
            TahtaKur("H", "H");
            yield return Kaydir(Vector2.right);

            // Geri alma bedeli ve hak sınırı ÖLÇÜMÜ engellemesin: ölçülen şey
            // SAYAÇ, ekonomi değil. Bedel karşılanmazsa ExecuteUndo hiçbir şey
            // yapmadan döner ve test yanlışlıkla "sayaç şişti" derdi
            // (ilk koşuda tam bu oldu: 10 döngüde 8 birleşme geri alınmadı).
            Field(grid, "usedUndos").SetValue(grid, 0);
            GeriAlmaBedeliniKarsila();
            Reflect.Call(grid, "ExecuteUndo");
            yield return null;

            Assert.AreEqual(0, YiginBoyu(), $"{i + 1}. geri alma yapılmadı");
        }

        int son = SaveService.Data.totalSynthesis;
        Debug.Log($"{Tag}_ISTISMAR: 10 birleştir-geri al sonrası {baslangic} -> {son}");
        Assert.AreEqual(baslangic, son,
            "geri alınan birleşmeler ömür boyu sayacı artırmış kalamaz");
    }

    /// <summary>
    /// Tarif keşfi KURALIN İSTİSNASI: hamle geri alınsa da keşfedilmiş kalır.
    /// Keşfedilen bilgi geri alınamaz — oyuncu o tepkimeyi gerçekten gördü.
    /// </summary>
    [UnityTest]
    public IEnumerator KesifGeriAlmaylaSilinmez()
    {
        SaveService.Data.discoveredRecipes.Clear();

        TahtaKur("H", "H");
        yield return Kaydir(Vector2.right);

        int kesif = SaveService.Data.discoveredRecipes.Count;
        Assert.Greater(kesif, 0, "birleşme keşif kaydetmeliydi");

        Field(grid, "usedUndos").SetValue(grid, 0);
        Reflect.Call(grid, "ExecuteUndo");
        yield return null;

        Debug.Log($"{Tag}_KESIF: geri almadan önce {kesif}, sonra " +
                  $"{SaveService.Data.discoveredRecipes.Count} (istisna: silinmez)");

        Assert.AreEqual(kesif, SaveService.Data.discoveredRecipes.Count,
            "keşif geri almayla silinmez (bilinçli istisna)");
    }

    // ---------------------------------------------------------- yardımcılar

    /// <summary>Geri alma bedeli ödenebilsin diye oturum skorunu yükseltir.</summary>
    private void GeriAlmaBedeliniKarsila()
    {
        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "GameManager");
        if (gm == null) return;

        int bedel = (int)Field(grid, "undoCost").GetValue(grid);
        Field(gm, "currentScore").SetValue(gm, bedel * 10);
    }

    /// <summary>Geri alma yığınındaki anlık görüntü sayısı.</summary>
    private int YiginBoyu()
    {
        object yigin = Field(grid, "historyStack").GetValue(grid);
        return (int)yigin.GetType().GetProperty("Count").GetValue(yigin);
    }

    private void TahtaKur(string sol, string sag)
    {
        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform child = cell.GetChild(i);
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }
        Field(grid, "movesSinceSpawn").SetValue(grid, 0);
        Field(grid, "catalystCharge").SetValue(grid, 0);

        Koy(12, sol);
        Koy(13, sag);
    }

    private void Koy(int index, string ad)
    {
        GameObject[] basics = (GameObject[])Field(grid, "basicElements").GetValue(grid);
        GameObject prefab = basics.FirstOrDefault(b => b.name == "Tile_" + ad);
        Assert.IsNotNull(prefab, "temel element yok: " + ad);

        GameObject tile = Object.Instantiate(prefab, cells[index]);
        tile.name = prefab.name;
        tile.transform.localScale = Vector3.one;
        tile.transform.localPosition = Vector3.zero;
    }

    private IEnumerator Kaydir(Vector2 yon)
    {
        MethodInfo m = grid.GetType().GetMethod("Shift",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        m.Invoke(grid, new object[] { yon });
        yield return null;
        yield return new WaitForSeconds(0.35f);
    }

    private static FieldInfo Field(object target, string name)
    {
        FieldInfo f = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, "alan yok: " + name);
        return f;
    }
}
