using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class NamespaceBuilder
{
    private const string defaultSpace = "Global";
    private const string attributeName = "JSNamespaceAttribute";
    private readonly List<NamespaceConverter> converters = [];

    public void CollectConverters (string outDir, string entryAssembly)
    {
        using var context = CreateLoadContext(outDir);
        var assemblyPath = Path.Combine(outDir, entryAssembly);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        foreach (var attribute in CollectAttributes(assembly))
            converters.Add(new NamespaceConverter(attribute));
    }

    public string Build (Type type)
    {
        var space = type.Namespace ?? defaultSpace;
        foreach (var converter in converters)
            space = converter.Convert(space);
        return space;
    }

    private IEnumerable<CustomAttributeData> CollectAttributes (Assembly assembly)
    {
        return assembly.CustomAttributes.Where(a => a.AttributeType.Name == attributeName);
    }
}
