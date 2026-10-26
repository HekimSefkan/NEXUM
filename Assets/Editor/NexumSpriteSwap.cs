using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sahnelerdeki ve prefab'lardaki sprite referanslarını Metal kiti
/// karşılıklarıyla değiştirir.
///
/// KURAL: yalnızca SPRITE değişir. Konum, boyut, anchor ve punto'ya
/// dokunulmaz. Kit çerçeveleri 9-slice olduğu için mevcut RectTransform
/// ölçüsüne aynen oturur; bu yüzden değişim yerleşimi bozmaz.
///
/// Metni GÖRSELİNİN İÇİNDE taşıyan butonlar (oyna / nasıloynanır /
/// ansiklopedi / ayarlar) bu tabloda YOKTUR: onların yerine kit çerçevesi
/// koymak metni siler, yerine TMP etiketi EKLEMEK gerekir — bu bir yapı
/// değişikliğidir ve 3b'ye bırakılmıştır.
///
/// IDEMPOTENT: sprite zaten hedefteyse dosyaya dokunmaz.
///
/// Menüden: NEXUM → Sprite'ları Kite Taşı
/// Batch:   -executeMethod NexumSpriteSwap.Run
/// </summary>
public static class NexumSpriteSwap
{
    private const string Tag = "NEXUM_SPRITESWAP";

    private struct Kural
    {
        public string eski;
        public string yeni;
        public string gerekce;
    }

    private static readonly Kural[] Tablo =
    {
        // --- arka planlar: seçenek C (hibrit) pişirilmiş hâliyle
        new Kural { eski = "Assets/Art/Figures/arkaplan.png",
                    yeni = "Assets/Art/Figures/background_c_main.png",
                    gerekce = "arka plan C: fotoğraf + ızgara + panel şeridi (pişirilmiş)" },
        new Kural { eski = "Assets/Art/Figures/level_selection_arkaplan.png",
                    yeni = "Assets/Art/Figures/background_c_level.png",
                    gerekce = "aynı dil, bölüm haritası" },
        new Kural { eski = "Assets/Art/Figures/Registration_arkaplan.png",
                    yeni = "Assets/Art/Figures/background_c_register.png",
                    gerekce = "aynı dil, kayıt ekranı" },

        // --- marka
        new Kural { eski = "Assets/Art/Figures/NEXUM_logo.png",
                    yeni = "Assets/Art/Figures/NEXUM_logo_metal.png",
                    gerekce = "logo B: geometri aynı, malzeme Metal kiti" },

        // --- 9-slice çerçeveler (metni görselde taşımayanlar)
        new Kural { eski = "Assets/Art/Figures/gamepanel.png",
                    yeni = "Assets/Art/UI/modal_normal.png",
                    gerekce = "boyalı buton yuvası olmayan 9-slice modal çerçeve" },
        new Kural { eski = "Assets/Art/Figures/SettingsPanel_.png",
                    yeni = "Assets/Art/UI/panel_lg_normal.png",
                    gerekce = "cam kart -> kit paneli" },
        new Kural { eski = "Assets/Art/Figures/Nasiloyanir_arkaplan2.png",
                    yeni = "Assets/Art/UI/panel_md_normal.png",
                    gerekce = "pop-up çerçevesi -> kit paneli" },
        new Kural { eski = "Assets/Art/Figures/Registration_cartblue.png",
                    yeni = "Assets/Art/UI/panel_md_normal.png",
                    gerekce = "kart çerçevesi -> kit paneli" },
        new Kural { eski = "Assets/Art/Figures/Registration_avatarkartı.png",
                    yeni = "Assets/Art/UI/avatar_frame_normal.png",
                    gerekce = "mentor kartı -> kit avatar çerçevesi" },
        new Kural { eski = "Assets/Art/Figures/avatar_cerceve.png",
                    yeni = "Assets/Art/UI/avatar_frame_normal.png",
                    gerekce = "profil avatarı -> kit avatar çerçevesi" },

        // --- butonlar (metin ayrı TMP objesinde olanlar)
        new Kural { eski = "Assets/Art/Figures/SettingsPanel_modsecimkartı.png",
                    yeni = "Assets/Art/UI/btn_secondary_normal.png",
                    gerekce = "mod seçimi ve oyun sonu butonları -> kit ikincil butonu" },
        new Kural { eski = "Assets/Art/Figures/Nasiloyanir_Buton.png",
                    yeni = "Assets/Art/UI/btn_secondary_normal.png",
                    gerekce = "panel içi butonlar -> kit ikincil butonu" },
        new Kural { eski = "Assets/Art/Figures/game_button.png",
                    yeni = "Assets/Art/UI/btn_ghost_normal.png",
                    gerekce = "GERİ AL / skor -> kit hayalet butonu" },
        new Kural { eski = "Assets/Art/Figures/game_button1.png",
                    yeni = "Assets/Art/UI/btn_secondary_normal.png",
                    gerekce = "PARÇALA -> kit ikincil butonu" },
        new Kural { eski = "Assets/Art/Figures/Registration_girişyapbutonu.png",
                    yeni = "Assets/Art/UI/btn_primary_normal.png",
                    gerekce = "kayıt birincil eylemi" },
        new Kural { eski = "Assets/Art/Figures/Registration_isim_butonu.png",
                    yeni = "Assets/Art/UI/input_field_normal.png",
                    gerekce = "isim girdisi -> kit girdi alanı" },
        new Kural { eski = "Assets/Art/Figures/Registration_emailbuton.png",
                    yeni = "Assets/Art/UI/input_field_normal.png",
                    gerekce = "e-posta girdisi -> kit girdi alanı" },
        new Kural { eski = "Assets/Art/Figures/level_selection_button.png",
                    yeni = "Assets/Art/UI/level_node_open.png",
                    gerekce = "açık bölüm düğümü" },
        new Kural { eski = "Assets/Art/Figures/level_selection_kilitlibutton.png",
                    yeni = "Assets/Art/UI/level_node_locked.png",
                    gerekce = "kilitli bölüm düğümü" },
        new Kural { eski = "Assets/Art/Figures/SettingsPanel_toogleacık.png",
                    yeni = "Assets/Art/UI/toggle_on_normal.png",
                    gerekce = "açık anahtar" },
        new Kural { eski = "Assets/Art/Figures/SettingsPanel_tooglekapalı.png",
                    yeni = "Assets/Art/UI/toggle_off_normal.png",
                    gerekce = "kapalı anahtar" },
        new Kural { eski = "Assets/Art/Figures/Cember.png",
                    yeni = "Assets/Art/Figures/ring_progress.png",
                    gerekce = "dolu daire -> gerçek halka (radyal dolgu dilim çiziyordu)" },
    };

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    [MenuItem("NEXUM/Sprite'ları Kite Taşı")]
    public static void Run()
    {
        Dictionary<Sprite, Kural> harita = new Dictionary<Sprite, Kural>();
        Dictionary<Sprite, Sprite> hedefler = new Dictionary<Sprite, Sprite>();

        foreach (Kural k in Tablo)
        {
            Sprite eski = AssetDatabase.LoadAssetAtPath<Sprite>(k.eski);
            Sprite yeni = AssetDatabase.LoadAssetAtPath<Sprite>(k.yeni);
            if (eski == null) { Debug.LogError($"{Tag}: eski sprite yok -> {k.eski}"); continue; }
            if (yeni == null) { Debug.LogError($"{Tag}: yeni sprite yok -> {k.yeni}"); continue; }

            harita[eski] = k;
            hedefler[eski] = yeni;
        }

        Dictionary<string, int> dokum = new Dictionary<string, int>();
        int degisen = 0;

        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            int d = 0;

            foreach (GameObject kok in scene.GetRootGameObjects())
            {
                foreach (Image img in kok.GetComponentsInChildren<Image>(true))
                {
                    if (Uygula(img, harita, hedefler, dokum)) d++;
                }

                foreach (MonoBehaviour mb in kok.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    d += AlanlariTasi(mb, hedefler, dokum);
                }
            }

            if (d > 0) EditorSceneManager.SaveScene(scene);
            degisen += d;
            Debug.Log($"{Tag}: {Path.GetFileName(yol)} -> değişen {d}");
        }

        int pd = 0;
        foreach (string yol in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories))
        {
            string normal = yol.Replace("\\", "/");
            GameObject kok = PrefabUtility.LoadPrefabContents(normal);
            bool degisti = false;

            foreach (Image img in kok.GetComponentsInChildren<Image>(true))
            {
                if (Uygula(img, harita, hedefler, dokum)) { degisti = true; pd++; }
            }

            foreach (MonoBehaviour mb in kok.GetComponentsInChildren<MonoBehaviour>(true))
            {
                int n = AlanlariTasi(mb, hedefler, dokum);
                if (n > 0) { degisti = true; pd += n; }
            }

            if (degisti) PrefabUtility.SaveAsPrefabAsset(kok, normal);
            PrefabUtility.UnloadPrefabContents(kok);
        }

        degisen += pd;
        Debug.Log($"{Tag}: prefab'lar -> değişen {pd}");

        foreach (var kv in dokum.OrderByDescending(k => k.Value))
        {
            Debug.Log($"{Tag}_ESLEME: {kv.Key} x{kv.Value}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: TOPLAM değişen {degisen}");
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>
    /// Scriptlerin SERİLEŞTİRİLMİŞ Sprite alanlarını da taşır.
    ///
    /// NEDEN: tarama yalnızca <c>Image.sprite</c>'a bakıyordu. Ayarlar
    /// anahtarları <c>SettingsManager.toggleOnSprite/toggleOffSprite</c>
    /// alanlarında duruyor ve çalışma zamanında atanıyor; bu yüzden kit
    /// geçişinde eski görselde kaldılar ve sahnede beyaz dikdörtgen
    /// göründüler. Aynı boşluk kilitli bölüm düğümleri gibi başka
    /// alanlarda da vardı.
    /// </summary>
    private static int AlanlariTasi(MonoBehaviour mb, Dictionary<Sprite, Sprite> hedefler,
                                    Dictionary<string, int> dokum)
    {
        if (mb == null) return 0;

        SerializedObject so = new SerializedObject(mb);
        SerializedProperty it = so.GetIterator();
        int n = 0;

        while (it.NextVisible(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

            Sprite eski = it.objectReferenceValue as Sprite;
            if (eski == null) continue;

            Sprite yeni;
            if (!hedefler.TryGetValue(eski, out yeni) || yeni == eski) continue;

            it.objectReferenceValue = yeni;
            string anahtar = $"{mb.GetType().Name}.{it.name}: {eski.name} -> {yeni.name}";
            dokum[anahtar] = dokum.TryGetValue(anahtar, out int k) ? k + 1 : 1;
            n++;
        }

        if (n > 0) so.ApplyModifiedPropertiesWithoutUndo();
        return n;
    }

    private static bool Uygula(Image img, Dictionary<Sprite, Kural> harita,
                               Dictionary<Sprite, Sprite> hedefler, Dictionary<string, int> dokum)
    {
        if (img == null || img.sprite == null) return false;

        Sprite yeni;
        if (!hedefler.TryGetValue(img.sprite, out yeni)) return false;
        if (img.sprite == yeni) return false;

        Kural k = harita[img.sprite];
        string anahtar = Path.GetFileName(k.eski) + " -> " + Path.GetFileName(k.yeni);

        img.sprite = yeni;

        // 9-slice çerçeveler Sliced çizilmeli ki köşe geometrisi bozulmasın
        if (yeni.border != Vector4.zero && img.type != Image.Type.Tiled)
        {
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
        }

        EditorUtility.SetDirty(img);
        dokum[anahtar] = dokum.TryGetValue(anahtar, out int n) ? n + 1 : 1;
        return true;
    }
}
