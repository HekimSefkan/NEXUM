using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahnelerdeki ve prefab'lardaki TEMA DIŞI renkleri paletin rollerine bağlar.
///
/// Eşleme ELLE KÜRE EDİLDİ, en yakın renk hesabıyla değil: `#515961` en yakın
/// token olarak `panelBackground`a düşüyor ama o bir METİN rengi, zemin değil.
/// Palette ona uyan bir rol olmadığı için `textMuted` eklendi.
/// Her satırın gerekçesi tabloda yazılı.
///
/// Yalnızca RENK değişir; konum, boyut, punto ve sprite'a dokunulmaz.
/// IDEMPOTENT: renk zaten hedefteyse dosyaya dokunmaz.
///
/// Menüden: NEXUM → Renkleri Temaya Bağla
/// Batch:   -executeMethod NexumColorTokens.Run
/// </summary>
public static class NexumColorTokens
{
    private const string Tag = "NEXUM_COLORTOKEN";

    private struct Kural
    {
        public string hex;        // tema dışı değer
        public string role;       // hedef palet rolü
        public string gerekce;
    }

    private static readonly Kural[] Tablo =
    {
        new Kural { hex = "F8F9FA", role = "textPrimary",
                    gerekce = "beyaza çok yakın gövde metni (10 bileşen)" },
        // DİKKAT — bu kural AÇIK zeminler içindir. #515961 taşıyan 6 bileşen
        // (HowToPlayPanel kategori butonları) sprite değişiminden sonra KOYU
        // bir yüzeye geçti; ölçüm:
        //   eski yüzey #A2E6F6 (açık): textMuted 5.15:1 ✓ · textSecondary 1.50:1 ✗
        //   yeni yüzey #213B4A (koyu): textMuted 1.65:1 ✗ · textSecondary 5.66:1 ✓
        // Bu yüzden o altı bileşen textSecondary'de BIRAKILDI. Kural, açık
        // zeminde #515961 kullanan yeni bir metin çıkarsa devreye girer.
        new Kural { hex = "515961", role = "textMuted",
                    gerekce = "açık zeminde sönük metin; palette kendi rolü var" },
        new Kural { hex = "007DD8", role = "primary",
                    gerekce = "camgöbeği-mavi vurgu metni" },
        new Kural { hex = "C4FFB9", role = "success",
                    gerekce = "açık yeşil vurgu" },
        new Kural { hex = "C3FFB8", role = "success",
                    gerekce = "yukarıdakinin 1 birim farklı yazımı" },
        new Kural { hex = "E4E1E1", role = "textPrimary",
                    gerekce = "beyaza yakın uyarı metni" },
        new Kural { hex = "44E453", role = "success",
                    gerekce = "yeşil başlık" },
        new Kural { hex = "D9D7D7", role = "textSecondary",
                    gerekce = "açık gri açıklama" },
        new Kural { hex = "5B5B5B", role = "textSecondary",
                    gerekce = "gri kimlik metni" },
        new Kural { hex = "48CAE4", role = "secondary",
                    gerekce = "camgöbeği vurgu" },
        new Kural { hex = "2ECC70", role = "success",
                    gerekce = "success'in 1 birim yanlış yazımı" },
        new Kural { hex = "00B2D6", role = "primary",
                    gerekce = "primary'nin yanlış yazımı" },
        new Kural { hex = "8FD9FF", role = "primaryBright",
                    gerekce = "primaryBright'ın yanlış yazımı" },
        new Kural { hex = "808080", role = "textSecondary",
                    gerekce = "nötr gri" },
        new Kural { hex = "F1C40F", role = "warning",
                    gerekce = "altın vurgu" },
        // --- 2d-3a düzeltmesi: BELGEDE VARDI, TABLODA YOKTU.
        // docs/DESIGN_TOKENS.md "Renk sapmaları" tablosu bu eşlemeleri zaten
        // yazmıştı ama uygulama tablosuna hiç geçmemişti; 26 taş prefab'ı
        // geçiş öncesinin renkleriyle kaldı ve hiçbir test fark etmedi.
        // Artık DesignTokenConsistencyTests belge ile tabloyu karşılaştırıyor.
        // (Taş harfleri ayrıca NexumTileVisuals tarafından beyaza alındı.)
        new Kural { hex = "00FF00", role = "success",
                    gerekce = "saf yeşil -> success (belge: Renk sapmaları)" },
        new Kural { hex = "C0C0C0", role = "textPrimary",
                    gerekce = "gümüş -> textPrimary (belge: beyaza yakın aile)" },
        new Kural { hex = "DADADA", role = "textPrimary",
                    gerekce = "beyaza yakın gri (belge)" },
        new Kural { hex = "D0D0D3", role = "textPrimary",
                    gerekce = "beyaza yakın gri (belge)" },
        new Kural { hex = "60E8F5", role = "secondary",
                    gerekce = "camgöbeği ailesi -> secondary (belge)" },
        new Kural { hex = "3F3F3F", role = "textOnLight",
                    gerekce = "koyu gri metin; belge panelBackground diyordu ama "
                              + "bu bir METİN rengi (bkz. #515961 tartışması)" },
        new Kural { hex = "0D0C0C", role = "textOnLight",
                    gerekce = "siyaha yakın metin; aynı gerekçe" },
        new Kural { hex = "2E3240", role = "textOnLight",
                    gerekce = "zemin rolü metinde kullanılmış; açık zeminde "
                              + "textOnLight, koyu zeminde kontrast taraması düzeltir" },
        new Kural { hex = "085D73", role = "primary", gerekce = "prefab içi camgöbeği (belge)" },
        new Kural { hex = "00B4FF", role = "primary", gerekce = "prefab içi camgöbeği (belge)" },
        new Kural { hex = "4EE145", role = "success", gerekce = "GERİ AL yeşili (belge)" },
        new Kural { hex = "4EE045", role = "success", gerekce = "GERİ AL yeşili (belge)" },
        new Kural { hex = "6DE847", role = "success", gerekce = "GERİ AL yeşili (belge)" },
        new Kural { hex = "5CFF56", role = "success", gerekce = "GERİ AL yeşili (belge)" },
        new Kural { hex = "87CF5D", role = "success", gerekce = "GERİ AL yeşili (belge)" },
        new Kural { hex = "269501", role = "success", gerekce = "GERİ AL yeşili (belge)" },

        new Kural { hex = "33CC33", role = "success", gerekce = "kombo yeşili" },
        new Kural { hex = "FF9900", role = "warning", gerekce = "kombo turuncusu" },
        new Kural { hex = "FF3333", role = "danger",  gerekce = "kombo kırmızısı" },
    };

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    [MenuItem("NEXUM/Renkleri Temaya Bağla")]
    public static void Run()
    {
        Dictionary<Color32, Kural> harita = new Dictionary<Color32, Kural>(new RenkKarsilastirici());
        foreach (Kural k in Tablo)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString("#" + k.hex, out c))
            {
                Debug.LogError($"{Tag}: hex çözülemedi -> {k.hex}");
                continue;
            }
            harita[(Color32)c] = k;
        }

        Dictionary<string, int> dokum = new Dictionary<string, int>();
        int degisen = 0, dokunulmayan = 0;

        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            int d = 0;

            foreach (GameObject kok in scene.GetRootGameObjects())
            {
                foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (Uygula(tmp, harita, dokum)) d++; else dokunulmayan++;
                }
            }

            if (d > 0) EditorSceneManager.SaveScene(scene);
            degisen += d;
            Debug.Log($"{Tag}: {Path.GetFileName(yol)} -> bağlanan {d}");
        }

        int pd = 0;
        foreach (string yol in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories))
        {
            string normal = yol.Replace("\\", "/");
            GameObject kok = PrefabUtility.LoadPrefabContents(normal);
            bool degisti = false;

            foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Uygula(tmp, harita, dokum)) { degisti = true; pd++; } else dokunulmayan++;
            }

            if (degisti) PrefabUtility.SaveAsPrefabAsset(kok, normal);
            PrefabUtility.UnloadPrefabContents(kok);
        }

        degisen += pd;
        Debug.Log($"{Tag}: prefab'lar -> bağlanan {pd}");

        foreach (var kv in dokum.OrderByDescending(k => k.Value))
        {
            Debug.Log($"{Tag}_ESLEME: {kv.Key} x{kv.Value}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: TOPLAM bağlanan {degisen}, dokunulmayan {dokunulmayan}");
        Debug.Log($"{Tag}: OK");
    }

    private static bool Uygula(TMP_Text tmp, Dictionary<Color32, Kural> harita,
                               Dictionary<string, int> dokum)
    {
        if (tmp == null) return false;

        Color32 mevcut = tmp.color;
        Kural k;
        if (!harita.TryGetValue(mevcut, out k)) return false;

        Color hedef = NexumPalette.ByRole(k.role);
        hedef.a = tmp.color.a;                      // saydamlık korunur
        if (((Color32)hedef).Equals(mevcut)) return false;

        tmp.color = hedef;
        EditorUtility.SetDirty(tmp);

        string anahtar = "#" + k.hex + " -> " + k.role;
        dokum[anahtar] = dokum.TryGetValue(anahtar, out int n) ? n + 1 : 1;
        return true;
    }

    /// <summary>Alfa'yı yok sayan renk karşılaştırması.</summary>
    private class RenkKarsilastirici : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        public int GetHashCode(Color32 c)
        {
            return (c.r << 16) | (c.g << 8) | c.b;
        }
    }
}
