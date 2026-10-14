using System;
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

    public static object Prop(string typeName, string name)
    {
        PropertyInfo p = Type(typeName).GetProperty(name, Any);
        Assert.IsNotNull(p, $"özellik yok: {typeName}.{name}");
        return p.GetValue(null);
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
}
