using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bilgi kartı ayarının sahne bağı (1.5).
///
/// "BİLİYOR MUYDUN?" kartı duraklat kartının iki çocuğundan oluşur:
/// <c>SubtitleText</c> (başlık) ve <c>FactText</c> (bilgi metni). Bilgi
/// Kartları ayarı kapalıyken <c>UIManager</c> bu ikisini gizler; bunun için
/// <c>UIManager.flashcardObjects</c> alanının dolu olması gerekir.
///
/// NEDEN AYRI SCRİPT (UIBuilder değil): bu bağ yalnızca MEVCUT bir alanın
/// DEĞERİNİ yazıyor — obje ya da bileşen eklemiyor. Kartı kuran
/// <c>RecipeBookBuilder</c> ise bugün sahnenin kayıtlı hâlini birebir
/// üretmiyor (bir koşuda 25 ilgisiz objeyi güncelliyor: LockIcon sprite'ları,
/// Backdrop, BackButton). Alanı oradan bağlamak, bu adımın PR'ına o sapmayı
/// da sokardı. Sapma <c>docs/NEXT_3B.md</c>'ye yazıldı.
///
/// IDEMPOTENT: alan zaten doğru iki objeye bakıyorsa sahne kaydedilmez.
///
/// Menüden: NEXUM -> Bilgi Kartı Bağını Kur
/// Batch mode: -executeMethod NexumFlashcardBind.Run
/// </summary>
public static class NexumFlashcardBind
{
    private const string Tag = "NEXUM_FLASHCARD";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string CardPath = "Canvas/PausePanel/ContainerBox";

    private static readonly string[] Parcalar = { "SubtitleText", "FactText" };

    [MenuItem("NEXUM/Bilgi Kartı Bağını Kur")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        if (ui == null) { Fail("Game sahnesinde UIManager yok"); return; }

        List<GameObject> hedefler = new List<GameObject>(Parcalar.Length);
        foreach (string ad in Parcalar)
        {
            GameObject go = Bul(scene, CardPath + "/" + ad);
            if (go == null) { Fail("obje yok: " + CardPath + "/" + ad); return; }
            hedefler.Add(go);
        }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty liste = so.FindProperty("flashcardObjects");
        if (liste == null) { Fail("UIManager.flashcardObjects alanı yok"); return; }

        bool ayni = liste.arraySize == hedefler.Count;
        if (ayni)
        {
            for (int i = 0; i < hedefler.Count; i++)
            {
                if (liste.GetArrayElementAtIndex(i).objectReferenceValue != hedefler[i]) { ayni = false; break; }
            }
        }

        if (ayni)
        {
            Debug.Log($"{Tag}: bağ zaten kurulu, sahne dokunulmadı ({hedefler.Count} obje)");
            Debug.Log($"{Tag}: OK");
            return;
        }

        liste.arraySize = hedefler.Count;
        for (int i = 0; i < hedefler.Count; i++)
        {
            liste.GetArrayElementAtIndex(i).objectReferenceValue = hedefler[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        foreach (GameObject go in hedefler) Debug.Log($"{Tag}_BAGLANDI: {go.name}");
        Debug.Log($"{Tag}: OK");
    }

    private static GameObject Bul(Scene scene, string yol)
    {
        string[] parcalar = yol.Split('/');

        Transform current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == parcalar[0]) { current = root.transform; break; }
        }

        for (int i = 1; i < parcalar.Length && current != null; i++)
        {
            current = current.Find(parcalar[i]);
        }
        return current != null ? current.gameObject : null;
    }

    private static void Fail(string mesaj)
    {
        Debug.LogError($"{Tag}_HATA: {mesaj}");
        Debug.Log($"{Tag}: FAIL");
    }
}
