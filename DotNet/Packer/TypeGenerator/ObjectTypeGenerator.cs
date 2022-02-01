using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TypeScriptModelsGenerator;
using TypeScriptModelsGenerator.Options;
using static Packer.TypeConversion;
using static Packer.Utilities;
using static TypeScriptModelsGenerator.TypeScriptModelsGeneration;

namespace Packer;

internal class ObjectTypeGenerator
{
    private readonly HashSet<Type> types = new();
    private readonly StringBuilder builder = new();

    public void AddObjectType (Type type)
    {
        if (!types.Add(type)) return;
        AddProperties(type);
        AddBaseType(type);
    }

    public string GenerateDefinitions ()
    {
        Setup(types, Configure).Execute(out var result);
        foreach (var file in result.Files)
            GenerateForFile(file);
        foreach (var enumType in types.Where(t => t.IsEnum))
            GenerateForEnum(enumType);
        return builder.ToString();
    }

    private void AddProperties (Type type)
    {
        var propertyTypesToAdd = type.GetProperties()
            .Select(m => m.PropertyType)
            .Where(ShouldConvertToObjectType);
        foreach (var propertyType in propertyTypesToAdd)
            AddObjectType(IsArray(propertyType) ? GetArrayElementType(propertyType) : propertyType);
    }

    private void AddBaseType (Type type)
    {
        if (type.BaseType != null && ShouldConvertToObjectType(type.BaseType))
            AddObjectType(type.BaseType);
    }

    private void Configure (IOptions options)
    {
        options.InitializeTypes = false;
        options.GenerationMode = GenerationMode.Loose;
        options.Rules.MatchType(t => !t.IsEnum);
        foreach (var code in Enum.GetNames<TypeCode>().Where(c => c != "Object"))
            options.Rules.ReplaceType(code, ToTypeScript(Type.GetType($"System.{code}")));
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
            .Where(p => IsStatic(p) || !IsAutoProperty(p))
            .Select(p => p.Name).ToArray();
    }

    public static bool IsStatic (PropertyInfo info)
    {
        return info.GetAccessors().Any(a => a.IsStatic);
    }

    public static bool IsAutoProperty (PropertyInfo info)
    {
        var backingFieldName = $"<{info.Name}>k__BackingField";
        var backingField = info.DeclaringType?.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return backingField != null;
    }

    private string ModifyLine (string content) => content
        .Replace(" extends Object", "")
        .Replace("List<", "Array<");
}
