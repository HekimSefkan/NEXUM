using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BÖLÜM HARİTASI DÜĞÜMLERİNE YILDIZ (Faz 2 / 2.2).
///
/// Her <c>LevelN</c> kapsülünün ALTINA üç yıldızlık küçük bir satır
/// kurar ve <see cref="LevelNodeStars"/> bileşenini bağlar.
///
/// <b>Kapsülün İÇİNE değil ALTINA.</b> Kapsül 400x150 ve içinde bölüm
/// numarası var; yıldızı içeri koymak numarayla yarışırdı. Düğümler
/// dikeyde en az 239 birim arayla duruyor (ölçüldü: L1 -711, L2 -468,
/// ... L7 654), yani kapsüller arasında 89 birimlik boşluk var;
/// 36 birimlik yıldız satırı oraya rahat sığıyor. Üstelik komşu
/// düğümler YATAYDA da ayrık (x = -250 / +250 dönüşümlü), yani
/// çakışma iki eksende birden imkânsız.
///
/// <b>Yıldızlar raycast YAKALAMAZ.</b> Kapsülün dokunma alanının
/// dışındalar ve <c>raycastTarget = false</c>; ne butonu çalarlar ne
/// de dokunma hedefi taramasına girerler.
///
/// Haritanın yeniden tasarımı 2.5'in işi; burada düğümlerin yeri,
/// rengi ve kapsül görselleri DEĞİŞMEDİ.
///
/// Menüden: NEXUM → Bölüm Haritası Yıldızlarını Kur
/// Batch:   -executeMethod LevelMapStarsBuilder.Run
/// </summary>
public static class LevelMapStarsBuilder
{
    private const string Tag = "NEXUM_MAPSTARS";

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string MapPath = "Canvas/SafeAreaRoot/LevelSelectionPanel/LevelMap/MapBG";

    private const string StarEmptyPath = "Assets/Art/UI/star_empty_normal.png";
    private const string StarFullPath = "Assets/Art/UI/star_full_normal.png";

    /// <summary>Kapsül sayısı. Sahnede Level1..Level7 var.</summary>
    private const int NodeCount = 7;

    private static readonly Vector2 StarSize = new Vector2(36f, 36f);
    private const float StarSpacing = 44f;

    /// <summary>Kapsül yarı yüksekliği 75; satır onun 20 birim altında.</summary>
    private const float RowY = -95f;

    [MenuItem("NEXUM/Bölüm Haritası Yıldızlarını Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("LevelMapStars", ScenePath, dryRun);

        Sprite bos = LoadSprite(StarEmptyPath);
        Sprite dolu = LoadSprite(StarFullPath);
        int kurulan = 0;

        for (int n = 1; n <= NodeCount; n++)
        {
            string nodeName = "Level" + n;
            string nodePath = MapPath + "/" + nodeName;

            if (b.FindObject(nodePath) == null)
            {
                b.Error("bölüm düğümü yok: " + nodePath);
                continue;
            }

            GameObject row = b.Ensure(nodePath, "Stars");

            // İLK dry-run'da Ensure null döner (obje henüz yok, kuru
            // koşuda yaratılmıyor). Çocukları o turda kurmaya kalkmak
            // "ebeveyn bulunamadı" hatası üretir ve sapma taramasını
            // kırmızıya çevirir; kurucu bir kez gerçek koşturulduktan
            // sonra bu dal hiç çalışmaz.
            if (row == null) continue;

            b.SetFixed(row, UITokens.Center, new Vector2(3f * StarSpacing, StarSize.y),
                       new Vector2(0f, RowY));

            Image[] images = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject star = b.Ensure(nodePath + "/Stars", "Star" + i);
                b.SetFixed(star, UITokens.Center, StarSize,
                           new Vector2((i - 1) * StarSpacing, 0f));
                images[i] = b.EnsureComponent<Image>(star, image =>
                {
                    // Sahnede BOŞ kaydedilir; dolu hâli çalışma zamanında
                    // LevelNodeStars yazar (kayıttan okunur).
                    image.sprite = bos;
                    image.type = Image.Type.Simple;
                    image.color = Color.white;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                });
            }

            int index = n - 1;
            b.EnsureComponent<LevelNodeStars>(row, s =>
            {
                s.levelIndex = index;
                s.stars = images;
                s.fullSprite = dolu;
                s.emptySprite = bos;
            });

            kurulan++;
        }

        Debug.Log($"{Tag}: {kurulan} düğüme yıldız satırı " +
                  $"({StarSize.x:F0} birim, hatve {StarSpacing:F0}, y={RowY:F0})");

        b.End();
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
