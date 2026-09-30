using System;
using System.Collections.Generic;

/// <summary>
/// <c>Assets/Data/Localization/tr.json</c> ve <c>en.json</c> dosyalarının biçimi.
///
/// Sözlük, JsonUtility'nin okuyabilmesi için sözlük değil LİSTE olarak tutulur;
/// böylece ek bir JSON ayrıştırıcısına gerek kalmaz ve dosya git'te satır satır
/// diff'lenebilir kalır.
///
/// <c>untranslated</c>: henüz çevrilmemiş, yani Türkçe metni taşıyan anahtarlar.
/// Faz 6'da bir anahtar çevrildikçe bu listeden çıkarılır.
/// </summary>
[Serializable]
public class LocFile
{
    [Serializable]
    public class Pair
    {
        public string key;
        public string value;

        public Pair() { }
        public Pair(string k, string v) { key = k; value = v; }
    }

    public string language = "tr";
    public bool translated = true;
    public List<Pair> entries = new List<Pair>();
    public List<string> untranslated = new List<string>();

    public Dictionary<string, string> ToMap()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        foreach (Pair p in entries)
        {
            if (p == null || string.IsNullOrEmpty(p.key)) continue;
            map[p.key] = p.value ?? "";
        }
        return map;
    }
}
