using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahneye obje ve bileşen ekleme işinin tek çatısı. Elle YAML düzenlemek
/// yerine kurucular bu API'yi kullanır.
///
/// Garantiler:
///  - IDEMPOTENT: obje ADLA bulunur, yoksa oluşturulur. İkinci çalıştırmada
///    hiçbir değer değişmez, sahne kaydedilmez, <c>git diff</c> boş kalır.
///  - İMZA: kurucunun yönettiği her yol <c>managed.json</c> kaydına yazılır.
///    Böylece hangi objenin script tarafından yönetildiği sahneyi kirletmeden
///    (marker bileşeni eklemeden) bellidir.
///  - GÜVENLİK: kayıtta olmayan ama sahnede var olan bir ada rastlanırsa kurucu
///    DURUR ve hata verir; elle yapılmış işi sessizce sahiplenmez. Bilerek
///    devralmak için <c>Ensure(..., adopt: true)</c> gerekir.
///  - DRY-RUN: <c>dryRun</c> açıkken hiçbir şey değiştirilmez, yalnızca ne
///    yapılacağı raporlanır.
///
/// Örnek:
/// <code>
/// var b = UIBuilder.Begin("SettingsCard", "Assets/Scenes/MainMenu.unity", dryRun);
/// GameObject card = b.Ensure("Canvas/SafeAreaRoot/SettingsPanel/GlassBackground",
///                            "CardContent", adopt: true);
/// b.SetStretch(card, Vector2.zero, Vector2.one);
/// b.EnsureComponent&lt;AspectRatioFitter&gt;(card, f =&gt; {
///     f.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
///     f.aspectRatio = UITokens.CardAspect;
/// });
/// b.End();
/// </code>
/// </summary>
public class UIBuilder
{
    public const string Tag = "NEXUM_UIBUILDER";
    private const string RegistryPath = "Assets/Editor/UIBuilder/managed.json";

    // ------------------------------------------------------------- kayıt defteri

    [Serializable]
    private class Registry
    {
        [Serializable]
        public class Entry
        {
            public string path;      // sahne yolu: <sahne>::Canvas/.../Obje
            public string builder;   // yöneten kurucunun adı
        }

        public List<Entry> managed = new List<Entry>();
    }

    private static Registry LoadRegistry()
    {
        if (!File.Exists(RegistryPath)) return new Registry();
        try
        {
            return JsonUtility.FromJson<Registry>(File.ReadAllText(RegistryPath)) ?? new Registry();
        }
        catch (Exception e)
        {
            Debug.LogError($"{Tag}: kayıt defteri okunamadı ({e.Message}); boş kabul edildi");
            return new Registry();
        }
    }

    private static void SaveRegistry(Registry registry)
    {
        registry.managed = registry.managed
            .GroupBy(e => e.path)
            .Select(g => g.First())
            .OrderBy(e => e.path, StringComparer.Ordinal)
            .ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(RegistryPath));
        File.WriteAllText(RegistryPath, JsonUtility.ToJson(registry, true));
        AssetDatabase.ImportAsset(RegistryPath);
    }

    // ------------------------------------------------------------------ durum

    private readonly string builderName;
    private readonly string scenePath;
    private readonly bool dryRun;
    private readonly Registry registry;
    private readonly Scene scene;

    private readonly List<string> created = new List<string>();
    private readonly List<string> updated = new List<string>();
    private readonly List<string> untouched = new List<string>();
    private readonly List<string> errors = new List<string>();

    public bool HasErrors { get { return errors.Count > 0; } }
    public int CreatedCount { get { return created.Count; } }
    public int UpdatedCount { get { return updated.Count; } }

    private UIBuilder(string name, string path, bool isDryRun)
    {
        builderName = name;
        scenePath = path;
        dryRun = isDryRun;
        registry = LoadRegistry();
        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    /// <summary>Kurucuyu başlatır ve hedef sahneyi açar.</summary>
    public static UIBuilder Begin(string builderName, string scenePath, bool dryRun = false)
    {
        return new UIBuilder(builderName, scenePath, dryRun);
    }

    /// <summary>Komut satırında <c>-dryRun</c> var mı?</summary>
    public static bool DryRunRequested
    {
        get { return Environment.GetCommandLineArgs().Any(a => a == "-dryRun"); }
    }

    // ----------------------------------------------------------------- objeler

    private string Key(string objectPath)
    {
        return Path.GetFileNameWithoutExtension(scenePath) + "::" + objectPath;
    }

    private bool IsManaged(string objectPath)
    {
        string key = Key(objectPath);
        return registry.managed.Any(e => e.path == key);
    }

    private void Claim(string objectPath)
    {
        string key = Key(objectPath);
        if (registry.managed.Any(e => e.path == key)) return;
        registry.managed.Add(new Registry.Entry { path = key, builder = builderName });
    }

    /// <summary>
    /// Verilen ebeveyn altında adı geçen objeyi bulur; yoksa oluşturur.
    /// Obje var ama kurucu kaydında yoksa: <paramref name="adopt"/> true değilse
    /// HATA verir (elle yapılmış obje sessizce sahiplenilmez).
    /// </summary>
    public GameObject Ensure(string parentPath, string name, bool adopt = false)
    {
        Transform parent = Find(parentPath);
        if (parent == null)
        {
            Error($"ebeveyn bulunamadı: {parentPath}");
            return null;
        }

        string objectPath = parentPath + "/" + name;
        Transform existing = parent.Find(name);

        if (existing != null)
        {
            if (!IsManaged(objectPath))
            {
                if (!adopt)
                {
                    Error($"isim çakışması: {objectPath} sahnede var ama kurucu kaydında yok " +
                          "(elle yapılmışsa adını değiştirin ya da Ensure(..., adopt: true) deyin)");
                    return null;
                }
                Claim(objectPath);
                updated.Add($"{objectPath} (devralındı)");
                return existing.gameObject;
            }

            untouched.Add(objectPath);
            return existing.gameObject;
        }

        created.Add(objectPath);
        Claim(objectPath);

        if (dryRun) return null;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Objeyi başka bir ebeveynin altına taşır (zaten oradaysa dokunmaz).</summary>
    public void Reparent(GameObject go, GameObject newParent)
    {
        if (go == null || newParent == null) return;
        if (go.transform.parent == newParent.transform)
        {
            untouched.Add(HierarchyPath(go.transform) + " (ebeveyn)");
            return;
        }

        updated.Add(HierarchyPath(go.transform) + " -> " + HierarchyPath(newParent.transform));
        if (!dryRun) go.transform.SetParent(newParent.transform, false);
    }

    // -------------------------------------------------------------- bileşenler

    /// <summary>Bileşen yoksa ekler, varsa bulur; ardından <paramref name="configure"/> uygular.</summary>
    public T EnsureComponent<T>(GameObject go, Action<T> configure = null) where T : Component
    {
        if (go == null) return null;

        T component = go.GetComponent<T>();
        if (component == null)
        {
            created.Add($"{HierarchyPath(go.transform)} + {typeof(T).Name}");
            if (dryRun) return null;
            component = go.AddComponent<T>();
        }

        if (configure != null) Apply(go, component, configure);
        return component;
    }

    /// <summary>
    /// Bileşeni yapılandırır. Dry-run'da DEĞER YAZILMAZ: kopya üzerinde denenir
    /// ve yalnızca fark olup olmadığı raporlanır.
    /// </summary>
    public void Apply<T>(GameObject go, T component, Action<T> configure) where T : Component
    {
        if (component == null || configure == null) return;

        string before = Snapshot(component);
        configure(component);
        string after = Snapshot(component);

        string label = $"{HierarchyPath(go.transform)} :: {typeof(T).Name}";
        if (before == after)
        {
            untouched.Add(label);
            return;
        }

        updated.Add(label);
        if (dryRun)
        {
            // Değişikliği geri al: dry-run hiçbir şeyi değiştirmemeli
            JsonUtility.FromJsonOverwrite(before, component);
        }
        else
        {
            EditorUtility.SetDirty(component);
        }
    }

    private static string Snapshot(Component component)
    {
        try
        {
            return JsonUtility.ToJson(component);
        }
        catch
        {
            return Guid.NewGuid().ToString();   // karşılaştırılamıyorsa "değişti" say
        }
    }

    // ---------------------------------------------------------- RectTransform

    /// <summary>Ebeveyni tamamen dolduran (stretch) yerleşim.</summary>
    public void SetStretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        SetRect(go, anchorMin, anchorMax, UITokens.Center, Vector2.zero, Vector2.zero);
    }

    /// <summary>Oransal band: anchor'lar yüzde, offset'ler sıfır.</summary>
    public void SetBand(GameObject go, float x0, float x1, float y0, float y1)
    {
        SetRect(go, new Vector2(x0, y0), new Vector2(x1, y1), UITokens.Center, Vector2.zero, Vector2.zero);
    }

    /// <summary>Nokta anchor + sabit boyut.</summary>
    public void SetFixed(GameObject go, Vector2 anchor, Vector2 size)
    {
        if (go == null) return;
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) return;

        ApplyRect(go, rect, anchor, anchor, UITokens.Center, size, Vector2.zero, true);
    }

    public void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (go == null) return;
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) return;

        ApplyRect(go, rect, anchorMin, anchorMax, pivot, offsetMin, offsetMax, false);
    }

    private void ApplyRect(GameObject go, RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
                           Vector2 pivot, Vector2 a, Vector2 b, bool fixedSize)
    {
        bool changed = false;

        if (rect.anchorMin != anchorMin) changed = true;
        if (rect.anchorMax != anchorMax) changed = true;
        if (rect.pivot != pivot) changed = true;
        if (rect.localScale != Vector3.one) changed = true;

        if (fixedSize)
        {
            if (rect.sizeDelta != a) changed = true;
            if (rect.anchoredPosition != b) changed = true;
        }
        else
        {
            if (rect.offsetMin != a) changed = true;
            if (rect.offsetMax != b) changed = true;
            if (rect.anchoredPosition != Vector2.zero) changed = true;
        }

        string label = HierarchyPath(rect) + " :: RectTransform";
        if (!changed)
        {
            untouched.Add(label);
            return;
        }

        updated.Add(label);
        if (dryRun) return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.localScale = Vector3.one;

        if (fixedSize)
        {
            rect.sizeDelta = a;
            rect.anchoredPosition = b;
        }
        else
        {
            rect.offsetMin = a;
            rect.offsetMax = b;
            rect.anchoredPosition = Vector2.zero;
        }

        EditorUtility.SetDirty(rect);
    }

    // ------------------------------------------------------------------ bitiş

    public void Error(string message)
    {
        errors.Add(message);
        Debug.LogError($"{Tag}[{builderName}]: {message}");
    }

    /// <summary>Kaydeder (dry-run değilse ve değişiklik varsa) ve raporu basar.</summary>
    public bool End()
    {
        bool changed = created.Count > 0 || updated.Count > 0;

        if (!dryRun && changed && errors.Count == 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SaveRegistry(registry);
            AssetDatabase.SaveAssets();
        }

        string mode = dryRun ? "DRY-RUN" : "UYGULANDI";
        Debug.Log($"{Tag}[{builderName}] {mode}: oluşturulan={created.Count} " +
                  $"güncellenen={updated.Count} dokunulmayan={untouched.Count} hata={errors.Count}");

        foreach (string s in created) Debug.Log($"{Tag}_OLUSTURULDU: {s}");
        foreach (string s in updated) Debug.Log($"{Tag}_GUNCELLENDI: {s}");
        foreach (string s in errors) Debug.Log($"{Tag}_HATA: {s}");

        return errors.Count == 0;
    }

    // ------------------------------------------------------------- yardımcılar

    private Transform Find(string path)
    {
        string[] parts = path.Split('/');
        Transform current = scene.GetRootGameObjects()
            .Select(g => g.transform)
            .FirstOrDefault(t => t.name == parts[0]);

        for (int i = 1; i < parts.Length && current != null; i++)
        {
            current = current.Find(parts[i]);
        }
        return current;
    }

    public GameObject FindObject(string path)
    {
        Transform t = Find(path);
        return t != null ? t.gameObject : null;
    }

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
