using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Font geçişinden sonra %85 sığma kuralını aşan metin kutularının puntosunu
/// düşürür.
///
/// NEDEN: Oxanium, LiberationSans'tan geniş. Aynı metin aynı kutuda bir satır
/// daha kırılıyor ve doluluk sınırı aşıyor. Kural gereği çözüm **punto
/// düşürmek**; konum ve boyut DEĞİŞMEZ.
///
/// Tablodaki her satır ÖLÇÜMLE bulunmuştur; gerekçesi yanında yazılıdır.
/// IDEMPOTENT: punto zaten hedef değerdeyse dokunmaz.
///
/// Menüden: NEXUM → Metin Puntolarını Düzelt
/// Batch:   -executeMethod NexumTextFit.Run
/// </summary>
public static class NexumTextFit
{
    private const string Tag = "NEXUM_TEXTFIT";

    private struct Duzeltme
    {
        public string scene;
        public string path;
        public float eski;
        public float yeni;
        public string gerekce;
    }

    private static readonly Duzeltme[] Tablo =
    {
        new Duzeltme
        {
            scene = "Assets/Scenes/Game.unity",
            path  = "Canvas/AssistantPanel/HintMessageText",
            eski  = 35f, yeni = 32f,
            gerekce = "Oxanium geçişinde 21 ipucundan 6'sı 2 satırdan 3 satıra " +
                      "düştü (319/350 = %91, sınır %85/298). 32 puntoda tekrar 2 satır.",
        },
    };

    [MenuItem("NEXUM/Metin Puntolarını Düzelt")]
    public static void Run()
    {
        Dictionary<string, List<Duzeltme>> sahneBasina = new Dictionary<string, List<Duzeltme>>();
        foreach (Duzeltme d in Tablo)
        {
            if (!sahneBasina.ContainsKey(d.scene)) sahneBasina[d.scene] = new List<Duzeltme>();
            sahneBasina[d.scene].Add(d);
        }

        int degisen = 0, dokunulmayan = 0, bulunamayan = 0;

        foreach (var kv in sahneBasina)
        {
            Scene scene = EditorSceneManager.OpenScene(kv.Key, OpenSceneMode.Single);
            bool kirli = false;

            foreach (Duzeltme d in kv.Value)
            {
                TMP_Text tmp = Bul(scene, d.path);
                if (tmp == null)
                {
                    Debug.LogError($"{Tag}: obje yok -> {d.path}");
                    bulunamayan++;
                    continue;
                }

                if (Mathf.Approximately(tmp.fontSize, d.yeni))
                {
                    dokunulmayan++;
                    Debug.Log($"{Tag}: {d.path} zaten {d.yeni} punto -> dokunulmadı");
                    continue;
                }

                float onceki = tmp.fontSize;
                tmp.fontSize = d.yeni;
                EditorUtility.SetDirty(tmp);
                kirli = true;
                degisen++;
                Debug.Log($"{Tag}: {d.path} punto {onceki} -> {d.yeni}  ({d.gerekce})");
            }

            if (kirli) EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"{Tag}: değişen {degisen}, dokunulmayan {dokunulmayan}, bulunamayan {bulunamayan}");
        Debug.Log($"{Tag}: {(bulunamayan == 0 ? "OK" : "FAIL")}");
    }

    private static TMP_Text Bul(Scene scene, string path)
    {
        string[] parca = path.Split('/');
        Transform t = null;

        foreach (GameObject kok in scene.GetRootGameObjects())
        {
            if (kok.name != parca[0]) continue;
            t = kok.transform;
            for (int i = 1; i < parca.Length && t != null; i++) t = t.Find(parca[i]);
            break;
        }

        return t != null ? t.GetComponent<TMP_Text>() : null;
    }
}
