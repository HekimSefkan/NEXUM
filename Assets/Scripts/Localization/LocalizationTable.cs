using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Çalışma zamanında okunan sözlük. Kaynak dosyalar
/// <c>Assets/Data/Localization/tr.json</c> ve <c>en.json</c>'dur; bu asset
/// onlardan <c>NexumLocalize</c> Editor scriptiyle DERLENİR.
///
/// Neden ara asset? Unity çalışma zamanında metin dosyasını yalnızca
/// <c>Resources</c> (ya da StreamingAssets) altından okuyabilir. JSON'ları
/// oraya koymak, projenin "Resources yalnızca kodla yüklenenleri tutar" kuralı
/// ile insan tarafından düzenlenen kaynak dosyaları karıştırırdı. Bu yüzden
/// kaynak JSON <c>Assets/Data</c> altında kalır, çalışma zamanı için tek bir
/// asset derlenir. İkisinin ayrışması validator tarafından denetlenir.
/// </summary>
public class LocalizationTable : ScriptableObject
{
    public const string ResourcePath = "Localization/LocalizationTable";

    [System.Serializable]
    public class Entry
    {
        public string key;
        public string turkish;
        public string english;
    }

    public List<Entry> entries = new List<Entry>();

    public Dictionary<string, Entry> ToDictionary()
    {
        Dictionary<string, Entry> map = new Dictionary<string, Entry>(entries.Count);
        foreach (Entry e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) continue;
            map[e.key] = e;
        }
        return map;
    }
}
