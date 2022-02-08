using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeScriptModelsGenerator;
using TypeScriptModelsGenerator.Options;
using static Packer.TextUtilities;
using static Packer.TypeUtilities;
using static TypeScriptModelsGenerator.TypeScriptModelsGeneration;

namespace Packer;

internal class TypeDeclarationGenerator
{
    private readonly StringBuilder builder = new();
    private readonly TypeConverter typeConverter = new();

    public string Generate (IReadOnlyCollection<Type> types)
    {
        Setup(types, Configure).Execute(out var result);
        foreach (var file in result.Files)
            GenerateForFile(file);
        foreach (var enumType in types.Where(t => t.IsEnum))
            GenerateForEnum(enumType);
        return builder.ToString();
    }

    private void Configure (IOptions options)
    {
        options.InitializeTypes = false;
        options.GenerationMode = GenerationMode.Loose;
        options.Rules.MatchType(t => !t.IsEnum);
        foreach (var code in Enum.GetNames<TypeCode>().Where(c => c != "Object"))
            options.Rules.ReplaceType(code, typeConverter.ToTypeScript(Type.GetType($"System.{code}")));
    }

    private void GenerateForEnum (Type enumType)
    {
        builder.Append($"export enum {enumType.Name} {{\n");
        builder.AppendJoin(",\n", Enum.GetNames(enumType).Select(e => $"    {e}"));
        builder.Append("\n}\n");
    }

    private void GenerateForFile (TypeScriptFile file)
    {
        var excludedProps = GetExcludedPropertyNames(file.Type);
        foreach (var line in SplitLines(file.Content))
            if (!line.StartsWith("import") && !DeclaresExcludedProperty(line, excludedProps))
                builder.Append(ModifyLine(line)).Append('\n');
    }

    private bool DeclaresExcludedProperty (string line, IEnumerable<string> excludedProps)
    {
        return excludedProps.Any(p => line.Contains($" {p}: ", StringComparison.OrdinalIgnoreCase));
    }

    private string[] GetExcludedPropertyNames (Type type)
    {
        if (type.IsInterface) return Array.Empty<string>();
        return type.GetProperties()
            .Where(p => IsStatic(p) || !IsAutoProperty(p, type))
            .Select(p => p.Name).ToArray();
    }

    private string ModifyLine (string content) => content
        .Replace(" extends Object", "")
        .Replace("List<", "Array<");
}
