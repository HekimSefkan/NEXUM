#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batch mode doğrulama aracı. Build Settings'teki sahneleri ve Assets/Prefabs altındaki prefab'ları okur,
// sorunları loglar. Hiçbir sahneyi veya asset'i KAYDETMEZ.
// Kullanım: Unity.exe -batchmode -nographics -quit -projectPath <proje> -executeMethod NexumValidation.Run -logFile <log>
public static class NexumValidation
{
    private const string ResultTag = "NEXUM_VALIDATION";
    private const string IssueTag = "NEXUM_VALIDATION_ISSUE";

    public static void Run()
    {
        var problems = new List<string>();
        int sceneCount = 0, objectCount = 0, rectCount = 0, prefabCount = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!File.Exists(buildScene.path))
            {
                problems.Add($"Sahne dosyası bulunamadı: {buildScene.path}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            sceneCount++;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    objectCount++;
                    CheckGameObject(buildScene.path, t.gameObject, problems);
                    if (t is RectTransform rect)
                    {
                        rectCount++;
                        CheckRect(buildScene.path, rect, problems);
                    }
                }
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                problems.Add($"Prefab yüklenemedi: {path}");
                continue;
            }

            prefabCount++;
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                objectCount++;
                CheckGameObject(path, t.gameObject, problems);
                if (t is RectTransform rect)
                {
                    rectCount++;
                    CheckRect(path, rect, problems);
                }
            }
        }

        foreach (string problem in problems) Debug.Log($"{IssueTag}: {problem}");
        Debug.Log($"{ResultTag}_SUMMARY: sahne={sceneCount} prefab={prefabCount} obje={objectCount} recttransform={rectCount} sorun={problems.Count}");

        if (problems.Count == 0)
        {
            Debug.Log($"{ResultTag}: OK");
        }
        else
        {
            Debug.Log($"{ResultTag}: FAIL {string.Join(" | ", problems)}");
            EditorApplication.Exit(1);
        }
    }

    private static void CheckGameObject(string assetPath, GameObject go, List<string> problems)
    {
        string where = $"{assetPath} :: {HierarchyPath(go.transform)}";

        int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (missingScripts > 0) problems.Add($"Missing script ({missingScripts}) -> {where}");

        if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.IsPrefabAssetMissing(go))
            problems.Add($"Prefab asset'i eksik (missing prefab) -> {where}");

        foreach (Component component in go.GetComponents<Component>())
        {
            if (component == null) continue; // Missing script yukarıda sayıldı

            bool isProjectScript = IsProjectScript(component);
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;

            while (property.Next(enterChildren))
            {
                // String'lerin karakter dizisine girme
                enterChildren = property.propertyType != SerializedPropertyType.String;
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue != null) continue;

                if (property.objectReferenceInstanceIDValue != 0)
                    problems.Add($"Kopuk referans (Missing) {component.GetType().Name}.{property.propertyPath} -> {where}");
                else if (isProjectScript && property.type == "PPtr<$GameObject>")
                    problems.Add($"Null prefab/GameObject referansı {component.GetType().Name}.{property.propertyPath} -> {where}");
            }
        }
    }

    private static void CheckRect(string assetPath, RectTransform rect, List<string> problems)
    {
        var bad = new List<string>();
        if (IsBad(rect.anchorMin)) bad.Add("anchorMin");
        if (IsBad(rect.anchorMax)) bad.Add("anchorMax");
        if (IsBad(rect.anchoredPosition)) bad.Add("anchoredPosition");
        if (IsBad(rect.sizeDelta)) bad.Add("sizeDelta");
        if (IsBad(rect.pivot)) bad.Add("pivot");
        if (IsBad(rect.localPosition)) bad.Add("localPosition");
        if (IsBad(rect.localScale)) bad.Add("localScale");
        Quaternion r = rect.localRotation;
        if (IsBad(r.x) || IsBad(r.y) || IsBad(r.z) || IsBad(r.w)) bad.Add("localRotation");

        if (bad.Count > 0)
            problems.Add($"NaN/Infinity ({string.Join(", ", bad)}) -> {assetPath} :: {HierarchyPath(rect)}");
    }

    private static bool IsProjectScript(Component component)
    {
        if (!(component is MonoBehaviour behaviour)) return false;
        MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
        string path = script != null ? AssetDatabase.GetAssetPath(script) : "";
        return path.StartsWith("Assets/Scripts/");
    }

    private static bool IsBad(float v) => float.IsNaN(v) || float.IsInfinity(v);
    private static bool IsBad(Vector2 v) => IsBad(v.x) || IsBad(v.y);
    private static bool IsBad(Vector3 v) => IsBad(v.x) || IsBad(v.y) || IsBad(v.z);

    private static string HierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
