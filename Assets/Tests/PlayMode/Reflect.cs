using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// Oyun kodu <c>Assembly-CSharp</c>'ta, testler kendi asmdef'inde. Önceden
/// derlenen derleme (predefined assembly) asmdef'leri referans alır, TERSİ
/// OLMAZ; bu yüzden test tarafı oyun tiplerine yansımayla erişir.
///
/// Ölçüm kodunun her testte tekrar yazılmaması için erişim tek yerde toplandı.
/// (Kalıcı veri <c>Nexum.Save</c>, sözlük <c>Nexum.Localization</c> asmdef'inde
/// olduğu için onlara doğrudan erişilir; burası yalnızca Assembly-CSharp için.)
/// </summary>
public static class Reflect
{
    private const BindingFlags Any =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static;

    /// <summary>Assembly-CSharp içindeki tip.</summary>
    public static Type Type(string name)
    {
        Type t = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.IsNotNull(t, "tip yok: " + name);
        return t;
    }

    // ------------------------------------------------------------------ static

    public static object Call(string typeName, string method, params object[] args)
    {
        Type t = Type(typeName);
        MethodInfo m = Find(t, method, args);
        Assert.IsNotNull(m, $"metot yok: {typeName}.{method}");
        return m.Invoke(null, args);
    }

    public static T Call<T>(string typeName, string method, params object[] args)
    {
        return (T)Call(typeName, method, args);
    }

    /// <summary>Static özellik ya da alan (ikisi de aynı kapıdan okunur).</summary>
    public static object Prop(string typeName, string name)
    {
        Type t = Type(typeName);

        PropertyInfo p = t.GetProperty(name, Any);
        if (p != null) return p.GetValue(null);

        FieldInfo f = t.GetField(name, Any);
        Assert.IsNotNull(f, $"alan/özellik yok: {typeName}.{name}");
        return f.GetValue(null);
    }

    public static T Prop<T>(string typeName, string name)
    {
        return (T)Prop(typeName, name);
    }

    public static void SetField(string typeName, string name, object value)
    {
        FieldInfo f = Type(typeName).GetField(name, Any);
        Assert.IsNotNull(f, $"alan yok: {typeName}.{name}");
        f.SetValue(null, value);
    }

    // ---------------------------------------------------------------- instance

    public static object Call(object target, string method, params object[] args)
    {
        MethodInfo m = Find(target.GetType(), method, args);
        Assert.IsNotNull(m, $"metot yok: {target.GetType().Name}.{method}");
        return m.Invoke(target, args);
    }

    public static T Call<T>(object target, string method, params object[] args)
    {
        return (T)Call(target, method, args);
    }

    public static object Get(object target, string name)
    {
        Type t = target.GetType();

        PropertyInfo p = t.GetProperty(name, Any);
        if (p != null) return p.GetValue(target);

        FieldInfo f = t.GetField(name, Any);
        Assert.IsNotNull(f, $"alan/özellik yok: {t.Name}.{name}");
        return f.GetValue(target);
    }

    public static T Get<T>(object target, string name)
    {
        return (T)Get(target, name);
    }

    public static void Set(object target, string name, object value)
    {
        Type t = target.GetType();

        PropertyInfo p = t.GetProperty(name, Any);
        if (p != null && p.CanWrite) { p.SetValue(target, value); return; }

        FieldInfo f = t.GetField(name, Any);
        Assert.IsNotNull(f, $"alan yok: {t.Name}.{name}");
        f.SetValue(target, value);
    }

    // --------------------------------------------------------------- yardımcı

    /// <summary>Aşırı yüklenmiş metotlarda parametre sayısına göre seçer.</summary>
    private static MethodInfo Find(Type type, string name, object[] args)
    {
        MethodInfo[] hepsi = type.GetMethods(Any);
        MethodInfo tek = null;
        int bulunan = 0;

        foreach (MethodInfo m in hepsi)
        {
            if (m.Name != name || m.GetParameters().Length != args.Length) continue;
            if (Uyumlu(m, args)) return m;
            tek = m;
            bulunan++;
        }

        return bulunan == 1 ? tek : null;
    }

    private static bool Uyumlu(MethodInfo m, object[] args)
    {
        ParameterInfo[] ps = m.GetParameters();
        for (int i = 0; i < ps.Length; i++)
        {
            if (args[i] == null) continue;
            if (!ps[i].ParameterType.IsInstanceOfType(args[i])) return false;
        }
        return true;
    }

    // ------------------------------------------------------- Editor derlemesi

    /// <summary>
    /// <c>NexumColorTokens.Tablo</c> -> { HEX : rol }.
    ///
    /// Tablo Assembly-CSharp-Editor icinde; test asmdef'i onu referans
    /// ALMAZ (ve alamaz). Bu yuzden tip adla cozulur.
    /// </summary>
    public static Dictionary<string, string> ColorTokenTable()
    {
        Type t = System.Type.GetType("NexumColorTokens, Assembly-CSharp-Editor");
        if (t == null) return null;

        FieldInfo f = t.GetField("Tablo", Any);
        if (f == null) return null;

        var dizi = f.GetValue(null) as System.Collections.IEnumerable;
        if (dizi == null) return null;

        Dictionary<string, string> sonuc = new Dictionary<string, string>();
        foreach (object kural in dizi)
        {
            Type kt = kural.GetType();
            string hex = (string)kt.GetField("hex", Any).GetValue(kural);
            string role = (string)kt.GetField("role", Any).GetValue(kural);
            if (!string.IsNullOrEmpty(hex)) sonuc[hex.ToUpperInvariant()] = role;
        }
        return sonuc;
    }

    /// <summary>NexumTileVisuals sabitleri (Assembly-CSharp-Editor).</summary>
    private static T EditorConst<T>(string alan, T varsayilan)
    {
        Type t = System.Type.GetType("NexumTileVisuals, Assembly-CSharp-Editor");
        if (t == null) return varsayilan;
        FieldInfo f = t.GetField(alan, Any);
        return f == null ? varsayilan : (T)f.GetValue(null);
    }

    public static float TileContentRatio() { return EditorConst("ContentRatio", 0.80f); }

    public static float TileMinFont() { return EditorConst("MinFont", 32f); }

    public static float TileMaxFont() { return EditorConst("MaxFont", 72f); }
}
