using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Etkileşimli elemanların DOKUNMA ALANINI alt sınıra
/// (<see cref="UITokens.TouchMin"/> = 144 birim) tamamlar.
///
/// <b>GÖRSEL BOYUT DEĞİŞMEZ.</b> İlk sürümde <c>sizeDelta</c> büyütülüyordu;
/// bu 52 bileşenin GRAFİĞİNİ de büyüttü ve 13 ekranın tasarımını topluca
/// değiştirdi — bu adımın kapsamı değil. Doğru yöntem: grafiğe dokunmadan,
/// elemanın altına SAYDAM bir <c>TouchArea</c> çocuğu konur. uGUI'de tıklama
/// olayı raycast'i yakalayan <c>Graphic</c>'ten en yakın
/// <c>IPointerClickHandler</c> atasına kabarır; saydam çocuk da aynı butonu
/// tetikler. <c>Selectable.targetGraphic</c> ve geçişleri etkilenmez.
///
/// Çocuk, elemanın merkezinde ve YALNIZCA eksik kalan eksende büyür:
/// 250x100 bir buton 250x144 dokunma alanı alır, grafiği 250x100 kalır.
///
/// NEDEN: 2d-3a teşhisinde ayarlar panelindeki üç mod butonunun 56x56'ya
/// çöktüğü görüldü ve testler bunu yakalamadı — "içeride mi / çakışıyor mu"
/// ölçütlerini çökme İYİLEŞTİRİYOR. Taramada 52 etkileşimli elemanın sınırın
/// altında olduğu çıktı; en küçüğü 86,9x30.
///
/// NE YAPMAZ: iki eksende de stretch olan (boyutu ebeveynden gelen)
/// elemanlara dokunmaz. Zaten 144'ü geçen eksende çocuk büyütülmez.
/// ScrollRect'in kaydırma çubukları muaftır (<see cref="Muaf"/>).
///
/// IDEMPOTENT: dokunma alanı zaten yeterliyse dosyaya dokunmaz.
///
/// Menüden: NEXUM → Dokunma Alanlarını Tamamla
/// Batch:   -executeMethod NexumTouchTargets.Run
/// </summary>
public static class NexumTouchTargets
{
    private const string Tag = "NEXUM_DOKUNMA";
    private const string AreaName = "TouchArea";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity",
    };

    [MenuItem("NEXUM/Dokunma Alanlarını Tamamla")]
    public static void Run()
    {
        int eklenen = 0, guncellenen = 0, yeterli = 0, esnek = 0;
        List<string> dokum = new List<string>();

        foreach (string yol in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(yol, OpenSceneMode.Single);
            bool degisti = QuizAraligi(scene, dokum);

            foreach (GameObject kok in scene.GetRootGameObjects())
            {
                foreach (Selectable s in kok.GetComponentsInChildren<Selectable>(true))
                {
                    int sonuc = Tamamla(s, dokum);
                    if (sonuc == 1) { eklenen++; degisti = true; }
                    else if (sonuc == 2) { guncellenen++; degisti = true; }
                    else if (sonuc == 3) esnek++;
                    else yeterli++;
                }
            }

            if (yol.EndsWith("Game.unity") && HudSutunu(dokum)) degisti = true;

            if (degisti) EditorSceneManager.SaveScene(scene);
            Debug.Log($"{Tag}: {Path.GetFileName(yol)} işlendi");
        }

        foreach (string p in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories))
        {
            string normal = p.Replace("\\", "/");
            GameObject kok = PrefabUtility.LoadPrefabContents(normal);
            bool degisti = false;

            foreach (Selectable s in kok.GetComponentsInChildren<Selectable>(true))
            {
                int sonuc = Tamamla(s, dokum);
                if (sonuc == 1) { eklenen++; degisti = true; }
                else if (sonuc == 2) { guncellenen++; degisti = true; }
                else if (sonuc == 3) esnek++;
                else yeterli++;
            }

            if (degisti) PrefabUtility.SaveAsPrefabAsset(kok, normal);
            PrefabUtility.UnloadPrefabContents(kok);
        }

        foreach (string d in dokum) Debug.Log($"{Tag}_ALAN: {d}");

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: eklenen {eklenen}, güncellenen {guncellenen}, " +
                  $"zaten yeterli {yeterli}, esnek (ebeveynden) {esnek}");
        Debug.Log($"{Tag}: GÖRÜNÜR BOYUT DEĞİŞTİRİLEN BİLEŞEN: 0");
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>
    /// Dokunma alanı KESİŞMESİNİ gideren konum düzeltmeleri.
    ///
    /// Kural: komşuyu çalmamak 144'e ulaşmaktan önce gelir. Grafik ölçüsü
    /// değişmez, yalnızca aralık açılır.
    ///
    /// Quiz şıkları: grafik 600x120, hatve 140 idi; 144'lük alanlar 4 birim
    /// kesişiyordu. Hatve 148 (y 60 / -88 / -236 / -384).
    /// </summary>
    private static readonly Dictionary<string, float> KonumY = new Dictionary<string, float>
    {
        { "OptionButton_A",   60f },
        { "OptionButton_B",  -88f },
        { "OptionButton_C", -236f },
        { "OptionButton_D", -384f },

        // Kayıt ekranı: 271x50 grafiğin 144'lük alanı alt sıra mentor
        // kartlarıyla 9 birim kesişiyordu. Buton zaten görünmez (alfa 0).
        { "DetailsButton",  -492f },
    };

    private static bool QuizAraligi(Scene scene, List<string> dokum)
    {
        bool degisti = false;
        foreach (GameObject kok in scene.GetRootGameObjects())
        {
            foreach (RectTransform rt in kok.GetComponentsInChildren<RectTransform>(true))
            {
                float hedef;
                if (!KonumY.TryGetValue(rt.name, out hedef)) continue;
                if (Mathf.Approximately(rt.anchoredPosition.y, hedef)) continue;

                dokum.Add($"{Yol(rt)}  y {rt.anchoredPosition.y:F0} -> {hedef:F0} " +
                          "(dokunma alanı kesişmesi)");
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, hedef);
                EditorUtility.SetDirty(rt);
                degisti = true;
            }
        }
        return degisti;
    }

    /// <summary>
    /// Oyun HUD'unun sağ sütunu: dokunma alanları kesişiyordu
    /// (JokerButton x UndoButton 3680 birim², JokerButton x PauseButton
    /// 4438 birim²). GRAFİK ÖLÇÜLERİ DEĞİŞMEDİ; yalnızca dikey ritim
    /// sıkıştırıldı.
    ///
    /// Sütun bütçesi: tahtanın üstü (290) ile güvenli alanın tepesi (960)
    /// arası 670 birim. Dokunma kapsamları 230 (ipucu karakteri, kendi
    /// grafiği zaten 144'ün üstünde) + 3 x 144 = 662; geriye 8 birim boşluk
    /// kalıyor. Bu yüzden aralıklar 2 birim: görünmez alanlar için yeterli,
    /// çakışma yok.
    ///
    /// Hedef SafeAreaRoot-yerel MERKEZ olarak yazılı; ölçülen merkezle farkı
    /// alınıp anchoredPosition'a eklenir — pivot/anchor karmaşasından
    /// bağımsız ve İDEMPOTENT.
    /// </summary>
    private static readonly Dictionary<string, Vector2> HudMerkez =
        new Dictionary<string, Vector2>
        {
            { "HintButton",  new Vector2(400f, 407f) },   // kapsam 292..522
            { "UndoButton",  new Vector2(400f, 596f) },   // 524..668
            { "JokerButton", new Vector2(400f, 742f) },   // 670..814
            { "PauseButton", new Vector2(464f, 888f) },   // 816..960, x 392..536
        };

    private static readonly Vector2 HudReferans = new Vector2(1080f, 1920f);

    private static bool HudSutunu(List<string> dokum)
    {
        RectTransform safe = Object.FindObjectsOfType<RectTransform>(true)
            .FirstOrDefault(r => r.name == "SafeAreaRoot");
        if (safe == null) return false;

        MonoBehaviour safeArea = safe.GetComponents<MonoBehaviour>()
            .FirstOrDefault(m => m != null && m.GetType().Name == "SafeArea");
        if (safeArea != null) safeArea.enabled = false;

        Vector2 aMin = safe.anchorMin, aMax = safe.anchorMax, piv = safe.pivot;
        Vector2 sd = safe.sizeDelta, ap = safe.anchoredPosition;

        safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
        safe.sizeDelta = HudReferans;
        safe.anchoredPosition = Vector2.zero;
        Canvas.ForceUpdateCanvases();

        bool degisti = false;
        foreach (var kv in HudMerkez)
        {
            RectTransform rt = safe.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(r => r.name == kv.Key);
            if (rt == null) { Debug.LogError($"{Tag}: HUD nesnesi yok -> {kv.Key}"); continue; }

            Vector2 merkez = Merkez(safe, rt);
            Vector2 fark = kv.Value - merkez;
            if (fark.sqrMagnitude < 0.25f) continue;

            dokum.Add($"{kv.Key} merkez ({merkez.x:F0},{merkez.y:F0}) -> " +
                      $"({kv.Value.x:F0},{kv.Value.y:F0})  — grafik ölçüsü değişmedi");
            rt.anchoredPosition += fark;
            EditorUtility.SetDirty(rt);
            degisti = true;
            Canvas.ForceUpdateCanvases();
        }

        safe.anchorMin = aMin; safe.anchorMax = aMax; safe.pivot = piv;
        safe.sizeDelta = sd; safe.anchoredPosition = ap;
        if (safeArea != null) safeArea.enabled = true;
        return degisti;
    }

    private static Vector2 Merkez(RectTransform kok, RectTransform rt)
    {
        Vector3[] k = new Vector3[4];
        rt.GetWorldCorners(k);
        Vector3 a = kok.InverseTransformPoint(k[0]);
        Vector3 b = kok.InverseTransformPoint(k[2]);
        return new Vector2((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f);
    }

    /// <summary>Kaydırma çubukları: ölçüleri kasıtlı olarak ince.</summary>
    private static bool Muaf(Selectable s)
    {
        return s is Scrollbar;
    }

    /// <returns>1 = çocuk eklendi, 2 = çocuk güncellendi, 3 = esnek, 0 = yeterli</returns>
    private static int Tamamla(Selectable s, List<string> dokum)
    {
        RectTransform rt = s.transform as RectTransform;
        if (rt == null || Muaf(s)) return 0;

        bool esnekX = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x);
        bool esnekY = !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);
        if (esnekX && esnekY) return 3;

        float w = esnekX ? UITokens.TouchMin : rt.sizeDelta.x;
        float h = esnekY ? UITokens.TouchMin : rt.sizeDelta.y;
        if (w + 0.5f >= UITokens.TouchMin && h + 0.5f >= UITokens.TouchMin)
        {
            TemizleVarsa(s, dokum);
            return 0;
        }

        Vector2 hedef = new Vector2(Mathf.Max(w, UITokens.TouchMin),
                                    Mathf.Max(h, UITokens.TouchMin));

        Transform mevcut = rt.Find(AreaName);
        bool yeni = mevcut == null;

        RectTransform alan;
        if (yeni)
        {
            GameObject go = new GameObject(AreaName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rt, false);
            go.transform.SetAsFirstSibling();     // grafiğin ALTINDA kalsın
            alan = (RectTransform)go.transform;
        }
        else
        {
            alan = (RectTransform)mevcut;
        }

        bool fark = yeni
                    || alan.anchorMin != UITokens.Center
                    || alan.anchorMax != UITokens.Center
                    || alan.pivot != UITokens.Center
                    || alan.sizeDelta != hedef
                    || alan.anchoredPosition != Vector2.zero;

        alan.anchorMin = alan.anchorMax = alan.pivot = UITokens.Center;
        alan.sizeDelta = hedef;
        alan.anchoredPosition = Vector2.zero;
        alan.localScale = Vector3.one;

        Image img = alan.GetComponent<Image>() ?? alan.gameObject.AddComponent<Image>();
        if (img.sprite != null || img.color.a > 0.001f || !img.raycastTarget)
        {
            img.sprite = null;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            fark = true;
        }

        if (!fark) return 0;

        dokum.Add($"{Yol(rt)}  grafik {w:F0}x{h:F0} (değişmedi) -> dokunma alanı " +
                  $"{hedef.x:F0}x{hedef.y:F0}");
        EditorUtility.SetDirty(alan);
        return yeni ? 1 : 2;
    }

    /// <summary>Eleman artık yeterliyse bırakılmış dokunma alanını boşaltır.</summary>
    private static void TemizleVarsa(Selectable s, List<string> dokum)
    {
        Transform t = s.transform.Find(AreaName);
        if (t == null) return;

        RectTransform alan = (RectTransform)t;
        if (alan.sizeDelta == Vector2.zero) return;

        // Silme yetkisi yok: alan sıfırlanır, obje kalır.
        alan.sizeDelta = Vector2.zero;
        EditorUtility.SetDirty(alan);
        dokum.Add($"{Yol(s.transform)}  dokunma alanı artık gereksiz, sıfırlandı");
    }

    private static string Yol(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s.Replace("Canvas/SafeAreaRoot/", "").Replace("Canvas/", "");
    }
}
