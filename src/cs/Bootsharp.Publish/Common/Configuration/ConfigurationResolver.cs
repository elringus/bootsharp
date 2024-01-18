using System.Reflection;
using System.Runtime.Loader;

namespace Bootsharp.Publish;

internal sealed class ConfigurationResolver (string entryAssemblyName)
{
    public Configuration Resolve (string outDir)
    {
        var ctx = new AssemblyLoadContext(entryAssemblyName, true);
        var assembly = LoadMainAssembly(ctx, outDir);
        return new(GetPreferences(assembly), ctx);
    }

    private Assembly LoadMainAssembly (AssemblyLoadContext ctx, string outDir)
    {
        var path = Path.Combine(outDir, entryAssemblyName);
        return ctx.LoadFromAssemblyPath(path);
    }

    private Preferences GetPreferences (Assembly assembly)
    {
        if (FindConfigurationAttribute(assembly) is not { } attr) return new();
        return InstantiateCustomPrefs(assembly, attr.AttributeType);
    }

    private CustomAttributeData? FindConfigurationAttribute (Assembly assembly)
    {
        foreach (var attr in assembly.CustomAttributes)
            if (IsConfigurationAttribute(attr.AttributeType))
                return attr;
        return null;
    }

    private bool IsConfigurationAttribute (Type attr)
    {
        if (attr.FullName is null) return false;
        return attr.FullName.StartsWith(typeof(JSConfigurationAttribute<>).FullName!, StringComparison.Ordinal);
    }

    private Preferences InstantiateCustomPrefs (Assembly assembly, Type attributeType)
    {
        var prefsType = attributeType.GenericTypeArguments[0];
        return (Preferences)assembly.CreateInstance(prefsType.FullName!)!;
    }
}
