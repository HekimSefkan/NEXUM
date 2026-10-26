using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Katalizör tüpü yatayda 3-slice (3b/B5).
///
/// <c>Entropitup.png</c> 1536×1024 ve göstergede 923×533 birime GERİLİYOR:
/// altın kapaklar da cam gövdeyle birlikte eziliyor, yani tüp kimliğini
/// kaybediyor. Yatay 3-slice'ta kapaklar sabit kalır, yalnızca gövde uzar.
///
/// GÖRSEL ÜRETİLMEDİ — yalnızca import ayarı:
///   spriteBorder.x = 352  (sol altın kapak: doku x 171..352)
///   spriteBorder.z = 320  (sağ altın kapak: doku x 1216..1348)
///   spriteBorder.y = w = 0 (dikeyde tek dilim; yükseklik eskisi gibi ölçeklenir)
///
/// <b>PPU 100 → 166.</b> 9-slice kenarlıkları birim olarak
/// <c>pixel / PPU × 100</c> çizilir. PPU 100'de 352 piksellik kapak 352
/// BİRİM çizilirdi; oysa bugünkü yatay ölçek 923/1536 = 0,601, yani kapak
/// ekranda 212 birim. PPU = 100 / 0,601 ≈ 166 kapakları bugünkü ölçüsünde
/// tutuyor (212 ve 193 birim), orta 518 birim geriliyor.
///
/// Menüden: NEXUM → Katalizör Tüpünü 3-Slice Yap
/// Batch:   -executeMethod NexumVial3Slice.Run
/// </summary>
public static class NexumVial3Slice
{
    private const string Tag = "NEXUM_TUP";
    private const string VialPath = "Assets/Art/Figures/Entropitup.png";

    private static readonly Vector4 Border = new Vector4(352f, 0f, 320f, 0f);
    private const float Ppu = 166f;

    [MenuItem("NEXUM/Katalizör Tüpünü 3-Slice Yap")]
    public static void Run()
    {
        TextureImporter imp = AssetImporter.GetAtPath(VialPath) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError($"{Tag}: importer yok -> {VialPath}");
            Debug.Log($"{Tag}: FAIL");
            return;
        }

        TextureImporterSettings ayar = new TextureImporterSettings();
        imp.ReadTextureSettings(ayar);

        bool degisti = false;
        if (ayar.spriteBorder != Border) { ayar.spriteBorder = Border; degisti = true; }
        if (!Mathf.Approximately(ayar.spritePixelsPerUnit, Ppu))
        {
            ayar.spritePixelsPerUnit = Ppu;
            degisti = true;
        }

        if (degisti)
        {
            imp.SetTextureSettings(ayar);
            imp.SaveAndReimport();
            Debug.Log($"{Tag}: border {Border}, PPU {Ppu:F0}");
        }
        else
        {
            Debug.Log($"{Tag}: import ayarı zaten yerinde");
        }

        CizimTipi();
        Debug.Log($"{Tag}: OK");
    }

    /// <summary>Tüpü çizen Image'lar Sliced'a alınır (yoksa kenarlık işlemez).</summary>
    private static void CizimTipi()
    {
        Sprite tup = AssetDatabase.LoadAssetAtPath<Sprite>(VialPath);
        if (tup == null) return;

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        int n = 0;

        foreach (Image img in Object.FindObjectsOfType<Image>(true))
        {
            if (img.sprite != tup) continue;
            if (img.type == Image.Type.Sliced && img.fillCenter) continue;

            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            EditorUtility.SetDirty(img);
            n++;
            Debug.Log($"{Tag}_CIZIM: {img.name} -> Sliced");
        }

        if (n > 0) EditorSceneManager.SaveScene(scene);
        Debug.Log($"{Tag}_CIZIM: {n} görsel güncellendi");
    }
}
