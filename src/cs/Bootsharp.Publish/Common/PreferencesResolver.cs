using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class PreferencesResolver (string entryAssemblyName)
{
    private static readonly string cfgAttrFullName = typeof(JSConfigurationAttribute<>).FullName!;

    public Preferences Resolve (string outDir)
    {
        using var ctx = CreateLoadContext(outDir);
        var assembly = LoadEntryAssembly(ctx, outDir);
        if (FindConfigurationAttribute(assembly) is not { } attr) return new();
        return InstantiateCustomPrefs(attr.AttributeType);
    }

    private Assembly LoadEntryAssembly (MetadataLoadContext ctx, string outDir)
    {
        var path = Path.Combine(outDir, entryAssemblyName);
        return ctx.LoadFromAssemblyPath(path);
    }

    private CustomAttributeData? FindConfigurationAttribute (Assembly assembly)
    {
        foreach (var attr in assembly.CustomAttributes)
            if (IsConfigurationAttribute(attr.AttributeType))
                return attr;
        return null;
    }

    private bool IsConfigurationAttribute (Type attributeType)
    {
        if (attributeType.FullName is null) return false;
        return attributeType.FullName.StartsWith(cfgAttrFullName, StringComparison.Ordinal);
    }

    private Preferences InstantiateCustomPrefs (Type attributeType)
    {
        var prefsType = attributeType.GenericTypeArguments[0];
        return (Preferences)Activator.CreateInstance(prefsType)!;
    }
}
