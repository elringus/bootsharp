using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class PreferencesResolver (string entryAssemblyName)
{
    public Preferences Resolve (string outDir)
    {
        using var ctx = CreateLoadContext(outDir);
        var assemblyPath = Path.Combine(outDir, entryAssemblyName);
        var assembly = ctx.LoadFromAssemblyPath(assemblyPath);
        var attribute = FindPreferencesAttribute(assembly);
        return CreatePreferences(attribute);
    }

    private CustomAttributeData? FindPreferencesAttribute (Assembly assembly)
    {
        foreach (var attr in assembly.CustomAttributes)
            if (attr.AttributeType.FullName == typeof(JSPreferencesAttribute).FullName)
                return attr;
        return null;
    }

    private Preferences CreatePreferences (CustomAttributeData? attr) => new() {
        Space = CreatePreferences(nameof(JSPreferencesAttribute.Space), attr) ?? [],
        Type = CreatePreferences(nameof(JSPreferencesAttribute.Type), attr) ?? [],
        Event = CreatePreferences(nameof(JSPreferencesAttribute.Event), attr) ?? [new(@"^Notify(\S+)", "On$1")],
        Function = CreatePreferences(nameof(JSPreferencesAttribute.Function), attr) ?? []
    };

    private Preference[]? CreatePreferences (string name, CustomAttributeData? attr)
    {
        if (attr is null || !attr.NamedArguments.Any(a => a.MemberName == name)) return null;
        var value = CreateValue(attr.NamedArguments.First(a => a.MemberName == name).TypedValue);
        var prefs = new Preference[value.Length / 2];
        for (int i = 0; i < prefs.Length; i++)
            prefs[i] = new(value[i * 2], value[(i * 2) + 1]);
        return prefs;
    }

    private string[] CreateValue (CustomAttributeTypedArgument arg)
    {
        var items = ((IEnumerable<CustomAttributeTypedArgument>)arg.Value!).ToArray();
        var value = new string[items.Length];
        for (int i = 0; i < items.Length; i++)
            value[i] = (string)items[i].Value!;
        return value;
    }
}
