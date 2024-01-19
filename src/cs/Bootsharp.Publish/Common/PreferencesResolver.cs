using System.Reflection;
using System.Runtime.Loader;

namespace Bootsharp.Publish;

internal sealed class PreferencesResolver (string entryAssemblyName)
{
    public Preferences Resolve (string outDir)
    {
        var ctx = new AssemblyLoadContext(entryAssemblyName, true);
        var assembly = LoadMainAssembly(ctx, outDir);
        if (FindConfigurationAttribute(assembly) is not { } attr) return new();
        return InstantiateCustomPrefs(assembly, attr.AttributeType);
    }

    private Assembly LoadMainAssembly (AssemblyLoadContext ctx, string outDir)
    {
        var path = Path.GetFullPath(Path.Combine(outDir, entryAssemblyName));
        using var stream = File.OpenRead(path);
        return ctx.LoadFromStream(stream);
    }

    private CustomAttributeData? FindConfigurationAttribute (Assembly assembly)
    {
        var cfgAttr = typeof(JSConfigurationAttribute<>).FullName!;
        foreach (var attr in assembly.CustomAttributes)
            if (attr.AttributeType.FullName!.StartsWith(cfgAttr, StringComparison.Ordinal))
                return attr;
        return null;
    }

    private Preferences InstantiateCustomPrefs (Assembly assembly, Type attributeType)
    {
        var prefsType = attributeType.GenericTypeArguments[0];
        return (Preferences)assembly.CreateInstance(prefsType.FullName!)!;
    }
}
