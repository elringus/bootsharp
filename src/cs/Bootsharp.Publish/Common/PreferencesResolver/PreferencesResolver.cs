using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bootsharp.Publish;

internal sealed class PreferencesResolver (string entryAssemblyName)
{
    public Preferences Resolve (string outDir)
    {
        var ctx = new PreferencesContext(outDir);
        var assembly = ctx.LoadAssembly(entryAssemblyName);
        if (FindConfigurationAttribute(assembly) is not { } attr) return new();
        return InstantiateCustomPrefs(assembly, attr.AttributeType);
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
        return Unsafe.As<Preferences>(assembly.CreateInstance(prefsType.FullName!)!);
    }
}
