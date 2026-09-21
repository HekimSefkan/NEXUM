#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Batch mode doğrulama aracı. Build Settings'teki sahneleri ve Assets/Prefabs altındaki prefab'ları okur,
// sorunları loglar. Hiçbir sahneyi veya asset'i KAYDETMEZ.
// Kullanım: Unity.exe -batchmode -nographics -quit -projectPath <proje> -executeMethod NexumValidation.Run -logFile <log>
public static class NexumValidation
{
    // ===== Kolay düzenlenebilir ayarlar =====

    // Bilerek boş (None) bırakılmış GameObject alanları. Biçim: "ComponentTipi.alanYolu"
    private static readonly HashSet<string> AllowedNullFields = new HashSet<string>
    {
        "GridManager.mergeParticlePrefab", // Overlay canvas'ta particle görünmediği için efekt kapalı
    };

    // Sahne yükleyen metotlar. Aynı olayda birden fazlası varsa uyarı verilir. Biçim: "Tip.Metot"
    private static readonly HashSet<string> SceneLoadMethods = new HashSet<string>
    {
        "MainMenuManager.LoadLevel",
        "LevelMenuManager.SelectLevelAndPlay",
        "UIManager.RestartGame",
        "UIManager.GoToMainMenu",
        "UIManager.NextLevel",
    };

    // =========================================

    private const string ResultTag = "NEXUM_VALIDATION";
    private const string IssueTag = "NEXUM_VALIDATION_ISSUE";
    private const string WarnTag = "NEXUM_VALIDATION_WARN";

    public static void Run()
    {
        var problems = new List<string>();
        var warnings = new List<string>();
        int sceneCount = 0, objectCount = 0, rectCount = 0, prefabCount = 0, eventCallCount = 0;

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
                    eventCallCount += CheckGameObject(buildScene.path, t.gameObject, problems, warnings);
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
                eventCallCount += CheckGameObject(path, t.gameObject, problems, warnings);
                if (t is RectTransform rect)
                {
                    rectCount++;
                    CheckRect(path, rect, problems);
                }
            }
        }

        foreach (string problem in problems) Debug.Log($"{IssueTag}: {problem}");
        foreach (string warning in warnings) Debug.Log($"{WarnTag}: {warning}");
        Debug.Log($"{ResultTag}_SUMMARY: sahne={sceneCount} prefab={prefabCount} obje={objectCount} recttransform={rectCount} olay_cagrisi={eventCallCount} sorun={problems.Count} uyari={warnings.Count}");
        Debug.Log($"{ResultTag}_WARNINGS: {warnings.Count}{(warnings.Count > 0 ? " " + string.Join(" | ", warnings) : "")}");

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

    // Kontrol edilen kalıcı UnityEvent çağrısı sayısını döndürür
    private static int CheckGameObject(string assetPath, GameObject go, List<string> problems, List<string> warnings)
    {
        string where = $"{assetPath} :: {HierarchyPath(go.transform)}";
        int eventCalls = 0;

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

                if (property.name == "m_PersistentCalls")
                {
                    eventCalls += CheckUnityEvent(component, property.Copy(), where, problems, warnings);
                    enterChildren = false; // Çağrı hedefleri CheckUnityEvent içinde kontrol edildi
                    continue;
                }

                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue != null) continue;

                string fieldKey = $"{component.GetType().Name}.{property.propertyPath}";
                if (property.objectReferenceInstanceIDValue != 0)
                    problems.Add($"Kopuk referans (Missing) {fieldKey} -> {where}");
                else if (isProjectScript && property.type == "PPtr<$GameObject>" && !AllowedNullFields.Contains(fieldKey))
                    problems.Add($"Null prefab/GameObject referansı {fieldKey} -> {where}");
            }
        }

        return eventCalls;
    }

    private static int CheckUnityEvent(Component component, SerializedProperty persistentCalls, string where, List<string> problems, List<string> warnings)
    {
        string eventPath = persistentCalls.propertyPath.Replace(".m_PersistentCalls", "");
        SerializedProperty calls = persistentCalls.FindPropertyRelative("m_Calls");
        if (calls == null || !calls.isArray) return 0;

        int sceneLoadCount = 0;
        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            SerializedProperty targetProperty = call.FindPropertyRelative("m_Target");
            string methodName = call.FindPropertyRelative("m_MethodName").stringValue;
            string declaredType = call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue.Split(',')[0];
            var mode = (PersistentListenerMode)call.FindPropertyRelative("m_Mode").intValue;
            UnityEngine.Object target = targetProperty.objectReferenceValue;
            string label = $"{component.GetType().Name}.{eventPath}[{i}] -> {(target != null ? target.GetType().Name : declaredType)}.{methodName}";

            if (target == null)
            {
                string kind = targetProperty.objectReferenceInstanceIDValue != 0 ? "Missing" : "null";
                problems.Add($"UnityEvent hedefi {kind}: {label} -> {where}");
                continue;
            }

            if (string.IsNullOrEmpty(methodName) || !MethodExists(target, methodName, mode, call))
                problems.Add($"UnityEvent metodu hedef tipte bulunamadı ({mode}): {label} -> {where}");

            string typeMethod = $"{target.GetType().Name}.{methodName}";
            if (SceneLoadMethods.Contains(typeMethod) || methodName.Contains("LoadScene")) sceneLoadCount++;

            if (component is ScrollRect && eventPath == "m_OnValueChanged" && IsSoundCall(target, methodName))
                warnings.Add($"ScrollRect.onValueChanged ses çağırıyor (kaydırırken her karede çalar): {label} -> {where}");

            if (target is MonoBehaviour behaviour && IsDestroyableSingletonCopy(behaviour))
                warnings.Add($"Singleton hedefleniyor; sahne yeniden yüklenince bu kopya yok edilir ve çağrı sessizce düşer: {label} -> {where}");
        }

        if (sceneLoadCount > 1)
            warnings.Add($"Aynı olayda {sceneLoadCount} sahne yükleme çağrısı: {component.GetType().Name}.{eventPath} -> {where}");

        return calls.arraySize;
    }

    private static bool MethodExists(UnityEngine.Object target, string methodName, PersistentListenerMode mode, SerializedProperty call)
    {
        if (mode == PersistentListenerMode.EventDefined)
        {
            // Dinamik parametre: olayın tipine göre değişir, isimle eşleşme yeterli
            return target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(m => m.Name == methodName);
        }

        Type[] argumentTypes;
        switch (mode)
        {
            case PersistentListenerMode.Void: argumentTypes = Type.EmptyTypes; break;
            case PersistentListenerMode.Int: argumentTypes = new[] { typeof(int) }; break;
            case PersistentListenerMode.Float: argumentTypes = new[] { typeof(float) }; break;
            case PersistentListenerMode.String: argumentTypes = new[] { typeof(string) }; break;
            case PersistentListenerMode.Bool: argumentTypes = new[] { typeof(bool) }; break;
            case PersistentListenerMode.Object:
                string typeName = call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue;
                argumentTypes = new[] { Type.GetType(typeName, false) ?? typeof(UnityEngine.Object) };
                break;
            default: return false;
        }

        return UnityEventBase.GetValidMethodInfo(target, methodName, argumentTypes) != null;
    }

    private static bool IsSoundCall(UnityEngine.Object target, string methodName)
    {
        return target.GetType().Name.Contains("Audio")
               || methodName.IndexOf("Sound", StringComparison.OrdinalIgnoreCase) >= 0
               || methodName.IndexOf("SFX", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Sahneler arası kalıcı singleton (kendi tipinden static Instance + script içinde DontDestroyOnLoad) olup
    // proxy desteği (IsProxy üyesi) olmayan tipler: sahne yeniden yüklenince kopyası yok edilir.
    // Sahneye bağlı singleton'lar (UIManager, GridManager gibi) sahneyle birlikte yeniden oluştuğu için uyarı almaz.
    private static bool IsDestroyableSingletonCopy(MonoBehaviour behaviour)
    {
        Type type = behaviour.GetType();
        const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        bool hasInstance = type.GetField("Instance", staticFlags)?.FieldType == type
                           || type.GetProperty("Instance", staticFlags)?.PropertyType == type;
        if (!hasInstance) return false;

        MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
        if (script == null || !script.text.Contains("DontDestroyOnLoad")) return false;

        const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        bool supportsProxy = type.GetProperty("IsProxy", instanceFlags) != null || type.GetField("IsProxy", instanceFlags) != null;
        return !supportsProxy;
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
