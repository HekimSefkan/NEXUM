using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
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
/// Punto 80 → 70: 80 puntoda en uzun formül kutunun %91'ini dolduruyordu
/// (sınır %85). 74 puntoda yükseklik kuralı geçiyor ama Fe₂O₃ GENİŞLİKTE
/// 205 px isterken kutu 200 px — kırpılıyordu. 70 puntoda ikisi de temiz.
/// Ölçüm: TileTextFitTests.
///
/// KONUM VE BOYUT DEĞİŞMEZ; yalnızca sprite, punto ve bileşen bağları.
/// IDEMPOTENT: değerler yerindeyse prefab'a dokunmaz.
///
/// Menüden: NEXUM → Taş Görsellerini Bağla
/// Batch:   -executeMethod NexumTileVisuals.Run
/// </summary>
public static class NexumTileVisuals
{
    private const string Tag = "NEXUM_TILEVISUAL";
    private const string TileDir = "Assets/Art/Tiles";
    private const float FontSize = 70f;

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

            foreach (TMP_Text tmp in kok.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!Mathf.Approximately(tmp.fontSize, FontSize))
                {
                    tmp.fontSize = FontSize;
                    degisti = true;
                }
            }

            if (degisti)
            {
                PrefabUtility.SaveAsPrefabAsset(kok, normal);
                degisen++;
                Debug.Log($"{Tag}: {tasAdi,-14} derinlik {derinlik} -> seviye {seviye}, punto {FontSize}");
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

        AssetDatabase.SaveAssets();
        Debug.Log($"{Tag}: değişen {degisen}, dokunulmayan {dokunulmayan}, hata {hata}");
        Debug.Log($"{Tag}: {(hata == 0 ? "OK" : "FAIL")}");
    }
}
