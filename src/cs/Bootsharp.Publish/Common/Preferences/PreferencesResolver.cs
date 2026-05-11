using System.Reflection;

namespace Bootsharp.Publish;

internal static class PreferencesResolver
{
    /// <summary>
    /// Resolved preferences of the current build task.
    /// </summary>
    internal static AsyncLocal<Preferences> Resolved { get; } = new();

    public static void Resolve (string entryAssemblyName, string outDir)
    {
        using var ctx = CreateLoadContext(outDir);
        var assemblyPath = Path.Combine(outDir, entryAssemblyName);
        var assembly = ctx.LoadFromAssemblyPath(assemblyPath);
        var attribute = FindPreferencesAttribute(assembly);
        Resolved.Value = CreatePreferences(attribute);
    }

    private static CustomAttributeData? FindPreferencesAttribute (Assembly assembly)
    {
        foreach (var attr in assembly.CustomAttributes)
            if (attr.AttributeType.FullName == typeof(PreferencesAttribute).FullName)
                return attr;
        return null;
    }

    private static Preferences CreatePreferences (CustomAttributeData? attr) => new() {
        Space = CreatePreferences(nameof(PreferencesAttribute.Space), attr) ?? [],
        Name = CreatePreferences(nameof(PreferencesAttribute.Name), attr) ?? [],
        Method = CreatePreferences(nameof(PreferencesAttribute.Method), attr) ?? [],
        Property = CreatePreferences(nameof(PreferencesAttribute.Property), attr) ?? [],
        Event = CreatePreferences(nameof(PreferencesAttribute.Event), attr) ?? []
    };

    private static Preference[]? CreatePreferences (string name, CustomAttributeData? attr)
    {
        if (attr is null || !attr.NamedArguments.Any(a => a.MemberName == name)) return null;
        var value = CreateValue(attr.NamedArguments.First(a => a.MemberName == name).TypedValue);
        var prefs = new Preference[value.Length / 2];
        for (int i = 0; i < prefs.Length; i++)
            prefs[i] = new(value[i * 2], value[(i * 2) + 1]);
        return prefs;
    }

    private static string[] CreateValue (CustomAttributeTypedArgument arg)
    {
        var items = ((IEnumerable<CustomAttributeTypedArgument>)arg.Value!).ToArray();
        var value = new string[items.Length];
        for (int i = 0; i < items.Length; i++)
            value[i] = (string)items[i].Value!;
        return value;
    }
}
