using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ana menüde HİYERARŞİ: bir birincil eylem, dört ikincil (3c/8).
///
/// Beş buton da aynı ikincil çerçeveyi taşıyordu; ekranda "önce şuna bas"
/// diyen hiçbir şey yoktu. OYNA artık birincil rolü (camgöbeği dolgu)
/// taşıyor, diğer dördü ikincil kalıyor.
///
/// YALNIZCA ROL DEĞİŞİR. Bu kurucu butonların boyutuna, konumuna, yazısına
/// ve bağlantısına DOKUNMAZ — tek yazdığı alan <c>Image.sprite</c>. Ölçüm
/// (<c>MenuHierarchyTests</c>) rect'lerin değişmediğini de doğrular.
///
/// Avatar çerçevesi buton bandıyla hizalanır: bandın sol kenarı neredeyse
/// avatar da oradan başlar, üstte güvenli alandan bir boşluk aşağıda.
///
/// Menüden: NEXUM → Menü Hiyerarşisini Kur
/// Batch:   -executeMethod MenuRoleBuilder.Run
/// </summary>
public static class MenuRoleBuilder
{
    private const string Tag = "NEXUM_MENUROL";

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PanelPath = "Canvas/SafeAreaRoot/MainMenuPanel";

    private const string PrimaryPath = "Assets/Art/UI/btn_primary_normal.png";
    private const string SecondaryPath = "Assets/Art/UI/btn_secondary_normal.png";

    /// <summary>Birincil eylem: tek.</summary>
    private const string Primary = "PlayButton";

    private static readonly string[] Secondary =
    {
        "HowToPlayButton", "EncyclopediaButton", "RecipeBookButton", "SettingButton",
    };

    /// <summary>Avatarın üst kenarı ile güvenli alanın tepesi arası.</summary>
    private const float AvatarTopMargin = 36f;

    [MenuItem("NEXUM/Menü Hiyerarşisini Kur")]
    public static void RunFromMenu() { Build(false); }

    public static void Run() { Build(UIBuilder.DryRunRequested); }

    private static void Build(bool dryRun)
    {
        UIBuilder b = UIBuilder.Begin("MenuRole", ScenePath, dryRun);

        Sprite birincil = AssetDatabase.LoadAssetAtPath<Sprite>(PrimaryPath);
        Sprite ikincil = AssetDatabase.LoadAssetAtPath<Sprite>(SecondaryPath);
        if (birincil == null || ikincil == null)
        {
            b.Error("kit buton sprite'ları bulunamadı");
            b.End();
            return;
        }

        Rol(b, Primary, birincil);
        foreach (string ad in Secondary) Rol(b, ad, ikincil);

        Avatar(b);

        b.End();
    }

    private static void Rol(UIBuilder b, string ad, Sprite sprite)
    {
        GameObject go = b.FindObject(PanelPath + "/" + ad);
        if (go == null) { b.Error($"buton yok: {ad}"); return; }

        Image img = go.GetComponent<Image>();
        if (img == null) { b.Error($"buton Image taşımıyor: {ad}"); return; }

        if (img.sprite != sprite)
        {
            Debug.Log($"{Tag}_ROL: {ad}: {(img.sprite != null ? img.sprite.name : "yok")} " +
                      $"-> {sprite.name}");
        }

        // SADECE sprite: rect'e, metne, bağlantıya dokunulmaz
        b.Apply(go, img, i =>
        {
            i.sprite = sprite;
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = Color.white;
        });
    }

    /// <summary>Avatar çerçevesi: buton bandının sol kenarına hizalı, üstte.</summary>
    private static void Avatar(UIBuilder b)
    {
        GameObject avatar = b.FindObject(PanelPath + "/ProfileButton");
        if (avatar == null) { b.Error("ProfileButton yok"); return; }

        RectTransform rt = avatar.GetComponent<RectTransform>();
        if (rt == null) return;

        // Bandın sol kenarı: butonların en solu (ölçülür, sabit yazılmaz)
        List<string> butonlar = new List<string>(Secondary) { Primary };
        float solKenar = butonlar
            .Select(a => b.FindObject(PanelPath + "/" + a))
            .Where(g => g != null)
            .Select(g => g.GetComponent<RectTransform>())
            .Where(r => r != null)
            .Select(r => r.anchoredPosition.x - r.sizeDelta.x * 0.5f)
            .DefaultIfEmpty(0f)
            .Min();

        // ÜST-ORTA anchor: band ekran merkezine göre dizili, avatar da öyle
        // olmalı — sol kenara anchor'lanırsa 1440 genişlikte band yerinde
        // kalır, avatar 30 birim kayardı.
        Vector2 boyut = rt.sizeDelta;
        Vector2 hedef = new Vector2(solKenar + boyut.x * 0.5f,
                                    -(AvatarTopMargin + boyut.y * 0.5f));

        Debug.Log($"{Tag}_AVATAR: band sol kenarı {solKenar:F0}, avatar {boyut.x:F0}x{boyut.y:F0} " +
                  $"-> {hedef.x:F0},{hedef.y:F0}");

        b.SetFixed(avatar, new Vector2(0.5f, 1f), boyut, hedef);
    }
}
