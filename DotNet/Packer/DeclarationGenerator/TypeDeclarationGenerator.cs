using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly HashSet<PropertyInfo> excludedProperties = new();
    private readonly HashSet<PropertyInfo> nullableProperties = new();

    public string Generate (IReadOnlyCollection<Type> types)
    {
        Setup(types, Configure).Execute(out var result);
        foreach (var type in types)
            ScanProperties(type);
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

    private void ScanProperties (Type type)
    {
        if (type.IsInterface) return;
        foreach (var property in type.GetProperties())
            if (ShouldExcludeProperty(property))
                excludedProperties.Add(property);
            else if (IsNullable(property))
                nullableProperties.Add(property);
    }

    private void GenerateForEnum (Type enumType)
    {
        builder.Append($"export enum {enumType.Name} {{\n");
        builder.AppendJoin(",\n", Enum.GetNames(enumType).Select(e => $"    {e}"));
        builder.Append("\n}\n");
    }

    private void GenerateForFile (TypeScriptFile file)
    {
        foreach (var line in SplitLines(file.Content))
            GenerateForLine(line, file.Type);
    }

    private void GenerateForLine (string line, Type declaringType)
    {
        if (line.StartsWith("import") || DeclaresExcludedProperty(line, declaringType)) return;
        line = Regex.Replace(line, " extends Object", "");
        line = Regex.Replace(line, "List<", "Array<");
        line = Regex.Replace(line, @": Nullable<(\S+)>", ": $1");
        if (DeclaresNullableProperty(line, declaringType))
            line = Regex.Replace(line, @" (\S+): ", " $1?: ");
        builder.Append(line).Append('\n');
    }

    private bool ShouldExcludeProperty (PropertyInfo property)
    {
        return IsStatic(property) || !IsAutoProperty(property);
    }

    private bool DeclaresExcludedProperty (string line, Type declaringType)
    {
        foreach (var property in excludedProperties)
            if (property.DeclaringType == declaringType &&
                line.Contains($" {property.Name}: ", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private bool DeclaresNullableProperty (string line, Type declaringType)
    {
        foreach (var property in nullableProperties)
            if (property.DeclaringType == declaringType &&
                line.Contains($" {property.Name}: ", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
