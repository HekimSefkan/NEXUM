using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Metin sözlüğü. Kullanıcıya görünen her metin buradan gelir.
///
/// Neden Unity Localization paketi değil? Paket AddressableAssets'i zorunlu
/// kılıyor, build boyutunu ve derleme süresini artırıyor ve tablo/locale
/// asset'leriyle sahne YAML'ını kabartıyor. NEXUM'un ihtiyacı iki dil ve
/// düz anahtar-değer eşlemesi; JSON + tek ScriptableObject bunu kat kat
/// daha az parçayla karşılıyor ve kaynak dosyalar git'te okunur/diff'lenebilir
/// kalıyor. Adressable'a ihtiyaç doğarsa geçiş yalnızca bu sınıfın içini
/// değiştirir.
///
/// Kendi assembly'sinde (Nexum.Localization): oyun kodu otomatik referans alır,
/// test derlemesi de referans ekleyerek yansımasız kullanabilir.
/// </summary>
public static class Loc
{
    public enum Language
    {
        Turkish = 0,
        English = 1,
    }

    private const string Tag = "NEXUM_LOC";

    /// <summary>Dil değiştiğinde tetiklenir; LocalizedText bileşenleri buna bağlanır.</summary>
    public static event Action LanguageChanged;

    private static Dictionary<string, LocalizationTable.Entry> table;
    private static readonly HashSet<string> reportedMissing = new HashSet<string>();
    private static Language current = Language.Turkish;
    private static bool initialized;

    public static Language Current
    {
        get
        {
            EnsureInitialized();
            return current;
        }
    }

    public static bool IsLoaded { get { return table != null; } }

    public static int KeyCount
    {
        get
        {
            EnsureInitialized();
            return table != null ? table.Count : 0;
        }
    }

    // ------------------------------------------------------------- kurulum

    private static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        LoadTable();
        current = ResolveStartupLanguage();
    }

    private static void LoadTable()
    {
        LocalizationTable asset = Resources.Load<LocalizationTable>(LocalizationTable.ResourcePath);
        if (asset == null)
        {
            Debug.LogError($"{Tag}: sözlük bulunamadı (Resources/{LocalizationTable.ResourcePath}); " +
                           "anahtarlar ham hâliyle gösterilecek");
            table = new Dictionary<string, LocalizationTable.Entry>();
            return;
        }

        table = asset.ToDictionary();
    }

    /// <summary>Kayıtta dil varsa onu, yoksa cihaz dilini kullanır (Türkçe → TR, diğer → EN).</summary>
    private static Language ResolveStartupLanguage()
    {
        int saved = SaveService.Data.language;
        if (saved >= 0) return (Language)Mathf.Clamp(saved, 0, 1);

        Language detected = Application.systemLanguage == SystemLanguage.Turkish
            ? Language.Turkish
            : Language.English;

        SaveService.Data.language = (int)detected;
        SaveService.MarkDirty();
        return detected;
    }

    /// <summary>Test ve Editor için: sözlüğü yeniden yükler.</summary>
    public static void Reload()
    {
        initialized = false;
        reportedMissing.Clear();
        EnsureInitialized();
        LanguageChanged?.Invoke();
    }

    // ----------------------------------------------------------------- dil

    /// <summary>Dili değiştirir; bağlı tüm metinler anında güncellenir.</summary>
    public static void SetLanguage(Language language)
    {
        EnsureInitialized();
        if (current == language) return;

        current = language;
        SaveService.Data.language = (int)language;
        SaveService.SaveNow();

        LanguageChanged?.Invoke();
    }

    // --------------------------------------------------------------- metin

    /// <summary>
    /// Anahtarın geçerli dildeki karşılığı. Anahtar yoksa ÇÖKMEZ: anahtarın
    /// kendisi köşeli parantez içinde döner ve bir kez uyarı loglanır.
    /// </summary>
    public static string Get(string key)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(key)) return "";

        if (table != null && table.TryGetValue(key, out LocalizationTable.Entry entry))
        {
            string value = current == Language.Turkish ? entry.turkish : entry.english;
            if (!string.IsNullOrEmpty(value)) return value;

            // İngilizcesi boşsa Türkçesine düş (Faz 6'ya kadar normal)
            if (!string.IsNullOrEmpty(entry.turkish)) return entry.turkish;
        }

        if (reportedMissing.Add(key))
        {
            Debug.LogWarning($"{Tag}: sözlükte yok -> {key}");
        }
        return "[" + key + "]";
    }

    /// <summary>Biçimlendirilmiş metin: <c>Get</c> sonucuna string.Format uygulanır.</summary>
    public static string Format(string key, params object[] args)
    {
        string pattern = Get(key);
        try
        {
            return string.Format(pattern, args);
        }
        catch (FormatException)
        {
            Debug.LogWarning($"{Tag}: biçim hatası -> {key}");
            return pattern;
        }
    }

    public static bool Has(string key)
    {
        EnsureInitialized();
        return table != null && table.ContainsKey(key);
    }

    public static IEnumerable<string> Keys
    {
        get
        {
            EnsureInitialized();
            return table != null ? table.Keys : new List<string>();
        }
    }
}
