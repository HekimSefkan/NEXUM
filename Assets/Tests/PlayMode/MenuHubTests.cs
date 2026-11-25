using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

/// <summary>
/// Ana menü hub'ı: DAVRANIŞ, GÖRÜNÜRLÜK ve DURUM SATIRLARI (1.7).
///
/// Neyi koruyor:
///  1. <b>Beş mevcut giriş aynı hedefi çağırıyor.</b> Hub yeniden dizildi;
///     bir butonun OnClick bağı taşınırken düşerse menü SESSİZCE çalışmaz
///     hâle gelir — tıklanır, hiçbir şey olmaz. Bağlar adıyla ölçülüyor.
///  2. <b>Uygulanmamış giriş GİZLİ.</b> Kilitli kutu/"yakında" yok.
///  3. <b>Gizli giriş boşluk bırakmıyor.</b> Satır yeniden akıyor.
///  4. <b>Durum satırları gerçek kayıttan.</b> Sahte değer yok: kayıt
///     değişince satır da değişmeli.
///  5. <b>Ses tek kaynak.</b> Hub'daki hızlı sessize alma, Ayarlar'ın
///     okuduğu alanları yazar; ikinci bir "sessiz" kaydı yok.
/// </summary>
public class MenuHubTests
{
    private const string Tag = "NEXUM_HUB";

    /// <summary>Taşınan beş giriş: obje adı -> çağırması gereken metotlar.</summary>
    private static readonly Dictionary<string, string[]> BeklenenBaglar =
        new Dictionary<string, string[]>
    {
        // Kahraman kart artık haritayı AÇMIYOR, sıradaki bölümü başlatıyor
        // (kullanıcı onayı, PR #30 eki). Harita Bölüm Seçimi kartında.
        { "PlayButton",         new[] { "StartNextLevel" } },
        { "HowToPlayButton",    new[] { "OpenHowToPlay" } },
        { "EncyclopediaButton", new[] { "OpenEncyclopedia" } },
        { "RecipeBookButton",   new[] { "OpenRecipeBook" } },
        { "SettingButton",      new[] { "OpenSettingsPanel" } },
    };

    private string yedek;
    private MonoBehaviour hub;
    private GameObject panel;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yedek = SaveService.ExportRaw();

        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(0.5f);

        hub = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "MenuHub");
        Assert.IsNotNull(hub, "MenuHub yok (MenuHubBuilder koşuldu mu?)");

        panel = hub.transform.parent.gameObject;
    }

    [TearDown]
    public void GeriYukle()
    {
        Reflect.Call("HubFeatures", "Varsayilana");
        SaveService.ImportRaw(yedek);
    }

    // ----------------------------------------------------------- davranış

    [UnityTest]
    public IEnumerator BesGirisAyniHedefiCagiriyor()
    {
        List<string> sorunlar = new List<string>();
        List<string> dokum = new List<string>();

        foreach (KeyValuePair<string, string[]> beklenen in BeklenenBaglar)
        {
            GameObject go = Bul(beklenen.Key);
            if (go == null) { sorunlar.Add($"{beklenen.Key}: sahnede yok"); continue; }

            Button button = go.GetComponent<Button>();
            if (button == null) { sorunlar.Add($"{beklenen.Key}: Button yok"); continue; }

            List<string> cagrilar = new List<string>();
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                Object hedef = button.onClick.GetPersistentTarget(i);
                string metot = button.onClick.GetPersistentMethodName(i);
                cagrilar.Add(metot);

                if (hedef == null) sorunlar.Add($"{beklenen.Key}: {metot} hedefi null");
            }

            dokum.Add($"{beklenen.Key,-20} -> {string.Join(", ", cagrilar)}");

            foreach (string gereken in beklenen.Value)
            {
                if (!cagrilar.Contains(gereken))
                {
                    sorunlar.Add($"{beklenen.Key}: {gereken} çağrısı KAYBOLDU " +
                                 $"(kalanlar: {string.Join(", ", cagrilar)})");
                }
            }

            if (!go.activeInHierarchy) sorunlar.Add($"{beklenen.Key}: hub'da görünmüyor");
        }

        foreach (string s in dokum) Debug.Log($"{Tag}_BAG: {s}");
        Assert.IsEmpty(sorunlar, "davranış korunmadı: " + string.Join(" | ", sorunlar));
        yield return null;
    }

    // --------------------------------------------------------- görünürlük

    [UnityTest]
    public IEnumerator UygulanmamisGirisGizli()
    {
        Reflect.Call("HubFeatures", "Varsayilana");
        Reflect.Call(hub, "Apply");
        yield return null;

        var gizliOlmali = new Dictionary<string, string>
        {
            { "streakBadge", "gün serisi (Faz 3)" },
            { "dailyPuzzleCard", "günlük bulmaca (Faz 3)" },
            { "boilingPointCard", "kaynama noktası (Faz 5)" },
            { "endlessLabCard", "sonsuz lab (Faz 5)" },
            { "weeklyExperimentCard", "haftanın deneyi (Faz 5)" },
            { "mentorButton", "mentor (Faz 5)" },
            { "modeRow", "mod satırı (hepsi gizli)" },
        };

        List<string> sorunlar = new List<string>();
        foreach (KeyValuePair<string, string> p in gizliOlmali)
        {
            GameObject go = Reflect.Get<GameObject>(hub, p.Key);
            if (go == null) { sorunlar.Add($"{p.Key}: bağlanmamış"); continue; }
            if (go.activeSelf) sorunlar.Add($"{p.Value} görünüyor");
        }

        // Görünür kalması gerekenler. rankXpGroup 2.1'de (HubFeatures.RankAndXp),
        // heroStars 2.2'de (HubFeatures.Stars) UYGULANDI: artık gizli değil.
        foreach (string alan in new[] { "levelSelectCard", "recipeBookCard",
                                        "rankXpGroup", "heroStars" })
        {
            GameObject go = Reflect.Get<GameObject>(hub, alan);
            Assert.IsNotNull(go, alan + " bağlanmamış");
            if (!go.activeSelf) sorunlar.Add($"{alan} gizli ama uygulanmış bir giriş");
        }

        Debug.Log($"{Tag}: bugünkü bayraklarla gizli {gizliOlmali.Count} giriş, sorun {sorunlar.Count}");
        Assert.IsEmpty(sorunlar, string.Join(" | ", sorunlar));
    }

    [UnityTest]
    public IEnumerator GizliGirisBoslukBirakmiyor()
    {
        RectTransform root = hub.transform as RectTransform;

        Reflect.Call("HubFeatures", "Varsayilana");
        Reflect.Call(hub, "Apply");
        yield return null;
        yield return null;

        float bugun = KullanilanYukseklik(root);

        Reflect.Call("HubFeatures", "TumunuAc");
        Reflect.Call(hub, "Apply");
        yield return null;
        yield return null;

        float hepsi = KullanilanYukseklik(root);

        Debug.Log($"{Tag}: kullanılan yükseklik bugün {bugun:F0}, hepsi açık {hepsi:F0}");

        Assert.Greater(hepsi, bugun + 100f,
            "bayraklar açılınca düzen büyümedi: gizli girişler yer kaplıyor olabilir");

        // İki hâl de 1920'ye sığmalı (kaydırma yok)
        Assert.LessOrEqual(hepsi, 1920f, "hepsi açıkken hub ekrana sığmıyor");
    }

    /// <summary>Etkin satırların en üstü ile en altı arasındaki mesafe.</summary>
    private static float KullanilanYukseklik(RectTransform root)
    {
        float ust = float.MinValue, alt = float.MaxValue;
        foreach (RectTransform c in root.GetComponentsInChildren<RectTransform>(false))
        {
            if (c == root) continue;
            if (c.parent != root) continue;

            Vector3[] k = new Vector3[4];
            c.GetWorldCorners(k);
            for (int i = 0; i < 4; i++)
            {
                Vector3 yerel = root.InverseTransformPoint(k[i]);
                ust = Mathf.Max(ust, yerel.y);
                alt = Mathf.Min(alt, yerel.y);
            }
        }
        return ust > alt ? ust - alt : 0f;
    }

    // ------------------------------------------------------ durum satırları

    [UnityTest]
    public IEnumerator DurumSatirlariGercekKayittan()
    {
        SaveService.Data.unlockedLevel = 3;
        SaveService.Data.maxLevelUnlocked = 4;
        SaveService.Data.discoveredRecipes.Clear();
        SaveService.Data.discoveredRecipes.AddRange(new[] { "a", "b", "c" });
        SaveService.Data.playerName = "Marie";

        Reflect.Call(hub, "Apply");
        yield return null;

        string hero = Reflect.Get<TextMeshProUGUI>(hub, "heroStatusText").text;
        string level = Reflect.Get<TextMeshProUGUI>(hub, "levelSelectStatusText").text;
        string book = Reflect.Get<TextMeshProUGUI>(hub, "recipeBookStatusText").text;
        string isim = Reflect.Get<TextMeshProUGUI>(hub, "playerNameText").text;

        Debug.Log($"{Tag}: kayıt A -> \"{hero}\" | \"{level}\" | \"{book}\" | \"{isim}\"");

        Assert.AreEqual("Marie", isim);
        StringAssert.Contains("4", hero, "kahraman satırı bölüm numarasını göstermiyor");
        StringAssert.Contains("4", level, "bölüm seçimi açık sayısını göstermiyor");
        StringAssert.Contains("3", book, "tarif defteri keşif sayısını göstermiyor");

        // Kayıt değişince satır da değişmeli (sahte/sabit değer olmadığının kanıtı)
        SaveService.Data.unlockedLevel = 0;
        SaveService.Data.maxLevelUnlocked = 1;
        SaveService.Data.discoveredRecipes.Clear();
        SaveService.Data.playerName = "Otto";

        Reflect.Call(hub, "Apply");
        yield return null;

        string hero2 = Reflect.Get<TextMeshProUGUI>(hub, "heroStatusText").text;
        string level2 = Reflect.Get<TextMeshProUGUI>(hub, "levelSelectStatusText").text;
        string book2 = Reflect.Get<TextMeshProUGUI>(hub, "recipeBookStatusText").text;
        string isim2 = Reflect.Get<TextMeshProUGUI>(hub, "playerNameText").text;

        Debug.Log($"{Tag}: kayıt B -> \"{hero2}\" | \"{level2}\" | \"{book2}\" | \"{isim2}\"");

        Assert.AreNotEqual(hero, hero2, "kahraman satırı kayıttan bağımsız (sabit değer)");
        Assert.AreNotEqual(level, level2, "bölüm seçimi satırı kayıttan bağımsız");
        Assert.AreNotEqual(book, book2, "tarif defteri satırı kayıttan bağımsız");
        Assert.AreEqual("Otto", isim2);
    }

    // ------------------------------------------- kahraman kart hangi bölümü açar

    /// <summary>
    /// Kartın YAZDIĞI bölüm ile AÇTIĞI bölüm aynı olmalı. İkisi ayrı
    /// hesaplansaydı sessizce ayrışırlardı: oyuncu "Bölüm 4" okur, Bölüm 3
    /// açılır ve bunu kimse fark etmez.
    /// </summary>
    [UnityTest]
    public IEnumerator KahramanKartYazdigiBolumuAciyor()
    {
        int bolumSayisi = Reflect.Call<int>("MenuHub", "LevelCount");
        Assert.Greater(bolumSayisi, 1, "bölüm sayısı okunamadı");

        List<string> dokum = new List<string>();

        // Hiç oynamamış oyuncudan son bölüme kadar
        foreach (int unlocked in new[] { 0, 1, 3, bolumSayisi - 1, bolumSayisi + 5 })
        {
            SaveService.Data.unlockedLevel = unlocked;
            SaveService.Data.selectedLevel = -1;

            Reflect.Call(hub, "Apply");
            yield return null;

            string yazan = Reflect.Get<TextMeshProUGUI>(hub, "heroStatusText").text;
            int indeks = Reflect.Get<int>(hub, "NextLevelIndex");

            // Kart neyi açıyor? LoadLevel sahne yüklüyor, bu yüzden
            // selectedLevel'a yazdığı değer ölçülür.
            SaveService.Data.selectedLevel = indeks;

            int beklenen = Mathf.Clamp(unlocked, 0, bolumSayisi - 1);
            dokum.Add($"unlockedLevel={unlocked} -> \"{yazan}\" indeks={indeks}");

            Assert.AreEqual(beklenen, indeks,
                $"unlockedLevel={unlocked} için açılacak bölüm yanlış");
            StringAssert.Contains((indeks + 1).ToString(), yazan,
                "kartın yazdığı bölüm ile açacağı bölüm ayrışmış");
        }

        foreach (string s in dokum) Debug.Log($"{Tag}_KAHRAMAN: {s}");

        // Hiç oynamamış oyuncu: Bölüm 1
        SaveService.Data.unlockedLevel = 0;
        Reflect.Call(hub, "Apply");
        yield return null;
        Assert.AreEqual(0, Reflect.Get<int>(hub, "NextLevelIndex"),
            "hiç oynamamış oyuncu için sıradaki bölüm 1 değil");
    }

    // ------------------------------------------------------------------ ses

    [UnityTest]
    public IEnumerator SesIkonuAyarlarlaTekKaynak()
    {
        SaveService.Data.musicOn = 1;
        SaveService.Data.sfxOn = 1;
        SaveService.SaveNow();

        Reflect.Call(hub, "ToggleMute");
        yield return null;

        Debug.Log($"{Tag}: sessize alma sonrası musicOn={SaveService.Data.musicOn} " +
                  $"sfxOn={SaveService.Data.sfxOn}");

        Assert.AreEqual(0, SaveService.Data.musicOn, "müzik susmadı");
        Assert.AreEqual(0, SaveService.Data.sfxOn, "efektler susmadı");

        // Ayarlar paneli AYNI alanları okumalı: ikinci bir kayıt yok
        var settings = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "SettingsManager");
        Assert.IsNotNull(settings, "SettingsManager yok");

        Image muzikImg = Reflect.Get<Image>(settings, "musicToggleImg");
        Sprite acikSprite = Reflect.Get<Sprite>(settings, "toggleOnSprite");
        Sprite kapaliSprite = Reflect.Get<Sprite>(settings, "toggleOffSprite");
        Assert.IsNotNull(muzikImg, "musicToggleImg bağlanmamış");

        // Panel açılınca anahtar kayıttan tazelenir
        settings.gameObject.SetActive(false);
        settings.gameObject.SetActive(true);
        yield return null;

        Debug.Log($"{Tag}: ayarlar anahtarı = " +
                  (muzikImg.sprite == kapaliSprite ? "KAPALI" : "açık"));
        Assert.AreSame(kapaliSprite, muzikImg.sprite,
            "Ayarlar anahtarı hub'daki sessize almayı görmüyor: iki ayrı kayıt var");

        // Geri aç
        Reflect.Call(hub, "ToggleMute");
        yield return null;
        Assert.AreEqual(1, SaveService.Data.musicOn, "tekrar açılmadı");
    }

    // -------------------------------------------------------------- yardımcı

    private GameObject Bul(string ad)
    {
        foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == ad) return t.gameObject;
        }
        return null;
    }
}
