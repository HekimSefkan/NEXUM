using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// NEXUM Sprite Atlas üretici. Tekrar tekrar çalıştırılabilir: her çalıştırmada
// atlas'ları listedeki hâline göre yeniden yazar.
//
// Menüden:  NEXUM / Sprite Atlas'ları Yeniden Oluştur
// Batch:    Unity.exe -batchmode -nographics -quit -projectPath <proje>
//                     -executeMethod NexumAtlasBuilder.Run -logFile <log>
//
// GRUPLAMA GEREKÇESİ
// - Atlas ancak AYNI EKRANDA aynı anda çizilen sprite'ları birleştirdiğinde
//   draw call kazandırır. Bu yüzden iki atlas var: menü ekranları ve oyun ekranı.
// - Avatarlar (9 x 1024x1024) atlasa ALINMADI: aynı anda tek avatar görünüyor,
//   atlaslanırsa tek avatarı göstermek için 2048'lik sayfanın tamamı belleğe
//   yükleniyor. Draw call kazancı yok, bellek maliyeti var.
// - Tutorial molekül/formül görselleri (CO2, H2O, NaCl, ... 1024x1024) aynı
//   nedenle dışarıda: deney föyünde aynı anda en fazla iki tanesi görünüyor.
// - Tam ekran arka planlar ve büyük paneller (arkaplan, gamepanel, KimlikKartı,
//   SettingsPanel_, Entropitup, ...) dışarıda: tek başlarına bir sayfayı
//   dolduruyorlar, atlasa girmeleri sadece israf.
// - Taş görselleri (1./2./3.seviyetas) oyun atlasında: 23 taş prefab'ı bu üç
//   sprite'ı paylaşıyor ve grid'de aynı anda 16 taş çiziliyor.
public static class NexumAtlasBuilder
{
    private const string AtlasFolder = "Assets/Art/Atlases";
    private const string Tag = "NEXUM_ATLAS";

    // Menü, profil, ayarlar, kayıt ve bölüm seçim ekranlarının UI parçaları
    private static readonly string[] MenuSprites =
    {
        "Assets/Art/Figures/oyna.png",
        "Assets/Art/Figures/ayarlar.png",
        "Assets/Art/Figures/ansiklopedi.png",
        "Assets/Art/Figures/nasıloynanır.png",
        "Assets/Art/Figures/NEXUM_logo.png",
        "Assets/Art/Figures/geri.png",
        "Assets/Art/Figures/x.png",
        "Assets/Art/Figures/Tik.png",
        "Assets/Art/Figures/Cember.png",
        "Assets/Art/Figures/avatar_cerceve.png",
        "Assets/Art/Figures/level_selection_button.png",
        "Assets/Art/Figures/level_selection_kilitlibutton.png",
        "Assets/Art/Figures/Nasiloyanir_Buton.png",
        "Assets/Art/Figures/Registration_isim_butonu.png",
        "Assets/Art/Figures/Registration_emailbuton.png",
        "Assets/Art/Figures/Registration_girişyapbutonu.png",
        "Assets/Art/Figures/Registration_avatarkartı.png",
        "Assets/Art/Figures/Registration_cartblue.png",
        "Assets/Art/Figures/SettingsPanel_toogleacık.png",
        "Assets/Art/Figures/SettingsPanel_tooglekapalı.png",
        "Assets/Art/Figures/SettingsPanel_modsecimkartı.png",
        "Assets/Art/Figures/ProfilPanel_ToplamSentez.png",
        "Assets/Art/Figures/ProfilPanel_KeşfedilenBileşik.png",
        "Assets/Art/Figures/ProfilPanel_LabaratuarKazası.png",
        "Assets/Art/Figures/ProfilPanel_TeorikBasarı.png",
    };

    // Oyun ekranının HUD parçaları ve taş görselleri
    private static readonly string[] GameSprites =
    {
        "Assets/Art/Figures/1.seviyetas.png",
        "Assets/Art/Figures/2.seviyetas.png",
        "Assets/Art/Figures/3.seviyetas.png",
        "Assets/Art/Figures/grid.png",
        "Assets/Art/Figures/game_button.png",
        "Assets/Art/Figures/game_button1.png",
        "Assets/Art/Figures/Beherglas.png",
        "Assets/Art/Figures/white-screen.png",
        "Assets/Art/Figures/vecteezy_detective-in-trench-coat-examining-clue-with-magnifying_65428683 (1).png",
    };

    [MenuItem("NEXUM/Sprite Atlas'ları Yeniden Oluştur")]
    public static void Run()
    {
        if (!Directory.Exists(AtlasFolder)) Directory.CreateDirectory(AtlasFolder);

        // Sprite Packer modu: V2'de atlas hem Play modunda hem build'de kullanılır.
        // Değer sayı olarak değil enum adıyla yazılır.
        SpritePackerMode mode = EditorSettings.spritePackerMode;
        Debug.Log(string.Format("{0}: mevcut Sprite Packer modu = {1} ({2})", Tag, mode, (int)mode));
        if (mode != SpritePackerMode.SpriteAtlasV2)
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            Debug.Log(string.Format("{0}: Sprite Packer modu {1} -> SpriteAtlasV2 ({2}) yapildi",
                Tag, mode, (int)SpritePackerMode.SpriteAtlasV2));
        }

        int total = 0;
        total += BuildAtlas("NexumMenuUI", MenuSprites);
        total += BuildAtlas("NexumGameUI", GameSprites);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(string.Format("{0}_SUMMARY: 2 atlas, toplam {1} sprite", Tag, total));
    }

    private static int BuildAtlas(string name, string[] spritePaths)
    {
        string path = Path.Combine(AtlasFolder, name + ".spriteatlasv2").Replace('\\', '/');

        SpriteAtlasAsset atlas = new SpriteAtlasAsset();
        atlas.SetIncludeInBuild(true);

        List<Object> packed = new List<Object>();
        foreach (string spritePath in spritePaths)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning(string.Format("{0}: sprite bulunamadi, atlandi: {1}", Tag, spritePath));
                continue;
            }
            packed.Add(sprite);
        }

        atlas.Add(packed.ToArray());
        SpriteAtlasAsset.Save(atlas, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // V2'de paketleme/doku ayarları asset'te değil .meta içindeki SpriteAtlasImporter'da tutulur.
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer == null)
        {
            Debug.LogWarning(string.Format("{0}: {1} icin SpriteAtlasImporter alinamadi, ayarlar varsayilan kaldi", Tag, name));
        }
        else
        {
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                padding = 4,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
                blockOffset = 1,
            };
            // Mevcut doku ayarlarının yalnızca ilgilendiğimiz alanları değiştirilir
            SpriteAtlasTextureSettings texture = importer.textureSettings;
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.sRGB = true;
            texture.filterMode = FilterMode.Bilinear;
            importer.textureSettings = texture;
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = 2048,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                overridden = false,
            });
            importer.includeInBuild = true;
            importer.SaveAndReimport();
        }

        Debug.Log(string.Format("{0}: {1} -> {2} sprite ({3})", Tag, name, packed.Count,
            string.Join(", ", packed.Select(o => o.name).Take(40))));
        return packed.Count;
    }
}
