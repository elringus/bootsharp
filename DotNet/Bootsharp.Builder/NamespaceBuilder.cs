using System.Reflection;
using System.Text.RegularExpressions;

namespace Bootsharp.Builder;

internal sealed class NamespaceBuilder
{
    private const string converterAttributeName = "JSNamespaceAttribute";
    private readonly List<Func<string, string>> converters = new();

    public void CollectConverters (string outDir, string entryAssembly)
    {
        using var context = CreateLoadContext(outDir);
        var assemblyPath = Path.Combine(outDir, entryAssembly);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        foreach (var attribute in CollectAttributes(assembly))
            AddConverter(attribute);
    }

    public string Build (Type type)
    {
        var space = type.Namespace ?? "Global";
        foreach (var converter in converters)
            space = converter(space);
        return space;
    }

    private CustomAttributeData[] CollectAttributes (System.Reflection.Assembly assembly)
    {
        return assembly.CustomAttributes
            .Where(a => a.AttributeType.Name == converterAttributeName).ToArray();
    }

    private void AddConverter (CustomAttributeData attribute)
    {
        var pattern = attribute.ConstructorArguments[0].Value as string;
        var replacement = attribute.ConstructorArguments[1].Value as string;
        converters.Add(space => Regex.Replace(space, pattern!, replacement!));
    }
}
