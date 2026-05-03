using System.Reflection;
using System.Text;

namespace Bootsharp.Publish;

internal sealed class TypeDeclarationGenerator (Preferences prefs)
{
    private readonly StringBuilder builder = new();
    private readonly TypeSyntaxBuilder ts = new(prefs);

    private Type type => GetTypeAt(index);
    private Type? prevType => index == 0 ? null : GetTypeAt(index - 1);
    private Type? nextType => index == types.Length - 1 ? null : GetTypeAt(index + 1);
    private int indent => !string.IsNullOrEmpty(GetNamespace(type)) ? 1 : 0;

    private DocumentationBuilder docs = null!;
    private InstancedMeta[] instanced = null!;
    private Type[] types = null!;
    private int index;

    public string Generate (SolutionInspection spec)
    {
        docs = new(spec.Documentation);
        instanced = [..spec.Instanced];
        types = spec.Types.Select(t => t.Clr).Where(IsUserType).OrderBy(GetNamespace).ToArray();
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

    private void DeclareEnum ()
    {
        builder.Append(docs.BuildType(type, indent));
        AppendLine($"export enum {type.Name} {{", indent);
        var names = Enum.GetNames(type);
        for (int i = 0; i < names.Length; i++)
        {
            builder.Append(docs.BuildProperty(type.GetField(names[i])!, indent + 1));
            if (i == names.Length - 1) AppendLine(names[i], indent + 1);
            else AppendLine($"{names[i]},", indent + 1);
        }
        AppendLine("}", indent);
    }

    private void DeclareInterface ()
    {
        builder.Append(docs.BuildType(type, indent));
        AppendLine($"export interface {BuildTypeName(type)}", indent);
        AppendExtensions();
        builder.Append(" {");
        if (instanced.FirstOrDefault(i => i.Type.Clr == type) is { } it)
            foreach (var member in it.Members)
                switch (member)
                {
                    case EventMeta e: AppendInstancedEvent(e); break;
                    case PropertyMeta p: AppendInstancedProperty(p); break;
                    case MethodMeta m: AppendInstancedFunction(m); break;
                }
        else AppendProperties();
        AppendLine("}", indent);
    }

    private void AppendExtensions ()
    {
        var extTypes = new List<Type>(type.GetInterfaces().Where(types.Contains));
        if (type.BaseType is { } baseType && types.Contains(baseType))
            extTypes.Insert(0, baseType);
        if (extTypes.Count > 0)
            builder.Append(" extends ").AppendJoin(", ", extTypes.Select(t => ts.Build(t, null)));
    }

    private void AppendProperties ()
    {
        var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
        foreach (var prop in type.GetProperties(flags))
            if (prop.GetMethod != null && prop.GetIndexParameters().Length == 0)
            {
                builder.Append(docs.BuildProperty(prop, indent + 1));
                AppendProperty(ToFirstLower(prop.Name), prop.PropertyType, GetNullability(prop));
            }
    }

    private void AppendProperty (string name, Type type, NullabilityInfo? nullability)
    {
        AppendLine(name, indent + 1);
        if (IsNullable(type, nullability)) builder.Append('?');
        builder.Append(": ");
        if (type.IsGenericTypeParameter) builder.Append(type.Name);
        else builder.Append(ts.Build(type, nullability));
        builder.Append(';');
    }

    private void AppendInstancedEvent (EventMeta evt)
    {
        builder.Append(docs.BuildEvent(evt, indent + 1));
        var type = evt.Interop == InteropKind.Export ? "EventSubscriber" : "EventBroadcaster";
        AppendLine(evt.JSName, indent + 1);
        builder.Append($": {type}<[");
        builder.AppendJoin(", ", evt.Arguments.Select(a => $"{a.JSName}: {ts.BuildArg(a)}"));
        builder.Append("]>;");
    }

    private void AppendInstancedProperty (PropertyMeta prop)
    {
        var value = prop.GetValue ?? prop.SetValue!;
        builder.Append(docs.BuildProperty(prop.Info, indent + 1));
        var name = prop.CanGet && !prop.CanSet ? $"readonly {prop.JSName}" : prop.JSName;
        AppendProperty(name, value.Type.Clr, value.Nullability);
    }

    private void AppendInstancedFunction (MethodMeta meta)
    {
        builder.Append(docs.BuildFunction(meta, indent + 1));
        AppendLine(meta.JSName, indent + 1);
        builder.Append('(');
        builder.AppendJoin(", ", meta.Arguments.Select(a => $"{a.JSName}: {ts.BuildArg(a)}"));
        builder.Append("): ");
        builder.Append(ts.BuildReturn(meta));
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
        if (!type.IsGenericType) return type.Name;
        var name = TrimGeneric(type.Name);
        var args = string.Join(", ", type.GetGenericArguments().Select(BuildTypeName));
        return $"{name}<{args}>";
    }

    private string GetNamespace (Type type)
    {
        return BuildJSSpace(type, prefs);
    }
}
