using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Packer.TypeUtilities;

namespace Packer;

internal class TypeDeclarationGenerator
{
    private readonly List<Type> types = new();
    private readonly StringBuilder builder = new();
    private readonly TypeConverter typeConverter = new();

    private Type type => types[index];
    private Type prevType => index == 0 ? null : types[index - 1];
    private Type nextType => index == types.Count - 1 ? null : types[index + 1];

    private int index;

    public string Generate (IEnumerable<Type> sourceTypes)
    {
        ResetState(sourceTypes);
        for (index = 0; index < types.Count; index++)
            ProcessType();
        return builder.ToString();
    }

    private void ResetState (IEnumerable<Type> sourceTypes)
    {
        builder.Clear();
        types.Clear();
        types.AddRange(sourceTypes.OrderBy(GetNamespace));
    }

    private void ProcessType ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (type.IsClass) ProcessClass();
        if (type.IsInterface) ProcessInterface();
        if (type.IsEnum) ProcessEnum();
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
        builder.Append($"\nexport namespace {name} {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextType is null) return true;
        return GetNamespace(nextType) != GetNamespace(type);
    }

    private void CloseNamespace ()
    {
        builder.Append("\n}");
    }

    private void ProcessClass () { }

    private void ProcessInterface () { }

    private void ProcessEnum ()
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
        return typeConverter.ToNamespace(assemblyName);
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
