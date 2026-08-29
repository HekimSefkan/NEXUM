#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class FontReplacerWindow : EditorWindow
{
    [Header("Orbitron Ailesi (Başlıklar ve Sayılar)")]
    public TMP_FontAsset orbitronBold;
    public TMP_FontAsset orbitronSemiBold;
    public TMP_FontAsset orbitronMedium;

    [Header("Oxanium Ailesi (Açıklamalar ve Butonlar)")]
    public TMP_FontAsset oxaniumSemiBold;
    public TMP_FontAsset oxaniumMedium;
    public TMP_FontAsset oxaniumRegular;

    [MenuItem("NEXUM/Gelişmiş Tipografi Yöneticisi")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacerWindow>("NEXUM Tipografi");
    }

    void OnGUI()
    {
        GUILayout.Label("NEXUM - 6 Kademeli Font Otomasyonu", EditorStyles.boldLabel);
        GUILayout.Space(10);

        orbitronBold = (TMP_FontAsset)EditorGUILayout.ObjectField("Orbitron Bold (Logo/Level)", orbitronBold, typeof(TMP_FontAsset), false);
        orbitronSemiBold = (TMP_FontAsset)EditorGUILayout.ObjectField("Orbitron SemiBold (Büyük Başlık)", orbitronSemiBold, typeof(TMP_FontAsset), false);
        orbitronMedium = (TMP_FontAsset)EditorGUILayout.ObjectField("Orbitron Medium (Alt Başlık/İsim)", orbitronMedium, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);

        oxaniumSemiBold = (TMP_FontAsset)EditorGUILayout.ObjectField("Oxanium SemiBold (Butonlar)", oxaniumSemiBold, typeof(TMP_FontAsset), false);
        oxaniumMedium = (TMP_FontAsset)EditorGUILayout.ObjectField("Oxanium Medium (Küçük Bilgi/Skor)", oxaniumMedium, typeof(TMP_FontAsset), false);
        oxaniumRegular = (TMP_FontAsset)EditorGUILayout.ObjectField("Oxanium Regular (Açıklama/Metin)", oxaniumRegular, typeof(TMP_FontAsset), false);

        GUILayout.Space(20);

        if (GUILayout.Button("Sahnedeki Tüm Yazıları Kurallara Göre Güncelle", GUILayout.Height(40)))
        {
            ExecuteAdvancedReplacement();
        }
    }

    void ExecuteAdvancedReplacement()
    {
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        int changedCount = 0;

        foreach (var txt in allTexts)
        {
            if (txt.gameObject.scene.name == null) continue; // Sadece açık sahnedekileri bul

            Undo.RecordObject(txt, "Advanced Font Change");

            // Hem objenin kendi ismini hem de "Buton" gibi parent objesinin ismini alır
            string objName = txt.gameObject.name.ToLower();
            string parentName = txt.transform.parent != null ? txt.transform.parent.name.ToLower() : "";
            string combinedName = objName + "_" + parentName; 

            // 1. KURAL: Logo ve Leveller
            if (combinedName.Contains("logo") || combinedName.Contains("level") || combinedName.Contains("sayı")) 
            {
                if (orbitronBold != null) txt.font = orbitronBold;
            }
            // 2. KURAL: Büyük Başlıklar
            else if (combinedName.Contains("title") || combinedName.Contains("başlık") || combinedName.Contains("header")) 
            {
                if (orbitronSemiBold != null) txt.font = orbitronSemiBold;
            }
            // 3. KURAL: Alt Başlıklar ve Mentor İsimleri
            else if (combinedName.Contains("name") || combinedName.Contains("isim") || combinedName.Contains("mentor")) 
            {
                if (orbitronMedium != null) txt.font = orbitronMedium;
            }
            // 4. KURAL: Butonlar
            else if (combinedName.Contains("btn") || combinedName.Contains("button") || combinedName.Contains("buton") || combinedName.Contains("tab")) 
            {
                if (oxaniumSemiBold != null) txt.font = oxaniumSemiBold;
            }
            // 5. KURAL: Küçük İstatistikler ve Skorlar
            else if (combinedName.Contains("score") || combinedName.Contains("skor") || combinedName.Contains("id") || combinedName.Contains("stat") || combinedName.Contains("kaza") || combinedName.Contains("sentez") || combinedName.Contains("teori") || combinedName.Contains("oran")) 
            {
                if (oxaniumMedium != null) txt.font = oxaniumMedium;
            }
            // 6. KURAL: Geri kalan her şey (Eğitim metinleri, uzun açıklamalar vb.)
            else 
            {
                if (oxaniumRegular != null) txt.font = oxaniumRegular;
            }

            EditorUtility.SetDirty(txt);
            changedCount++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"<color=#00B4D8><b>NEXUM TİPOGRAFİ:</b></color> <b>{changedCount}</b> adet metin yeni 6'lı sisteme göre başarıyla güncellendi!");
    }
}
#endif