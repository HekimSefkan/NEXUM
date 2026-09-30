using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// "Nasıl Oynanır" rehber metinlerinin ve Bölüm 1 deney föyü hatırlatmasının
// tek kaynağı.
//
// UZUNLUK KURALI: metin kutusunun %85'ini geçemez (pop-up kutusu 549x822).
// Sınır ölçümle korunuyor: Assets/Tests/PlayMode/GuideFitTests.cs.
//
// IDEMPOTENT: aynı metin zaten yazılıysa hiçbir şeyi değiştirmez.
//
// Menüden: NEXUM -> Rehber Metinlerini Yaz
// Batch mode: -executeMethod NexumGuideTexts.Run
public static class NexumGuideTexts
{
    private const string Tag = "NEXUM_GUIDE_TEXTS";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    private struct Kategori
    {
        public string title;
        public string description;
        public Kategori(string t, string d) { title = t; description = d; }
    }

    private static readonly Kategori[] Kategoriler =
    {
        new Kategori("ATOM SİSTEMİ VE SENTEZ",
            "Elementleri parmağınla (veya yön tuşlarıyla) kaydır. Taşlar duvara ya da " +
            "önlerindeki engele kadar kayar. Uyumlu atomlar çarpışınca kimyasal bağ kurup " +
            "yeni bileşik olur (Örn: H + H = H<sub>2</sub>). Bir hamlede üretilen bileşik, " +
            "o hamlede ikinci kez birleşmez. Hedefin sol üstte istenen molekülleri üretmek."),

        new Kategori("MATRİSİN DOLMASI VE YENİ ELEMENTLER",
            "Her sentezden sonra matrise yeni bir element düşer. Sentez yapamadığın " +
            "hamlelerde de boş durmaz: birleşmesiz her 2 hamlede bir yeni element gelir. " +
            "Yeni element, kaydırdığın yönün TERSİNDEKİ kenarda kendi beliriş animasyonuyla " +
            "doğar. Matris dolar ve yapacak hamle kalmazsa laboratuvar kilitlenir."),

        new Kategori("MENTOR DESTEĞİ VE İPUÇLARI",
            "Deney tıkandığında yalnız değilsin. Seçtiğin efsanevi Mentor, biriken sentez " +
            "puanların karşılığında sana hangi iki atomu birleştirmen gerektiğini holografik " +
            "olarak gösterir. Ancak unutma: Asistan yardımı sınırlıdır ve eğer \"Sınav " +
            "Modu\"ndaysan tüm bağlantılar kesilir, kendi bilgine güvenmek zorundasın."),

        new Kategori("TERSİNİR TEPKİME, JOKER VE KATALİZÖR",
            "Puanlarını kullanarak son hamleni geri alabilirsin. Matris tıkandıysa Joker " +
            "(Hipotez) modunu açıp puan karşılığında istediğin elementi silebilirsin. " +
            "Grid'in altındaki KATALİZÖR ŞARJI her sentezde dolar; beşe ulaşınca bir kez " +
            "Joker'i BEDAVA kullanma hakkı kazanırsın."),

        new Kategori("TEORİK BİLGİ VE KURTARMA",
            "Eğer matris tamamen dolar ve hareket edecek yer kalmazsa laboratuvar " +
            "kilitlenir. Ancak her şey bitmiş değil! Karşına çıkacak teorik kimya sorusunu " +
            "doğru cevaplarsan, sistem acil durum tahliyesi yapar ve matristen 3 elementi " +
            "silerek sana ikinci bir şans (Revive) verir."),

        new Kategori("OYUN MODLARI VE KARİYER",
            "Ayarlar panelinden laboratuvar koşullarını değiştirebilirsin. 'Normal Mod' " +
            "dengeli bir deneyim sunar. 'Sınav Modu' asistanı kapatır ama 2 kat puan verir. " +
            "'Serbest Mod' hedefsiz bir keşif alanıdır: kaybetme yok, ipuçları bedava, " +
            "istediğin bileşiği denemekte özgürsün."),
    };

    // Bölüm 1 deney föyüne eklenecek tek cümlelik hatırlatma
    private const string FoyHatirlatma =
        "Taşlar duvara kadar kayar ve birleşme olmayan her 2 hamlede matrise yeni bir element düşer.";

    [MenuItem("NEXUM/Rehber Metinlerini Yaz")]
    public static void Run()
    {
        int rehber = WriteGuide();
        int foy = WriteTutorialReminder();

        Debug.Log($"{Tag}: rehber +{rehber}, deney foyu +{foy}");
        Debug.Log($"{Tag}: OK");
    }

    private static int WriteGuide()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        HowToPlayManager manager = Object.FindObjectOfType<HowToPlayManager>(true);
        if (manager == null)
        {
            Debug.LogError($"{Tag}: HowToPlayManager yok");
            return 0;
        }

        if (manager.guideCategories == null || manager.guideCategories.Length != Kategoriler.Length)
        {
            Debug.LogError($"{Tag}: kategori sayısı uyuşmuyor " +
                           $"(sahnede {manager.guideCategories?.Length ?? 0}, tabloda {Kategoriler.Length})");
            return 0;
        }

        int changed = 0;
        for (int i = 0; i < Kategoriler.Length; i++)
        {
            GuideCategory current = manager.guideCategories[i];
            if (current.title == Kategoriler[i].title && current.description == Kategoriler[i].description) continue;

            current.title = Kategoriler[i].title;
            current.description = Kategoriler[i].description;
            manager.guideCategories[i] = current;
            changed++;
        }

        if (changed > 0)
        {
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return changed;
    }

    private static int WriteTutorialReminder()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        LevelManager levels = Object.FindObjectOfType<LevelManager>(true);
        if (levels == null || levels.levels == null || levels.levels.Count == 0)
        {
            Debug.LogError($"{Tag}: LevelManager ya da bölüm listesi yok");
            return 0;
        }

        LevelData first = levels.levels[0];
        if (first.tutorialContent != null && first.tutorialContent.Contains(FoyHatirlatma)) return 0;

        first.tutorialContent = (first.tutorialContent ?? string.Empty).TrimEnd() + "\n\n" + FoyHatirlatma;
        levels.levels[0] = first;

        EditorUtility.SetDirty(levels);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return 1;
    }
}
