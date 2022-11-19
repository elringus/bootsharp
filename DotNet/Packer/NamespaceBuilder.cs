using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static Packer.TypeUtilities;

namespace Packer;

internal class NamespaceBuilder
{
    private const string converterAttributeName = "JSNamespaceAttribute";
    private readonly List<Func<string, string, string>> converters = new();

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
        var space = type.Namespace ?? "Bindings";
        foreach (var converter in converters)
            space = converter(space, type.Name);
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
        var appendType = (bool)attribute.ConstructorArguments[2].Value!;
        converters.Add(Convert);

        string Convert (string space, string type)
        {
            if (appendType) space = $"{space}.{type}";
            return Regex.Replace(space, pattern!, replacement!);
        }
    }
}
