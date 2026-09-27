#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
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

    // Resources altında kalmasına izin verilen (kod tarafından yüklenen) girdiler
    private static readonly HashSet<string> AllowedResources = new HashSet<string>
    {
        "Elements",               // EncyclopediaManager: Resources.LoadAll<ElementData>("Elements")
        "DOTweenSettings.asset",  // DOTween kendi yükler
    };

    // =========================================

    private const string AtlasFolder = "Assets/Art/Atlases";

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

            CheckLevelSolvability(buildScene.path, problems, warnings);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    objectCount++;
                    eventCallCount += CheckGameObject(buildScene.path, t.gameObject, problems, warnings);
                    RecordSpriteUsage(t.gameObject);
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
                RecordSpriteUsage(t.gameObject);
                if (t is RectTransform rect)
                {
                    rectCount++;
                    CheckRect(path, rect, problems);
                }
            }
        }

        CheckProjectSettings(problems);
        CheckElementData(warnings);
        CheckAudioImport(warnings);
        CheckResourcesFolder(warnings);
        CheckTextureRules(warnings);
        CheckAndroidIcons(warnings);

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

        // Sahnede kaydırma konumu sıfırlanmamışsa panel ortadan açılır (çalışma zamanı sıfırlaması ayrı)
        foreach (ScrollRect scroll in go.GetComponents<ScrollRect>())
        {
            if (scroll.content != null && scroll.content.anchoredPosition != Vector2.zero)
                warnings.Add($"ScrollRect içeriği sahnede kaydırılmış durumda ({scroll.content.anchoredPosition}) -> {where}");
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

            // (ScrollRect konum kontrolü CheckGameObject içinde)
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

    // Ansiklopedi verisi: boş ya da tek karakterlik metin alanları (ör. Fe2O3 için "c")
    // Her bölümün hedef bileşiği, o bölümün spawn havuzundan birleşme grafiğiyle üretilebiliyor mu?
    // Üretilemiyorsa bölüm çözülemez demektir (FAIL). Yinelenen tarif uyarı verir.
    private static void CheckLevelSolvability(string scenePath, List<string> problems, List<string> warnings)
    {
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>(true);
        LevelManager levelManager = UnityEngine.Object.FindObjectOfType<LevelManager>(true);
        if (grid == null || levelManager == null) return;
        if (grid.recipes == null || levelManager.levels == null) return;

        // GridManager.BuildDictionary ile aynı davranış: aynı anahtardan ilk tarif geçerli
        var active = new List<MergeRecipe>();
        var seen = new HashSet<string>();
        foreach (MergeRecipe recipe in grid.recipes)
        {
            if (recipe.element1 == null || recipe.element2 == null || recipe.resultPrefab == null)
            {
                problems.Add($"{scenePath}: GridManager tarif tablosunda boş alan var");
                continue;
            }

            string key = MergeKey(recipe.element1.name, recipe.element2.name);
            if (seen.Add(key)) active.Add(recipe);
            else warnings.Add($"{scenePath}: yinelenen tarif ({recipe.element1.name} + {recipe.element2.name} -> {recipe.resultPrefab.name}), ilk tanım geçerli");
        }

        for (int i = 0; i < levelManager.levels.Count; i++)
        {
            LevelData level = levelManager.levels[i];
            string levelName = string.IsNullOrEmpty(level.levelName) ? $"Level {i + 1}" : level.levelName;

            var reachable = new HashSet<string>();
            if (level.spawnPool != null)
            {
                foreach (SpawnElement spawn in level.spawnPool)
                {
                    if (spawn.elementPrefab != null && spawn.spawnWeight > 0) reachable.Add(spawn.elementPrefab.name);
                }
            }

            // Üretilebilir bileşikler kümesi büyümeyi bırakana kadar tarifleri uygula
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (MergeRecipe recipe in active)
                {
                    if (reachable.Contains(recipe.element1.name) && reachable.Contains(recipe.element2.name)
                        && reachable.Add(recipe.resultPrefab.name)) grew = true;
                }
            }

            if (level.levelGoals == null || level.levelGoals.Count == 0)
            {
                problems.Add($"{scenePath}: {levelName} bölümünün hedefi yok");
                continue;
            }

            foreach (LevelGoal goal in level.levelGoals)
            {
                if (goal.targetPrefab == null)
                {
                    problems.Add($"{scenePath}: {levelName} bölümünde boş hedef var");
                    continue;
                }

                if (!reachable.Contains(goal.targetPrefab.name))
                {
                    problems.Add($"{scenePath}: {levelName} hedefi {goal.targetPrefab.name}, bu bölümün spawn havuzuyla üretilemiyor");
                }
            }
        }

        Debug.Log($"{ResultTag}_LEVELS: {levelManager.levels.Count} bölüm, {active.Count} etkin tarif kontrol edildi");
    }

    // GridManager.GetMergeKey ile aynı sıralama
    private static string MergeKey(string name1, string name2)
    {
        string a = name1.Replace("(Clone)", "");
        string b = name2.Replace("(Clone)", "");
        return string.Compare(a, b) < 0 ? a + "_" + b : b + "_" + a;
    }

    // ===== EH: doku / yayın ayarı kontrolleri =====

    // Çalışma zamanında atanan sprite'ların ekrandaki gerçek boyutu (sahnedeki Image'a bakmak yanıltır).
    // Ölçüm: MainMenu ve Game sahnelerindeki hedef Image'ların RectTransform boyutları.
    private static readonly Dictionary<string, Vector2> RuntimeSpriteUsage = new Dictionary<string, Vector2>
    {
        { "Assets/Art/Avatars", new Vector2(663f, 724f) },                          // RegistrationManager.detailAvatar
        { "Assets/Art/Figures/SettingsPanel_toogleacık.png", new Vector2(130f, 60f) },
        { "Assets/Art/Figures/SettingsPanel_tooglekapalı.png", new Vector2(130f, 60f) },
        { "Assets/Art/Figures/level_selection_kilitlibutton.png", new Vector2(400f, 150f) },

        // Prefab kökleri boyutlarını yerleştirildikleri kaptan alır; asset olarak 0x0 görünürler
        { "Assets/Art/Figures/1.seviyetas.png", new Vector2(200f, 200f) },   // GridBoard hücresi
        { "Assets/Art/Figures/2.seviyetas.png", new Vector2(200f, 200f) },
        { "Assets/Art/Figures/3.seviyetas.png", new Vector2(200f, 200f) },
        { "Assets/Art/Figures/Cember.png", new Vector2(300f, 300f) },        // ElementCard (ansiklopedi ızgarası)
        { "Assets/Art/Figures/Beherglas.png", new Vector2(250f, 300f) },     // BeakerGoalPrefab
        { "Assets/Art/Figures/white-screen.png", new Vector2(95f, 151f) },   // beher içindeki sıvı dolgusu
    };

    // Deney föyü görselleri: 400 birimde çiziliyorlar, kural 1024 derdi ama 512'de
    // kalmaları bilinçli bir karar (APK boyutu). Bulanıklık uyarısından muaflar.
    private static readonly HashSet<string> BlurCheckExceptions = new HashSet<string>
    {
        "Assets/Art/Figures/CO2.png", "Assets/Art/Figures/CO2_formul.png", "Assets/Art/Figures/CaCO3.png",
        "Assets/Art/Figures/CaO.png", "Assets/Art/Figures/F2CO3.png", "Assets/Art/Figures/H2CO3_Mavi.png",
        "Assets/Art/Figures/H2CO3_Yeşil.png", "Assets/Art/Figures/H2O.png", "Assets/Art/Figures/NH3.png",
        "Assets/Art/Figures/NaCl.png", "Assets/Art/Figures/NaCl_Formul.png", "Assets/Art/Figures/Su damla.png",
        "Assets/Art/Figures/Su_Formul.png",
    };

    // Sahne/prefab taramasında toplanan en büyük Image boyutları (referans birimi, 1080x1920)
    private static readonly Dictionary<string, Vector2> spriteUsage = new Dictionary<string, Vector2>();

    private static void RecordSpriteUsage(GameObject go)
    {
        Image image = go.GetComponent<Image>();
        if (image == null || image.sprite == null) return;

        string path = AssetDatabase.GetAssetPath(image.sprite);
        if (string.IsNullOrEmpty(path)) return;

        RectTransform rect = go.transform as RectTransform;
        if (rect == null) return;

        Vector2 size = new Vector2(Mathf.Abs(rect.rect.width), Mathf.Abs(rect.rect.height));
        if (!spriteUsage.TryGetValue(path, out Vector2 best) || Mathf.Max(size.x, size.y) > Mathf.Max(best.x, best.y))
        {
            spriteUsage[path] = size;
        }
    }

    // Android ikon slotları: Adaptive / Round / Legacy türlerinden biri bile boşsa
    // uygulama mağazada ve launcher'da varsayılan ikonla görünür.
    private static void CheckAndroidIcons(List<string> warnings)
    {
        // Tür adları bu sürümde API numarası taşır ("Adaptive (API 26)"), önekle eşleşilir
        foreach (string kindName in new[] { "Adaptive", "Round", "Legacy" })
        {
            PlatformIconKind kind = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android)
                .FirstOrDefault(k => k.ToString().StartsWith(kindName, StringComparison.OrdinalIgnoreCase));
            if (kind == null)
            {
                warnings.Add($"ProjectSettings: Android {kindName} ikon türü bulunamadı");
                continue;
            }

            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            int empty = icons.Count(icon =>
            {
                Texture2D[] textures = icon.GetTextures();
                return textures == null || textures.Length == 0 || textures.All(t => t == null);
            });

            if (empty > 0)
            {
                warnings.Add($"ProjectSettings: Android {kindName} ikon slotlarının {empty}/{icons.Length} tanesi boş");
            }
        }
    }

    private static void CheckTextureRules(List<string> warnings)
    {
        // 1) Atlasa giren dokuda importer sıkıştırması açıksa çift sıkıştırma olur
        var atlased = new Dictionary<string, string>();
        if (Directory.Exists(AtlasFolder))
        {
            foreach (string atlasPath in Directory.GetFiles(AtlasFolder, "*.spriteatlasv2", SearchOption.AllDirectories))
            {
                string atlasName = Path.GetFileNameWithoutExtension(atlasPath);
                foreach (Match m in Regex.Matches(File.ReadAllText(atlasPath), "guid: ([0-9a-f]{32})"))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(m.Groups[1].Value);
                    if (!string.IsNullOrEmpty(assetPath)) atlased[assetPath] = atlasName;
                }
            }
        }

        foreach (KeyValuePair<string, string> pair in atlased)
        {
            TextureImporter importer = AssetImporter.GetAtPath(pair.Key) as TextureImporter;
            if (importer == null) continue;
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                warnings.Add($"{pair.Key}: {pair.Value} atlasına giriyor ama importer sıkıştırması açık (çift sıkıştırma)");
            }
        }

        // 2) Target API Level "Automatic" ise yayın öncesi sabitlenmeli
        if (PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto)
        {
            warnings.Add("ProjectSettings: Android Target API Level 'Automatic'; yayın öncesi sabit bir sürüme ayarlanmalı");
        }

        // 3) Etkin doku boyutu, ekrandaki en büyük kullanımın 1,5 katının altındaysa bulanıklaşır
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (BlurCheckExceptions.Contains(path)) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            Vector2 usage;
            if (!TryGetUsage(path, out usage)) continue;

            // DİKKAT: LoadAssetAtPath ile gelen Texture2D'nin width/height'ı İÇE AKTARILMIŞ
            // boyuttur (maxTextureSize uygulanmış hâli). Kaynak görselin gerçek boyutu
            // importer'dan alınmalı, yoksa "ayar mı kısıtlıyor" sorusu yanlış cevaplanır.
            int sourceWidth, sourceHeight;
            importer.GetSourceTextureWidthAndHeight(out sourceWidth, out sourceHeight);
            if (sourceWidth == 0 || sourceHeight == 0) continue;

            // Yalnızca DÜZELTİLEBİLİR durum uyarı verir: sınırı koyan maxTextureSize ayarı.
            // Kaynak görselin kendisi küçükse (ör. avatarlar 1024, arka planlar 941x1672)
            // import ayarıyla çözülmez, yeni görsel gerekir; bu her koşuda tekrarlayan
            // gürültü olurdu. Not: kapalı panellerde AspectRatioFitter'ın bıraktığı rect
            // değerleri bayat olabilir, bu yüzden ölçüt kaynak boyutuyla sınırlanıyor.
            int sourceMax = Mathf.Max(sourceWidth, sourceHeight);
            float needed = Mathf.Max(usage.x, usage.y) * 1.5f;

            if (importer.maxTextureSize < needed && importer.maxTextureSize < sourceMax)
            {
                warnings.Add($"{path}: maxTextureSize {importer.maxTextureSize}px, ekranda {Mathf.Max(usage.x, usage.y):F0} birim kullanılıyor (gereken ≥ {Mathf.Min(needed, sourceMax):F0}px) - bulanıklaşabilir");
            }
        }
    }

    private static bool TryGetUsage(string path, out Vector2 usage)
    {
        foreach (KeyValuePair<string, Vector2> pair in RuntimeSpriteUsage)
        {
            if (path == pair.Key || path.StartsWith(pair.Key + "/"))
            {
                usage = pair.Value;
                return true;
            }
        }
        return spriteUsage.TryGetValue(path, out usage);
    }

    private static void CheckElementData(List<string> warnings)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ElementData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ElementData data = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            if (data == null) continue;

            CheckText(warnings, path, "elementName", data.elementName, 2);
            CheckText(warnings, path, "symbol", data.symbol, 1);
            CheckText(warnings, path, "description", data.description, 10);
            // Temel elementlerin (puanı 0) sentez formülü doğal olarak boştur
            if (data.synthesisScore > 0) CheckText(warnings, path, "recipe", data.recipe, 3);
        }
    }

    private static void CheckText(List<string> warnings, string path, string field, string value, int minLength)
    {
        if (string.IsNullOrWhiteSpace(value)) warnings.Add($"ElementData.{field} boş -> {path}");
        else if (value.Trim().Length < minLength) warnings.Add($"ElementData.{field} çok kısa (\"{value}\") -> {path}");
    }

    // Ses import kuralı: >10 sn Streaming+Vorbis+arka planda yükleme, <3 sn Decompress+ADPCM+mono
    private static void CheckAudioImport(List<string> warnings)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Plugins") || path.StartsWith("Assets/TextMesh Pro")) continue;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (clip == null || importer == null) continue;

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            if (clip.length > 10f)
            {
                if (s.loadType != AudioClipLoadType.Streaming) warnings.Add($"Ses ({clip.length:F1} sn) Streaming olmalı: {path}");
                if (s.compressionFormat != AudioCompressionFormat.Vorbis) warnings.Add($"Ses ({clip.length:F1} sn) Vorbis olmalı: {path}");
                if (!importer.loadInBackground) warnings.Add($"Ses ({clip.length:F1} sn) loadInBackground açık olmalı: {path}");
            }
            else if (clip.length < 3f)
            {
                if (s.loadType != AudioClipLoadType.DecompressOnLoad) warnings.Add($"Ses ({clip.length:F1} sn) Decompress On Load olmalı: {path}");
                if (s.compressionFormat != AudioCompressionFormat.ADPCM) warnings.Add($"Ses ({clip.length:F1} sn) ADPCM olmalı: {path}");
                if (!importer.forceToMono) warnings.Add($"Ses ({clip.length:F1} sn) forceToMono açık olmalı: {path}");
            }
            else
            {
                if (s.loadType != AudioClipLoadType.CompressedInMemory) warnings.Add($"Ses ({clip.length:F1} sn) Compressed In Memory olmalı: {path}");
                if (s.compressionFormat != AudioCompressionFormat.Vorbis) warnings.Add($"Ses ({clip.length:F1} sn) Vorbis olmalı: {path}");
            }
        }
    }

    // ProjectSettings içindeki asset referansları (uygulama ikonu, splash vb.) çözülüyor mu?
    private static void CheckProjectSettings(List<string> problems)
    {
        foreach (string file in Directory.GetFiles("ProjectSettings", "*.asset"))
        {
            string text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, "guid: ([0-9a-f]{32})"))
            {
                string guid = m.Groups[1].Value;
                if (guid.Trim('0').Length == 0) continue;                       // boş referans
                if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))) continue;  // yerleşik kaynaklar dahil
                problems.Add($"ProjectSettings'te çözülemeyen asset referansı: {guid} -> {file}");
            }
        }
    }

    // Resources altındaki her şey build'e girer; kod tarafından yüklenmeyenler uyarı alır
    private static void CheckResourcesFolder(List<string> warnings)
    {
        const string root = "Assets/Resources";
        if (!Directory.Exists(root)) return;

        foreach (string entry in Directory.GetFileSystemEntries(root))
        {
            string name = Path.GetFileName(entry);
            if (name.EndsWith(".meta") || AllowedResources.Contains(name)) continue;
            warnings.Add($"Resources altında kod tarafından yüklenmeyen girdi (build'e giriyor): {root}/{name}");
        }
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
