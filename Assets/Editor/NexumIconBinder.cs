using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simge ailesini projedeki TÜM karşılıklarına bağlar (3c/2).
///
/// Geri tuşu, duraklat (II), kapatma (X) ve ipucu birbirinden kopuktu;
/// bazıları Unity'nin yerleşik görseliydi. Artık hepsi tek üreteçten
/// (<c>Tools/generate_icons_family.py</c>) geliyor.
///
/// İKİ İŞ:
///   1) Simge yuvaları: eski raster ya da yerleşik görsel -> aile simgesi.
///   2) Yerleşik sprite'ın simge OLMADIĞI yerler: tam ekran karartma
///      katmanları ve ScrollRect parçaları. Bunlara simge konmaz; düz renk
///      dolgusu için sprite GEREKMEZ, <c>sprite = null</c> yapılır. Böylece
///      yerleşik Unity sprite'ı projede kalmaz.
///
/// Kaydırma çubuğu ve tutamacı hariç tutulur: Unity'nin kendi ScrollRect
/// parçaları, görünümleri o bileşenin işi.
///
/// IDEMPOTENT: değer yerindeyse dosyaya dokunmaz.
///
/// Menüden: NEXUM → Simgeleri Bağla
/// Batch:   -executeMethod NexumIconBinder.Run
/// </summary>
public static class NexumIconBinder
{
    private const string Tag = "NEXUM_SIMGE";
    private const string IconDir = "Assets/Art/Icons/UI";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    /// <summary>Obje adı -> aile simgesi. Ad eşleşmesi, yol değil.</summary>
    private static readonly Dictionary<string, string> Slots = new Dictionary<string, string>
    {
        { "BackButton", "icon_back" },
        { "PauseButton", "icon_pause" },
        { "CloseButton", "icon_close" },
    };

    /// <summary>
    /// Kit çerçevesi alacak özel yuvalar (3c/7: quiz şıkları kitin kendi
    /// şık sprite'ına geçer; durum mantığı ve cevap akışı DEĞİŞMEZ).
    /// </summary>
    private static readonly Dictionary<string, string> FrameSlots = new Dictionary<string, string>
    {
        { "OptionButton_A", "Assets/Art/UI/quiz_option_normal.png" },
        { "OptionButton_B", "Assets/Art/UI/quiz_option_normal.png" },
        { "OptionButton_C", "Assets/Art/UI/quiz_option_normal.png" },
        { "OptionButton_D", "Assets/Art/UI/quiz_option_normal.png" },
    };

    /// <summary>Eski raster -> aile simgesi (her kullanıldığı yerde).</summary>
    private static readonly Dictionary<string, string> Replace = new Dictionary<string, string>
    {
        { "geri", "icon_back" },
        { "x", "icon_close" },
        { "lock_icon", "icon_lock" },
    };

    /// <summary>Simge konmayacak, sprite'ı boşaltılacak: karartma katmanları.</summary>
    private static bool DuzDolgu(Image img)
    {
        return img.name.EndsWith("Panel");
    }

    /// <summary>Yerleşik sprite taşıyan METİN butonları: kit çerçevesine geçer.</summary>
    private const string GhostFramePath = "Assets/Art/UI/btn_ghost_normal.png";
    private const string InputFieldPath = "Assets/Art/UI/input_field_normal.png";
    private const string FillPath = "Assets/Art/UI/progress_fill_selected.png";

    [MenuItem("NEXUM/Simgeleri Bağla")]
    public static void Run()
    {
        int baglanan = 0, bosaltilan = 0, dokunulmayan = 0;
        List<string> dokum = new List<string>();

        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            bool degisti = false;

            foreach (Image img in Object.FindObjectsOfType<Image>(true))
            {
                if (img == null) continue;
                if (KaydirmaCubugu(img.transform)) continue;

                string ozelCerceve;
                if (FrameSlots.TryGetValue(img.name, out ozelCerceve))
                {
                    Sprite ozelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ozelCerceve);
                    // TİNT BEYAZ OLMALI. 3c/7'de yalnızca sprite bağlanmıştı;
                    // kutuların üzerinde eski tasarımın parlak yeşil tinti
                    // kalmıştı ve kit sprite'ını yeşile boyuyordu (cihaz
                    // görüntüsünde ölçüldü). Kit kendi rengini taşır.
                    bool tintYanlis = img.color != Color.white;
                    if (ozelSprite != null && (img.sprite != ozelSprite || tintYanlis))
                    {
                        string eskiAd = img.sprite != null ? img.sprite.name : "yok";
                        dokum.Add($"{Yol(img.transform)}: {eskiAd} {Hex(img.color)} -> " +
                                  $"{System.IO.Path.GetFileNameWithoutExtension(ozelCerceve)} (beyaz tint)");
                        img.sprite = ozelSprite;
                        img.type = Image.Type.Sliced;
                        img.fillCenter = true;
                        img.color = Color.white;
                        EditorUtility.SetDirty(img);
                        baglanan++;
                        degisti = true;
                    }
                    else
                    {
                        dokunulmayan++;
                    }

                    // Etiket koyu kit yüzeyinde okunabilir kalmalı
                    foreach (TMPro.TMP_Text tmp in img.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    {
                        if (tmp.color == UITokens.Theme.palette.textPrimary) continue;
                        tmp.color = UITokens.Theme.palette.textPrimary;
                        EditorUtility.SetDirty(tmp);
                        baglanan++;
                        degisti = true;
                    }
                    continue;
                }

                string hedefAd;
                if (Slots.TryGetValue(img.name, out hedefAd)
                    || (img.sprite != null && Replace.TryGetValue(img.sprite.name, out hedefAd)))
                {
                    Sprite simge = Yukle(hedefAd);
                    if (simge == null) continue;
                    if (img.sprite == simge) { dokunulmayan++; continue; }

                    dokum.Add($"{Yol(img.transform)}: {(img.sprite != null ? img.sprite.name : "yok")} -> {hedefAd}");
                    img.sprite = simge;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                    baglanan++;
                    degisti = true;
                    continue;
                }

                if (!Yerlesik(img.sprite)) continue;

                // Karartma katmanı: düz renk dolgusu, sprite GEREKMEZ
                if (DuzDolgu(img))
                {
                    dokum.Add($"{Yol(img.transform)}: yerleşik {img.sprite.name} -> sprite yok (karartma)");
                    img.sprite = null;
                    img.type = Image.Type.Simple;
                    EditorUtility.SetDirty(img);
                    bosaltilan++;
                    degisti = true;
                    continue;
                }

                // Görünmez dokunma alanı: sprite'a gerek yok
                if (img.color.a < 0.01f)
                {
                    dokum.Add($"{Yol(img.transform)}: yerleşik {img.sprite.name} -> sprite yok (görünmez)");
                    img.sprite = null;
                    img.type = Image.Type.Simple;
                    EditorUtility.SetDirty(img);
                    bosaltilan++;
                    degisti = true;
                    continue;
                }

                // Dolum çubuğu (Filled): kit dolgusu, tip korunur
                if (img.type == Image.Type.Filled)
                {
                    Sprite dolgu = AssetDatabase.LoadAssetAtPath<Sprite>(FillPath);
                    if (dolgu == null) continue;
                    dokum.Add($"{Yol(img.transform)}: yerleşik {img.sprite.name} -> progress_fill_selected");
                    img.sprite = dolgu;
                    EditorUtility.SetDirty(img);
                    baglanan++;
                    degisti = true;
                    continue;
                }

                // Girdi alanı ve metin butonları: kit çerçevesi
                bool girdi = img.GetComponent<TMPro.TMP_InputField>() != null
                             || img.transform.parent != null
                             && img.transform.parent.GetComponent<TMPro.TMP_InputField>() != null;
                string cerceve = girdi ? InputFieldPath : GhostFramePath;

                // Buraya gelen her şey GÖRÜNÜR, yerleşik ve ScrollRect dışı:
                // kit çerçevesine geçer. Yerleşik sprite projede kalmaz.

                Sprite kit = AssetDatabase.LoadAssetAtPath<Sprite>(cerceve);
                if (kit == null) continue;

                dokum.Add($"{Yol(img.transform)}: yerleşik {img.sprite.name} -> " +
                          $"{System.IO.Path.GetFileNameWithoutExtension(cerceve)}");
                img.sprite = kit;
                img.type = Image.Type.Sliced;
                img.fillCenter = true;
                EditorUtility.SetDirty(img);
                baglanan++;
                degisti = true;
            }

            if (degisti) EditorSceneManager.SaveScene(scene);
        }

        foreach (string d in dokum) Debug.Log($"{Tag}_BAGLANTI: {d}");

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: bağlanan {baglanan}, boşaltılan {bosaltilan}, dokunulmayan {dokunulmayan}");
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>Unity'nin yerleşik kaynaklarından mı (UISprite, Knob, Background…)?</summary>
    private static bool Yerlesik(Sprite s)
    {
        if (s == null) return false;
        string yol = AssetDatabase.GetAssetPath(s);
        // Yerleşik kaynaklar "Resources/unity_builtin_extra" ya da
        // "Library/unity default resources" altında; proje varlıkları
        // her zaman "Assets/" ile başlar.
        return string.IsNullOrEmpty(yol) || !yol.StartsWith("Assets/");
    }

    /// <summary>
    /// ScrollRect'in KENDİ parçaları (görünümleri o bileşenin işi).
    /// Ata zincirinin tamamına bakmak fazla geniş: kaydırılan İÇERİK de
    /// elenirdi (AddEmailButton böyle atlanıyordu).
    /// </summary>
    private static bool KaydirmaCubugu(Transform t)
    {
        if (t.GetComponent<Scrollbar>() != null || t.GetComponent<ScrollRect>() != null) return true;
        if (t.name == "Viewport" || t.name == "Handle" || t.name == "Sliding Area") return true;
        return t.parent != null && t.parent.GetComponent<Scrollbar>() != null;
    }

    private static Sprite Yukle(string ad)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconDir}/{ad}_normal.png");
        if (s == null) Debug.LogError($"{Tag}: simge yok -> {IconDir}/{ad}_normal.png");
        return s;
    }

    private static string Hex(Color c)
    {
        Color32 x = c;
        return $"#{x.r:X2}{x.g:X2}{x.b:X2}";
    }

    private static string Yol(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s.Replace("Canvas/SafeAreaRoot/", "").Replace("Canvas/", "");
    }
}
