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
    private int indent => !string.IsNullOrEmpty(GetNamespace(type)) ? 1 : 0;

    private InterfaceMeta[] instanced = null!;
    private Type[] types = null!;
    private int index;

    public string Generate (SolutionInspection inspection)
    {
        instanced = [..inspection.InstancedInterfaces];
        types = inspection.Crawled.Where(FilterCrawled).OrderBy(GetNamespace).ToArray();
        for (index = 0; index < types.Length; index++)
            DeclareType();
        return builder.ToString();
    }

    private Type GetTypeAt (int index)
    {
        var type = types[index];
        return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
    }

    private bool FilterCrawled (Type type)
    {
        return !IsList(type) && !IsCollection(type) && !IsNullable(type);
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
        if (string.IsNullOrEmpty(GetNamespace(type))) return false;
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
        if (string.IsNullOrEmpty(GetNamespace(type))) return false;
        if (nextType is null) return true;
        return GetNamespace(nextType) != GetNamespace(type);
    }

    private void CloseNamespace ()
    {
        AppendLine("}", 0);
    }

    private void DeclareInterface ()
    {
        AppendLine($"export interface {BuildTypeName(type)}", indent);
        AppendExtensions();
        builder.Append(" {");
        if (instanced.FirstOrDefault(i => i.Type == type) is { } inst)
            AppendInstancedMethods(inst);
        else AppendProperties();
        AppendLine("}", indent);
    }

    private void DeclareEnum ()
    {
        AppendLine($"export enum {type.Name} {{", indent);
        var names = Enum.GetNames(type);
        for (int i = 0; i < names.Length; i++)
            if (i == names.Length - 1) AppendLine(names[i], indent + 1);
            else AppendLine($"{names[i]},", indent + 1);
        AppendLine("}", indent);
    }

    private string GetNamespace (Type type)
    {
        return BuildJSSpace(type, prefs);
    }

    private void AppendExtensions ()
    {
        var extTypes = new List<Type>(type.GetInterfaces().Where(types.Contains));
        if (type.BaseType is { } baseType && types.Contains(baseType))
            extTypes.Insert(0, baseType);
        if (extTypes.Count > 0)
            builder.Append(" extends ").AppendJoin(", ", extTypes.Select(t => converter.ToTypeScript(t, null)));
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
        AppendLine(ToFirstLower(property.Name), indent + 1);
        if (IsNullable(property)) builder.Append('?');
        builder.Append(": ");
        if (property.PropertyType.IsGenericTypeParameter) builder.Append(property.PropertyType.Name);
        else builder.Append(converter.ToTypeScript(property.PropertyType, GetNullability(property)));
        builder.Append(';');
    }

    private void AppendInstancedMethods (InterfaceMeta instanced)
    {
        foreach (var meta in instanced.Methods)
            if (meta.Kind == MethodKind.Event)
                AppendInstancedEvent(meta);
            else AppendInstancedFunction(meta);
    }

    private void AppendInstancedEvent (MethodMeta meta)
    {
        AppendLine(meta.JSName, indent + 1);
        builder.Append(": Event<[");
        builder.AppendJoin(", ", meta.Arguments.Select(a => $"{a.JSName}: {a.Value.JSTypeSyntax}"));
        builder.Append("]>;");
    }

    private void AppendInstancedFunction (MethodMeta meta)
    {
        AppendLine(meta.JSName, indent + 1);
        builder.Append('(');
        builder.AppendJoin(", ", meta.Arguments.Select(a => $"{a.JSName}: {a.Value.JSTypeSyntax}"));
        builder.Append("): ");
        builder.Append(meta.ReturnValue.JSTypeSyntax);
        builder.Append(';');
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
        var name = BuildJSSpaceName(type);
        if (!type.IsGenericType) return name;
        var args = string.Join(", ", type.GetGenericArguments().Select(BuildTypeName));
        return $"{name}<{args}>";
    }
}
