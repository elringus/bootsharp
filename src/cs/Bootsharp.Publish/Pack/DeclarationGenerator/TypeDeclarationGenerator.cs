using System.Reflection;
using System.Text;

namespace Bootsharp.Publish;

internal sealed class TypeDeclarationGenerator (Preferences prefs)
{
    private readonly StringBuilder builder = new();
    private readonly TypeConverter converter = new(prefs);

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
        if (type.Namespace == null) return false;
        if (prevType == null) return true;
        return GetNamespace(prevType) != GetNamespace(type);
    }

    private void OpenNamespace ()
    {
        var space = GetNamespace(type);
        AppendLine($"export namespace {space} {{", 0);
    }

    private bool ShouldCloseNamespace ()
    {
        if (type.Namespace == null) return false;
        if (nextType is null) return true;
        return GetNamespace(nextType) != GetNamespace(type);
    }

    private void CloseNamespace ()
    {
        AppendLine("}", 0);
    }

    private void DeclareInterface ()
    {
        var indent = type.Namespace != null ? 1 : 0;
        AppendLine($"export interface {BuildTypeName(type)}", indent);
        AppendExtensions();
        builder.Append(" {");
        AppendProperties();
        AppendLine("}", indent);
    }

    private void DeclareEnum ()
    {
        var indent = type.Namespace != null ? 1 : 0;
        AppendLine($"export enum {type.Name} {{", indent);
        var names = Enum.GetNames(type);
        for (int i = 0; i < names.Length; i++)
            if (i == names.Length - 1) AppendLine(names[i], indent + 1);
            else AppendLine($"{names[i]},", indent + 1);
        AppendLine("}", indent);
    }

    private string GetNamespace (Type type)
    {
        var space = WithPrefs(prefs.Space, type.FullName!, BuildJSSpace(type));
        var lastDotIdx = space.LastIndexOf('.');
        return lastDotIdx >= 0 ? space[..lastDotIdx] : space;
    }

    private void AppendExtensions ()
    {
        var extTypes = new List<Type>(type.GetInterfaces().Where(types.Contains));
        if (type.BaseType is { } baseType && types.Contains(baseType))
            extTypes.Insert(0, baseType);
        if (extTypes.Count > 0)
            builder.Append(" extends ").AppendJoin(", ", extTypes.Select(t => WithPrefs(prefs.Space, t.FullName!, BuildJSSpace(t))));
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
        var indent = type.Namespace != null ? 1 : 0;
        AppendLine(ToFirstLower(property.Name), indent + 1);
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
        var space = WithPrefs(prefs.Space, type.FullName!, BuildJSSpace(type));
        var name = space[(space.LastIndexOf('.') + 1)..];
        if (!type.IsGenericType) return name;
        var args = string.Join(", ", type.GetGenericArguments().Select(BuildTypeName));
        return $"{name}<{args}>";
    }
}
