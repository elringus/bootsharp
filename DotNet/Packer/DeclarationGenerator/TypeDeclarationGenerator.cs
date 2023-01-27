using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static Packer.TypeUtilities;
using static Packer.TextUtilities;

namespace Packer;

internal class TypeDeclarationGenerator
{
    private readonly StringBuilder builder = new();
    private readonly NamespaceBuilder spaceBuilder;
    private readonly TypeConverter converter;

    private Type type => GetTypeAt(index);
    private Type? prevType => index == 0 ? null : GetTypeAt(index - 1);
    private Type? nextType => index == types.Length - 1 ? null : GetTypeAt(index + 1);

    private Type[] types = null!;
    private int index;

    public TypeDeclarationGenerator (NamespaceBuilder spaceBuilder)
    {
        this.spaceBuilder = spaceBuilder;
        converter = new TypeConverter(spaceBuilder);
    }

    public string Generate (IEnumerable<Type> sourceTypes)
    {
        types = sourceTypes.OrderBy(GetNamespace).ToArray();
        for (index = 0; index < types.Length; index++)
            DeclareType();
        return builder.ToString();
    }

    private Type GetTypeAt (int index)
    {
        var type = types[index];
        return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
    }

    private void DeclareType ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (type.IsClass || IsStructType()) DeclareClass();
        if (type.IsInterface) DeclareInterface();
        if (type.IsEnum) DeclareEnum();
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevType is null) return true;
        return spaceBuilder.Build(prevType) != GetNamespace(type);
    }

    private void OpenNamespace ()
    {
        var space = GetNamespace(type);
        AppendLine($"export namespace {space} {{", 0);
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextType is null) return true;
        return GetNamespace(nextType) != GetNamespace(type);
    }

    private void CloseNamespace ()
    {
        AppendLine("}", 0);
    }

    private void DeclareClass ()
    {
        AppendLine($"export class {BuildTypeName(type)}", 1);
        AppendBaseType();
        AppendInterfaces();
        builder.Append(" {");
        AppendProperties();
        AppendLine("}", 1);
    }

    private void DeclareInterface ()
    {
        AppendLine($"export interface {BuildTypeName(type)}", 1);
        AppendBaseType();
        AppendInterfaces();
        builder.Append(" {");
        AppendProperties();
        AppendLine("}", 1);
    }

    private void DeclareEnum ()
    {
        AppendLine($"export enum {type.Name} {{", 1);
        var names = Enum.GetNames(type);
        for (int i = 0; i < names.Length; i++)
            if (i == names.Length - 1) AppendLine(names[i], 2);
            else AppendLine($"{names[i]},", 2);
        AppendLine("}", 1);
    }

    private bool IsStructType () => type is { IsClass: false, IsEnum: false, IsValueType: true, IsPrimitive: false };

    private string GetNamespace (Type type)
    {
        return spaceBuilder.Build(type);
    }

    private void AppendBaseType ()
    {
        if (type.BaseType is { } baseType && types.Contains(baseType))
            builder.Append($" extends {converter.ToTypeScript(baseType)}");
    }

    private void AppendInterfaces ()
    {
        var interfaces = type.GetInterfaces().Where(i => types.Contains(i)).ToArray();
        if (interfaces.Length == 0) return;
        builder.Append(" implements ");
        builder.AppendJoin(", ", interfaces.Select(converter.ToTypeScript));
    }

    private void AppendProperties ()
    {
        var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
        foreach (var property in type.GetProperties(flags))
            if (IsAutoProperty(property) || type.IsInterface)
                AppendProperty(property);
    }

    private void AppendProperty (PropertyInfo property)
    {
        AppendLine(ToFirstLower(property.Name), 2);
        if (IsNullable(property)) builder.Append('?');
        builder.Append($": {BuildType()};");

        string BuildType ()
        {
            if (property.PropertyType.IsGenericTypeParameter) return property.PropertyType.Name;
            return converter.ToTypeScript(property.PropertyType, GetNullability(property));
        }
    }

    private void AppendLine (string content, int level)
    {
        builder.Append('\n');
        Append(content, level);
    }

    private void Append (string content, int level)
    {
        for (int i = 0; i < level * 4; i++)
            builder.Append(' ');
        builder.Append(content);
    }

    private string BuildTypeName (Type type)
    {
        if (!type.IsGenericType) return type.Name;
        var args = string.Join(", ", type.GetGenericArguments().Select(a => a.Name));
        return $"{GetGenericNameWithoutArgs(type)}<{args}>";
    }
}
