using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Taş prefab'larına Metal kitinin taş sprite'larını ve türetilen seviyeyi
/// bağlar.
///
/// SEVİYE TEK KAYNAKTAN: prefab'da elle seviye alanı yok; seviye
/// <see cref="TileLevels"/> tarafından tarif grafiğinden türetilir. Eskiden
/// `1/2/3.seviyetas.png` prefab'lara ELLE atanmıştı ve 4 taşta türetimle
/// çelişiyordu (CO₂, H₂O, NH₃, NaCl).
///
/// <b>2d-3a düzeltmesi — metin kutusu, renk ve punto.</b>
///
/// Metin kutusu ARTIK TAŞIN TAMAMI DEĞİL, çerçevenin İÇ DÜZ ALANI
/// (<see cref="ContentRatio"/>, taşın %80'i). Önceki sürümde kutu 200×200'dü
/// ama çerçevenin düz iç alanı L1'de 190, L4'te 70 birimdi; metin 200'e göre
/// ölçülüp göze 70'e göre görünüyordu. Üreteçte iç alan dört seviyede de
/// eşitlendi (<c>Tools/generate_ui_kit.py</c>, <c>TILE_CONTENT</c>), kutu da
/// ona oturtuldu.
///
/// <b>Punto FORMÜL BAŞINA hesaplanır ve prefab'a PİŞİRİLİR.</b> "Tek punto"
/// kuralı SEVİYELER arası eşitlik içindi (bir taş seviyesi yüzünden küçük
/// görünmesin), formüller arası değil. Tek punto uygulandığında 26 formülün
/// hepsi en uzun formüle (CaCO₃) göre küçülüyordu; "H" gereksiz yere 44
/// puntoda kalıyordu. Artık her sembol için, iç alana %85 kuralıyla sığan EN
/// BÜYÜK punto <see cref="MinFont"/>–<see cref="MaxFont"/> arasında aranır.
/// Çalışma zamanı auto-size YOK: değer prefab'da sabit.
///
/// Satır kırma KAPATILDI: kimyasal formül bölünmez — CaCO₃ eski sürümde
/// "CaCO" / "₃" diye iki satıra kırılıyordu ve test bunu göremiyordu.
/// Ölçüm: TileTextFitTests.
///
/// Harf rengi: dördü de <c>textPrimary</c> (beyaz). Eskiden geçiş öncesinden
/// kalma dört aile vardı (#FFD700 ×8, #C0C0C0 ×8, #00FF00 ×7, #60E8F5 ×3);
/// hiçbiri <see cref="NexumColorTokens"/> tablosunda yoktu.
///
/// KONUM VE DIŞ BOYUT DEĞİŞMEZ; sprite, metin kutusu, punto, renk ve
/// bileşen bağları yazılır.
/// IDEMPOTENT: değerler yerindeyse prefab'a dokunmaz.
///
/// Menüden: NEXUM → Taş Görsellerini Bağla
/// Batch:   -executeMethod NexumTileVisuals.Run
/// </summary>
public static class NexumTileVisuals
{
    private const string Tag = "NEXUM_TILEVISUAL";
    private const string TileDir = "Assets/Art/Tiles";

    /// <summary>Çerçevenin iç düz alanı / taş kenarı. Üreteçteki TILE_CONTENT ile aynı.</summary>
    public const float ContentRatio = 0.80f;

    /// <summary>Punto taban/tavanı. Taban okunabilirlik, tavan tasarım sınırı.</summary>
    private const float MinFont = 32f;
    private const float MaxFont = 72f;

    /// <summary>Metin kutusunun doldurabileceği en büyük oran (CLAUDE.md %85 kuralı).</summary>
    private const float MaxFill = 0.85f;

    private static readonly string[] Durumlar =
    {
        "normal", "selected", "merging", "newborn", "discovered",
    };

    [MenuItem("NEXUM/Taş Görsellerini Bağla")]
    public static void Run()
    {
        TileLevels.Clear();

        int degisen = 0, dokunulmayan = 0, hata = 0;
        Dictionary<int, int> dagilim = new Dictionary<int, int>();
        Dictionary<string, float> puntolar = new Dictionary<string, float>();

        foreach (string yol in Directory.GetFiles("Assets/Prefabs", "Tile_*.prefab"))
        {
            string normal = yol.Replace("\\", "/");
            string tasAdi = Path.GetFileNameWithoutExtension(normal);

            int seviye = TileLevels.Level(tasAdi);
            int derinlik = TileLevels.Depth(tasAdi);
            if (derinlik == TileLevels.UnknownDepth)
            {
                Debug.LogError($"{Tag}: seviye türetilemedi -> {tasAdi}");
                hata++;
                continue;
            }
            dagilim[seviye] = dagilim.TryGetValue(seviye, out int n) ? n + 1 : 1;

            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
            bool eksik = false;
            foreach (string durum in Durumlar)
            {
                string sp = $"{TileDir}/tile_L{seviye}_{durum}.png";
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(sp);
                if (s == null)
                {
                    Debug.LogError($"{Tag}: sprite yok -> {sp}");
                    eksik = true;
                    break;
                }
                sprites[durum] = s;
            }
            if (eksik) { hata++; continue; }

            GameObject kok = PrefabUtility.LoadPrefabContents(normal);
            bool degisti = false;

            Image img = kok.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogError($"{Tag}: {tasAdi} kökünde Image yok");
                PrefabUtility.UnloadPrefabContents(kok);
                hata++;
                continue;
            }

            if (img.sprite != sprites["normal"]) { img.sprite = sprites["normal"]; degisti = true; }
            if (img.type != Image.Type.Simple) { img.type = Image.Type.Simple; degisti = true; }
            if (img.color != Color.white) { img.color = Color.white; degisti = true; }

            TileVisual gorsel = kok.GetComponent<TileVisual>();
            if (gorsel == null) { gorsel = kok.AddComponent<TileVisual>(); degisti = true; }

            if (gorsel.level != seviye) { gorsel.level = seviye; degisti = true; }
            if (gorsel.normal != sprites["normal"]) { gorsel.normal = sprites["normal"]; degisti = true; }
            if (gorsel.selected != sprites["selected"]) { gorsel.selected = sprites["selected"]; degisti = true; }
            if (gorsel.merging != sprites["merging"]) { gorsel.merging = sprites["merging"]; degisti = true; }
            if (gorsel.newborn != sprites["newborn"]) { gorsel.newborn = sprites["newborn"]; degisti = true; }
            if (gorsel.discovered != sprites["discovered"]) { gorsel.discovered = sprites["discovered"]; degisti = true; }

            float kenar = (1f - ContentRatio) * 0.5f;
            Vector2 icMin = new Vector2(kenar, kenar);
            Vector2 icMax = new Vector2(1f - kenar, 1f - kenar);

            float kutu = IcAlanGenisligi();

            foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
            {
                // Kırma kapalıyken ölç: açıkken taşma sessizce satır kırmaya
                // dönüşür ve ölçüm göremez (2d-3a teşhisi).
                tmp.enableWordWrapping = false;

                float punto = EnBuyukPunto(tmp, kutu);
                if (!Mathf.Approximately(tmp.fontSize, punto))
                {
                    tmp.fontSize = punto;
                    degisti = true;
                }
                puntolar[tasAdi] = punto;

                if (tmp.color != NexumPalette.TextPrimary)
                {
                    tmp.color = NexumPalette.TextPrimary;
                    degisti = true;
                }

                // Kimyasal formül bölünmez: kırma açıkken taşma sessizce
                // satır kırmaya dönüşüyor ve ölçüm göremiyordu.
                if (tmp.enableWordWrapping)
                {
                    tmp.enableWordWrapping = false;
                    degisti = true;
                }

                // Metin kutusu = çerçevenin iç düz alanı (oransal: hücre
                // boyutu değişse de pay korunur).
                RectTransform rt = tmp.rectTransform;
                if (rt.anchorMin != icMin || rt.anchorMax != icMax ||
                    rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero)
                {
                    rt.anchorMin = icMin;
                    rt.anchorMax = icMax;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    degisti = true;
                }
            }

            if (degisti)
            {
                PrefabUtility.SaveAsPrefabAsset(kok, normal);
                degisen++;
                Debug.Log($"{Tag}: {tasAdi,-14} derinlik {derinlik} -> seviye {seviye}, " +
                          $"punto {puntolar[tasAdi]:F0}");
            }
            else
            {
                dokunulmayan++;
            }

            PrefabUtility.UnloadPrefabContents(kok);
        }

        foreach (var kv in dagilim.OrderBy(k => k.Key))
        {
            Debug.Log($"{Tag}_DAGILIM: seviye {kv.Key} -> {kv.Value} taş");
        }

        foreach (var kv in puntolar.OrderBy(k => k.Value))
        {
            Debug.Log($"{Tag}_PUNTO: {kv.Key,-16} {kv.Value:F0}");
        }
        if (puntolar.Count > 0)
        {
            Debug.Log($"{Tag}_PUNTO_ARALIK: en küçük {puntolar.Values.Min():F0}, " +
                      $"en büyük {puntolar.Values.Max():F0} " +
                      $"(taban {MinFont:F0}, tavan {MaxFont:F0})");
        }

        hata += BosHucreleriKur();

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: değişen {degisen}, dokunulmayan {dokunulmayan}, hata {hata}");
        Debug.Log($"{Tag}: {(hata == 0 ? "OK" : "FAIL")}");
    }


    /// <summary>
    /// Taşın iç alanının GENİŞLİĞİ (birim). Hücre ölçüsü Game sahnesindeki
    /// <c>GridBoard</c>'un <c>GridLayoutGroup</c>'undan okunur — sabit
    /// yazılmaz, tahta ölçüsü değişirse punto da değişir.
    /// </summary>
    private static float IcAlanGenisligi()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        GridLayoutGroup grid = Object.FindObjectsOfType<GridLayoutGroup>(true)
            .FirstOrDefault(g => g.name == "GridBoard");
        float hucre = grid != null ? grid.cellSize.x : 200f;
        if (grid == null) Debug.LogWarning($"{Tag}: GridBoard bulunamadı, hücre 200 varsayıldı");
        return hucre * ContentRatio;
    }

    /// <summary>
    /// Sembolün iç alana %85 kuralıyla sığdığı EN BÜYÜK punto.
    ///
    /// Kırma kapalı olduğu için <c>GetPreferredValues</c> metnin GERÇEKTEN
    /// istediği genişliği verir; yükseklik de aynı kutuya göre ölçülür.
    /// Sonuç tabana dayanırsa bu bir kusurdur ve test düşer — sessizce
    /// kırpılmış metin bırakılmaz.
    /// </summary>
    private static float EnBuyukPunto(TMP_Text tmp, float kutu)
    {
        float eski = tmp.fontSize;
        float sonuc = MinFont;

        for (float p = MaxFont; p >= MinFont; p -= 1f)
        {
            tmp.fontSize = p;
            tmp.ForceMeshUpdate();
            Vector2 ister = tmp.GetPreferredValues(tmp.text, 0f, 0f);
            if (ister.x <= kutu * MaxFill && ister.y <= kutu * MaxFill)
            {
                sonuc = p;
                break;
            }
        }

        tmp.fontSize = eski;
        return sonuc;
    }

    /// <summary>
    /// Boş hücreler görünür bir KUYU olur.
    ///
    /// Eskiden 16 hücrenin Image'ı sprite'sız ve alfa 0'dı; boş hücre yalnızca
    /// tahta arka planından (grid.png) okunuyordu ve dolu taşla arasındaki
    /// kontrast 1,51:1 idi — cihazda boş hücre taşa benziyordu. Artık kitin
    /// <c>tile_empty</c> sprite'ı kullanılıyor; ölçüm TileContrastTests'te.
    ///
    /// Yalnızca MEVCUT bileşenlerin DEĞERLERİ yazılır (sprite + renk); obje
    /// veya bileşen eklenmez.
    /// </summary>
    private static int BosHucreleriKur()
    {
        Sprite bos = AssetDatabase.LoadAssetAtPath<Sprite>(TileDir + "/tile_empty.png");
        if (bos == null)
        {
            Debug.LogError($"{Tag}: boş hücre sprite'ı yok -> {TileDir}/tile_empty.png");
            return 1;
        }

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        Transform board = Object.FindObjectsOfType<RectTransform>(true)
            .FirstOrDefault(r => r.name == "GridBoard");
        if (board == null)
        {
            Debug.LogError($"{Tag}: GridBoard yok");
            return 1;
        }

        int yazilan = 0;
        foreach (Transform hucre in board)
        {
            Image img = hucre.GetComponent<Image>();
            if (img == null) continue;

            if (img.sprite == bos && img.color == Color.white && img.type == Image.Type.Simple) continue;

            img.sprite = bos;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;
            EditorUtility.SetDirty(img);
            yazilan++;
        }

        if (yazilan > 0) EditorSceneManager.SaveScene(scene);
        Debug.Log($"{Tag}_BOSHUCRE: {yazilan} hücreye kuyu sprite'ı yazıldı " +
                  $"({board.childCount} hücreden)");
        return 0;
    }
}
