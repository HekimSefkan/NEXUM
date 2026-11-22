using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quiz bankasının tek kurucusu (1.5).
///
/// Yaptıkları:
///  1. 1.5'te eklenen 20 soruyu <c>Assets/Data/Quizzes</c> altında kurar.
///  2. 36 sorunun TAMAMINA etiket yazar (konu · zorluk · ilgili bileşik);
///     etiketler Faz 5'in "KAYNAMA NOKTASI" modunun süzme verisidir.
///  3. Mevcut sorulardaki iki yazım hatasını düzeltir ve şıkların baştaki /
///     sondaki boşluklarını kırpar.
///  4. Game sahnesindeki <c>UIManager.allQuizzes</c> listesini bankanın
///     tamamıyla doldurur (doğal sıra: Soru_1 … Soru_36).
///
/// IDEMPOTENT: ikinci çalıştırmada hiçbir dosya değişmez (git diff boş).
/// Açıklama METNİ burada DEĞİL: sözlükte (<c>QuizStrings</c>), anahtar
/// <c>ChemistryQuiz.ExplanationKey</c> ile asset adından türetilir.
///
/// Menüden: NEXUM -> Quiz Bankasını Kur
/// Batch mode: -executeMethod NexumQuizBank.Run
/// </summary>
public static class NexumQuizBank
{
    private const string Tag = "NEXUM_QUIZBANK";
    private const string QuizDir = "Assets/Data/Quizzes";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    private class Spec
    {
        public string name;
        public string question;
        public string[] options;
        public int correct;
        public string topic;
        public int difficulty;
        public string compound;
    }

    private struct Etiket
    {
        public string topic;
        public int zorluk;
        public string bilesik;

        public Etiket(string topic, int zorluk, string bilesik)
        {
            this.topic = topic;
            this.zorluk = zorluk;
            this.bilesik = bilesik;
        }
    }

    /// <summary>Mevcut 16 sorunun etiketleri (gövdelerine dokunulmaz).</summary>
    private static readonly Dictionary<string, Etiket> Etiketler = new Dictionary<string, Etiket>
    {
        ["Soru_1"] = new Etiket("Asit-baz", 1, "HCl"),
        ["Soru_2"] = new Etiket("Periyodik sistem", 2, "F"),
        ["Soru_3"] = new Etiket("Bağlar", 1, "H2"),
        ["Soru_4"] = new Etiket("Elementler", 2, "Hg"),
        ["Soru_5"] = new Etiket("Asit-baz", 2, "NaOH"),
        ["Soru_6"] = new Etiket("Organik kimya", 1, "CH4"),
        ["Soru_7"] = new Etiket("Allotropi", 2, "C"),
        ["Soru_8"] = new Etiket("Günlük kimya", 1, "CH3COOH"),
        ["Soru_9"] = new Etiket("Tepkime hızı", 2, "Katalizör"),
        ["Soru_10"] = new Etiket("Molekül geometrisi", 3, "H2O"),
        ["Soru_11"] = new Etiket("Tepkime türleri", 2, "Fe2O3"),
        ["Soru_12"] = new Etiket("Atomun yapısı", 1, "H"),
        ["Soru_13"] = new Etiket("Saf madde ve karışım", 2, "H2O"),
        ["Soru_14"] = new Etiket("Günlük kimya", 2, "HNO3"),
        ["Soru_15"] = new Etiket("Periyodik sistem", 1, "Cl"),
        ["Soru_16"] = new Etiket("Yanma", 1, "H2"),
    };

    private struct Duzeltme
    {
        public string asset;
        public int sik;
        public string eski;
        public string yeni;

        public Duzeltme(string asset, int sik, string eski, string yeni)
        {
            this.asset = asset;
            this.sik = sik;
            this.eski = eski;
            this.yeni = yeni;
        }
    }

    /// <summary>
    /// Mevcut sorulardaki yazım hataları. Eski metin birebir eşleşmezse
    /// HİÇBİR ŞEY yazılmaz, hata raporlanır (elle düzeltilmiş olabilir).
    /// </summary>
    private static readonly Duzeltme[] YazimDuzeltmeleri =
    {
        new Duzeltme("Soru_8", 0, "Siktik Asit", "Sitrik Asit"),
        new Duzeltme("Soru_12", 2, "Proten ve Nötron", "Proton ve Nötron"),
    };

    /// <summary>1.5'te eklenen 20 soru. Oyundaki taş ve tariflere bağlı.</summary>
    private static readonly Spec[] YeniSorular =
    {
        new Spec { name = "Soru_17", correct = 1, topic = "Atmosfer kimyası", difficulty = 1, compound = "O3",
            question = "Ozon tabakası (O<sub>3</sub>) Dünya'yı Güneş'ten gelen hangi ışınlardan korur?",
            options = new[] { "Kızılötesi ışınlar", "Ultraviyole ışınlar", "Radyo dalgaları", "Mikrodalgalar" } },

        new Spec { name = "Soru_18", correct = 2, topic = "Bileşikler", difficulty = 1, compound = "CaCO3",
            question = "Kireç taşı ve mermerin yapısındaki ana bileşik hangisidir?",
            options = new[] { "NaCl", "CaO", "CaCO<sub>3</sub>", "Fe<sub>2</sub>O<sub>3</sub>" } },

        new Spec { name = "Soru_19", correct = 0, topic = "Günlük kimya", difficulty = 2, compound = "CaO",
            question = "Halk arasında \"sönmemiş kireç\" denilen madde hangisidir?",
            options = new[] { "CaO", "CaCO<sub>3</sub>", "NaCl", "H<sub>2</sub>CO<sub>3</sub>" } },

        new Spec { name = "Soru_20", correct = 3, topic = "Tepkime türleri", difficulty = 2, compound = "CaCO3",
            question = "Kireç taşı (CaCO<sub>3</sub>) fırında yüksek sıcaklıkta ısıtıldığında açığa çıkan gaz hangisidir?",
            options = new[] { "O<sub>2</sub>", "H<sub>2</sub>", "CO", "CO<sub>2</sub>" } },

        new Spec { name = "Soru_21", correct = 2, topic = "Asit-baz", difficulty = 2, compound = "H2CO3",
            question = "Karbondioksitin suda çözünmesiyle oluşan zayıf asit hangisidir?",
            options = new[] { "Nitrik asit", "Sülfürik asit", "Karbonik asit", "Asetik asit" } },

        new Spec { name = "Soru_22", correct = 2, topic = "Yükseltgenme basamağı", difficulty = 3, compound = "Fe2O3",
            question = "Fe<sub>2</sub>O<sub>3</sub> bileşiğinde demirin yükseltgenme basamağı kaçtır?",
            options = new[] { "+1", "+2", "+3", "+4" } },

        new Spec { name = "Soru_23", correct = 1, topic = "İyonik bileşikler", difficulty = 3, compound = "NaCl",
            question = "Katı sofra tuzu elektriği iletmez, ama suda çözündüğünde iletir. Bunun nedeni nedir?",
            options = new[] { "Suyun kendisinin iletken olması",
                              "İyonların çözeltide serbest hareket etmesi",
                              "Tuzun suda moleküllere ayrılması",
                              "Çözünme sırasında klor gazı çıkması" } },

        new Spec { name = "Soru_24", correct = 1, topic = "Asit-baz", difficulty = 1, compound = "NH3",
            question = "Amonyağın (NH<sub>3</sub>) sulu çözeltisi için aşağıdakilerden hangisi doğrudur?",
            options = new[] { "Asidiktir", "Baziktir", "Nötrdür", "pH değeri sıfırdır" } },

        new Spec { name = "Soru_25", correct = 0, topic = "Sanayi kimyası", difficulty = 2, compound = "NH3",
            question = "Azot ve hidrojen gazlarından amonyak üretilen sanayi yöntemi hangisidir?",
            options = new[] { "Haber-Bosch yöntemi", "Solvay yöntemi", "Kontakt yöntemi", "Elektroliz yöntemi" } },

        new Spec { name = "Soru_26", correct = 2, topic = "Atmosfer kimyası", difficulty = 1, compound = "N2",
            question = "Kuru havanın hacimce yaklaşık yüzde 78'ini oluşturan gaz hangisidir?",
            options = new[] { "Oksijen", "Karbondioksit", "Azot", "Argon" } },

        new Spec { name = "Soru_27", correct = 2, topic = "Bağlar", difficulty = 2, compound = "N2",
            question = "Azot molekülünü (N<sub>2</sub>) oda koşullarında çok kararlı yapan bağ türü nedir?",
            options = new[] { "Tek kovalent bağ", "İkili kovalent bağ", "Üçlü kovalent bağ", "İyonik bağ" } },

        new Spec { name = "Soru_28", correct = 3, topic = "Çevre kimyası", difficulty = 3, compound = "NO",
            question = "Taşıt motorlarında yüksek sıcaklıkta havanın azotu ile oksijeninden oluşan kirletici gaz hangisidir?",
            options = new[] { "CO<sub>2</sub>", "SO<sub>2</sub>", "CH<sub>4</sub>", "NO" } },

        new Spec { name = "Soru_29", correct = 0, topic = "Sağlık ve kimya", difficulty = 2, compound = "CO",
            question = "Karbonmonoksit (CO) solunduğunda neden ölümcül olabilir?",
            options = new[] { "Hemoglobine oksijenden daha güçlü bağlanır",
                              "Kanı asitleştirir",
                              "Akciğer dokusunu yakar",
                              "Suda çözünüp baz oluşturur" } },

        new Spec { name = "Soru_30", correct = 1, topic = "Yanma", difficulty = 2, compound = "CO",
            question = "Karbon içeren bir yakıt yeterli oksijen bulamadan yandığında hangi gaz oluşur?",
            options = new[] { "CO<sub>2</sub>", "CO", "O<sub>3</sub>", "H<sub>2</sub>" } },

        new Spec { name = "Soru_31", correct = 2, topic = "Moleküller arası etkileşim", difficulty = 2, compound = "H2O",
            question = "Suyun, benzer büyüklükteki moleküllere göre kaynama noktasının çok yüksek olmasının nedeni nedir?",
            options = new[] { "İyonik bağ", "Metalik bağ", "Hidrojen bağı", "London kuvvetleri" } },

        new Spec { name = "Soru_32", correct = 0, topic = "Elektrokimya", difficulty = 3, compound = "H2O",
            question = "Suyun elektrolizinde açığa çıkan hidrojen ve oksijen gazlarının hacim oranı kaçtır?",
            options = new[] { "2'ye 1", "1'e 1", "1'e 2", "3'e 1" } },

        new Spec { name = "Soru_33", correct = 1, topic = "Günlük kimya", difficulty = 1, compound = "Cl2",
            question = "İçme suyuna ve havuz suyuna klor (Cl<sub>2</sub>) eklenmesinin amacı nedir?",
            options = new[] { "Suyu yumuşatmak", "Mikroorganizmaları yok etmek", "Suyun pH değerini yükseltmek", "Suya tat vermek" } },

        new Spec { name = "Soru_34", correct = 0, topic = "Metaller", difficulty = 2, compound = "Na",
            question = "Sodyum metali neden gazyağı içinde saklanır?",
            options = new[] { "Hava ve nemle hızla tepkime verdiği için",
                              "Çok kolay eridiği için",
                              "Işık altında rengini kaybettiği için",
                              "Kokusunu yitirdiği için" } },

        new Spec { name = "Soru_35", correct = 0, topic = "Allotropi", difficulty = 2, compound = "O3",
            question = "O<sub>2</sub> ile O<sub>3</sub> arasındaki ilişkiyi en iyi hangisi anlatır?",
            options = new[] { "Aynı elementin allotroplarıdır",
                              "Birbirinin izotopudur",
                              "Birbirinin izomeridir",
                              "Aynı maddenin farklı fiziksel hâlleridir" } },

        new Spec { name = "Soru_36", correct = 3, topic = "Biyokimya", difficulty = 1, compound = "Ca",
            question = "Kemik ve dişlerin sertliğini sağlayan, vücutta en bol bulunan metal hangisidir?",
            options = new[] { "Demir", "Sodyum", "Klor", "Kalsiyum" } },
    };

    [MenuItem("NEXUM/Quiz Bankasını Kur")]
    public static void Run()
    {
        Directory.CreateDirectory(QuizDir);

        List<string> olusturulan = new List<string>();
        List<string> guncellenen = new List<string>();
        List<string> hatalar = new List<string>();
        int dokunulmayan = 0;

        // --- 1) yeni sorular
        foreach (Spec spec in YeniSorular)
        {
            string yol = QuizDir + "/" + spec.name + ".asset";
            ChemistryQuiz q = AssetDatabase.LoadAssetAtPath<ChemistryQuiz>(yol);
            bool yeni = q == null;

            if (yeni)
            {
                q = ScriptableObject.CreateInstance<ChemistryQuiz>();
                AssetDatabase.CreateAsset(q, yol);
            }

            bool degisti = false;
            degisti |= Yaz(ref q.questionText, spec.question);
            if (q.options == null || q.options.Length != 4) { q.options = new string[4]; degisti = true; }
            for (int i = 0; i < 4; i++) degisti |= Yaz(ref q.options[i], spec.options[i]);
            degisti |= Yaz(ref q.correctAnswerIndex, spec.correct);
            degisti |= Yaz(ref q.topic, spec.topic);
            degisti |= Yaz(ref q.difficulty, spec.difficulty);
            degisti |= Yaz(ref q.relatedCompound, spec.compound);

            if (yeni) olusturulan.Add(spec.name);
            else if (degisti) guncellenen.Add(spec.name);
            else dokunulmayan++;

            if (yeni || degisti) EditorUtility.SetDirty(q);
        }

        // --- 2) mevcut 16 sorunun etiketleri + şık boşlukları
        foreach (KeyValuePair<string, Etiket> pair in Etiketler)
        {
            string yol = QuizDir + "/" + pair.Key + ".asset";
            ChemistryQuiz q = AssetDatabase.LoadAssetAtPath<ChemistryQuiz>(yol);
            if (q == null) { hatalar.Add("soru bulunamadı: " + yol); continue; }

            bool degisti = false;
            degisti |= Yaz(ref q.topic, pair.Value.topic);
            degisti |= Yaz(ref q.difficulty, pair.Value.zorluk);
            degisti |= Yaz(ref q.relatedCompound, pair.Value.bilesik);

            // Şıkların baştaki/sondaki boşlukları (ör. "Laktik Asit ")
            if (q.options != null)
            {
                for (int i = 0; i < q.options.Length; i++)
                {
                    if (string.IsNullOrEmpty(q.options[i])) continue;
                    degisti |= Yaz(ref q.options[i], q.options[i].Trim());
                }
            }

            if (degisti) { guncellenen.Add(pair.Key); EditorUtility.SetDirty(q); }
            else dokunulmayan++;
        }

        // --- 3) yazım hataları
        foreach (Duzeltme d in YazimDuzeltmeleri)
        {
            string yol = QuizDir + "/" + d.asset + ".asset";
            ChemistryQuiz q = AssetDatabase.LoadAssetAtPath<ChemistryQuiz>(yol);
            if (q == null || q.options == null || d.sik >= q.options.Length)
            {
                hatalar.Add("yazım düzeltmesi uygulanamadı (soru/şık yok): " + d.asset);
                continue;
            }

            string simdiki = q.options[d.sik] != null ? q.options[d.sik].Trim() : "";
            if (simdiki == d.yeni) { dokunulmayan++; continue; }      // zaten düzeltilmiş
            if (simdiki != d.eski)
            {
                hatalar.Add($"yazım düzeltmesi eşleşmedi: {d.asset} şık {d.sik} = \"{simdiki}\" " +
                            $"(beklenen \"{d.eski}\"); DOKUNULMADI");
                continue;
            }

            q.options[d.sik] = d.yeni;
            EditorUtility.SetDirty(q);
            if (!guncellenen.Contains(d.asset)) guncellenen.Add(d.asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- 4) sahnedeki liste
        bool sahneDegisti = ListeyiDoldur(hatalar);

        Debug.Log($"{Tag}: oluşturulan={olusturulan.Count} güncellenen={guncellenen.Count} " +
                  $"dokunulmayan={dokunulmayan} sahne={(sahneDegisti ? "güncellendi" : "dokunulmadı")} " +
                  $"hata={hatalar.Count}");
        foreach (string s in olusturulan) Debug.Log($"{Tag}_OLUSTURULDU: {s}");
        foreach (string s in guncellenen) Debug.Log($"{Tag}_GUNCELLENDI: {s}");
        foreach (string s in hatalar) Debug.LogError($"{Tag}_HATA: {s}");

        Debug.Log(hatalar.Count == 0 ? $"{Tag}: OK" : $"{Tag}: FAIL");
    }

    /// <summary>Game sahnesindeki UIManager.allQuizzes = bankanın tamamı.</summary>
    private static bool ListeyiDoldur(List<string> hatalar)
    {
        List<ChemistryQuiz> banka = AssetDatabase.FindAssets("t:ChemistryQuiz", new[] { QuizDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ChemistryQuiz>)
            .Where(q => q != null)
            .OrderBy(q => SiraNo(q.name))
            .ThenBy(q => q.name)
            .ToList();

        if (banka.Count == 0) { hatalar.Add("quiz asset'i bulunamadı"); return false; }

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        if (ui == null) { hatalar.Add("Game sahnesinde UIManager yok"); return false; }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty liste = so.FindProperty("allQuizzes");
        if (liste == null) { hatalar.Add("UIManager.allQuizzes alanı yok"); return false; }

        bool ayni = liste.arraySize == banka.Count;
        if (ayni)
        {
            for (int i = 0; i < banka.Count; i++)
            {
                if (liste.GetArrayElementAtIndex(i).objectReferenceValue != banka[i]) { ayni = false; break; }
            }
        }
        if (ayni) return false;

        liste.arraySize = banka.Count;
        for (int i = 0; i < banka.Count; i++)
        {
            liste.GetArrayElementAtIndex(i).objectReferenceValue = banka[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    /// <summary>"Soru_7" -> 7 (alfabetik sıra 10'u 2'nin önüne atıyor).</summary>
    private static int SiraNo(string ad)
    {
        int alt = ad.LastIndexOf('_');
        if (alt < 0 || alt + 1 >= ad.Length) return int.MaxValue;
        return int.TryParse(ad.Substring(alt + 1), out int n) ? n : int.MaxValue;
    }

    private static bool Yaz(ref string hedef, string deger)
    {
        if (hedef == deger) return false;
        hedef = deger;
        return true;
    }

    private static bool Yaz(ref int hedef, int deger)
    {
        if (hedef == deger) return false;
        hedef = deger;
        return true;
    }
}
