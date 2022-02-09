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
    private readonly NamespaceBuilder namespaceBuilder;
    private readonly TypeConverter converter;

    private Type type => types[index];
    private Type prevType => index == 0 ? null : types[index - 1];
    private Type nextType => index == types.Length - 1 ? null : types[index + 1];

    private Type[] types;
    private int index;

    public TypeDeclarationGenerator (NamespaceBuilder namespaceBuilder)
    {
        this.namespaceBuilder = namespaceBuilder;
        converter = new TypeConverter(namespaceBuilder);
    }

    public string Generate (IEnumerable<Type> sourceTypes)
    {
        types = sourceTypes.OrderBy(GetNamespace).ToArray();
        for (index = 0; index < types.Length; index++)
            DeclareType();
        return builder.ToString();
    }

    private void DeclareType ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (type.IsClass) DeclareClass();
        if (type.IsInterface) DeclareInterface();
        if (type.IsEnum) DeclareEnum();
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevType is null) return true;
        return GetNamespace(prevType) != GetNamespace(type);
    }

    private void OpenNamespace ()
    {
        var name = GetNamespace(type);
        AppendLine($"export namespace {name} {{", 0);
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
        AppendLine($"export class {type.Name}", 1);
        AppendBaseType();
        AppendInterfaces();
        builder.Append(" {");
        AppendProperties();
        AppendLine("}", 1);
    }

    private void DeclareInterface ()
    {
        AppendLine($"export interface {type.Name}", 1);
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

    private string GetNamespace (Type type)
    {
        var assemblyName = GetAssemblyName(type);
        return namespaceBuilder.Build(assemblyName);
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
        builder.Append($": {converter.ToTypeScript(property.PropertyType)};");
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
}
