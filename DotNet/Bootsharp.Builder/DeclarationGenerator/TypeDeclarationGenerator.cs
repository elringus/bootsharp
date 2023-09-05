using System.Reflection;
using System.Text;

namespace Bootsharp.Builder;

internal sealed class TypeDeclarationGenerator(NamespaceBuilder spaceBuilder)
{
    private readonly StringBuilder builder = new();
    private readonly TypeConverter converter = new(spaceBuilder);

    private Type type => GetTypeAt(index);
    private Type? prevType => index == 0 ? null : GetTypeAt(index - 1);
    private Type? nextType => index == types.Length - 1 ? null : GetTypeAt(index + 1);

    private Type[] types = null!;
    private int index;

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
        if (type.IsEnum) DeclareEnum();
        else DeclareInterface();
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

    private void DeclareInterface ()
    {
        AppendLine($"export interface {BuildTypeName(type)}", 1);
        AppendExtensions();
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
        return spaceBuilder.Build(type);
    }

    private void AppendExtensions ()
    {
        var extTypes = new List<Type>(type.GetInterfaces().Where(types.Contains));
        if (type.BaseType is { } baseType && types.Contains(baseType))
            extTypes.Insert(0, baseType);
        if (extTypes.Count > 0)
            builder.Append(" extends ").AppendJoin(", ", extTypes.Select(converter.ToTypeScript));
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
