using System.Reflection;
using System.Text;

namespace Bootsharp.Publish;

internal sealed class TypeDeclarationGenerator (Preferences prefs)
{
    private readonly StringBuilder bld = new();
    private readonly TypeSyntaxBuilder ts = new(prefs);

    private Type type => types[index];
    private Type? prevType => index == 0 ? null : types[index - 1];
    private Type? nextType => index == types.Length - 1 ? null : types[index + 1];
    private int indent => !string.IsNullOrEmpty(GetNamespace(type)) ? 1 : 0;

    private DocumentationBuilder docs = null!;
    private Dictionary<Type, InstancedMeta> itByType = null!;
    private Type[] types = null!;
    private int index;

    public string Generate (SolutionInspection spec)
    {
        docs = new(spec.Documentation);
        itByType = spec.Instanced.ToDictionary(it => it.Clr);
        types = spec.Types.Select(t => t.Clr).Where(IsUserType).OrderBy(GetNamespace).ToArray();
        for (index = 0; index < types.Length; index++)
            DeclareType();
        return bld.ToString();
    }

    private void DeclareType ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (itByType.TryGetValue(type, out var it)) DeclareInstanced(it);
        else if (type.IsEnum) DeclareEnum();
        else DeclareSerialized();
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
        bld.Append(docs.BuildType(type, indent));
        AppendLine($"export enum {type.Name} {{", indent);
        var names = Enum.GetNames(type);
        for (int i = 0; i < names.Length; i++)
        {
            bld.Append(docs.BuildProperty(type.GetField(names[i])!, indent + 1));
            if (i == names.Length - 1) AppendLine(names[i], indent + 1);
            else AppendLine($"{names[i]},", indent + 1);
        }
        AppendLine("}", indent);
    }

    private void DeclareSerialized ()
    {
        bld.Append(docs.BuildType(type, indent));
        AppendLine($"export type {ts.BuildName(type)} = ", indent);
        if (type.BaseType is { } baseType && types.Contains(baseType))
            bld.Append(ts.BuildFullName(baseType)).Append(" & ");
        bld.Append("Readonly<{");
        var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
        foreach (var prop in type.GetProperties(flags))
            if (prop.GetMethod != null && prop.GetIndexParameters().Length == 0)
                AppendProperty(prop);
        AppendLine("}>;", indent);

        void AppendProperty (PropertyInfo prop)
        {
            bld.Append(docs.BuildProperty(prop, indent + 1));
            AppendLine(BuildJSName(prop.Name), indent + 1);
            bld.Append(ts.BuildProperty(prop));
            bld.Append(';');
        }
    }

    private void DeclareInstanced (InstancedMeta it)
    {
        bld.Append(docs.BuildType(type, indent));
        AppendLine($"export interface {ts.BuildName(type)}", indent);
        AppendExtensions();
        bld.Append(" {");
        foreach (var member in it.Members.Where(m => m.Info.DeclaringType == it.Clr))
            if (member is EventMeta evt) AppendEvent(evt);
            else if (member is PropertyMeta prop) AppendProperty(prop);
            else AppendMethod((MethodMeta)member);
        AppendLine("}", indent);

        void AppendExtensions ()
        {
            var extTypes = new List<Type>(type.GetInterfaces().Where(types.Contains));
            if (type.BaseType is { } baseType && types.Contains(baseType))
                extTypes.Insert(0, baseType);
            if (extTypes.Count > 0)
                bld.Append(" extends ").AppendJoin(", ", extTypes.Select(ts.BuildFullName));
        }

        void AppendEvent (EventMeta evt)
        {
            bld.Append(docs.BuildEvent(evt, indent + 1));
            AppendLine(evt.JSName, indent + 1);
            var type = evt.Interop == InteropKind.Export ? "EventSubscriber" : "EventBroadcaster";
            bld.Append($": {type}<[");
            bld.AppendJoin(", ", evt.Arguments.Select(a => $"{a.JSName}: {ts.BuildArg(evt.Info, a.Info)}"));
            bld.Append("]>;");
        }

        void AppendProperty (PropertyMeta prop)
        {
            bld.Append(docs.BuildProperty(prop.Info, indent + 1));
            var name = !prop.CanSet ? $"readonly {prop.JSName}" : prop.JSName;
            AppendLine(name, indent + 1);
            bld.Append(ts.BuildProperty(prop.Info));
            bld.Append(';');
        }

        void AppendMethod (MethodMeta meta)
        {
            bld.Append(docs.BuildFunction(meta, indent + 1));
            AppendLine(meta.JSName, indent + 1);
            bld.Append('(');
            bld.AppendJoin(", ", meta.Arguments.Select(a => $"{a.JSName}: {ts.BuildArg(a.Info)}"));
            bld.Append("): ");
            bld.Append(ts.BuildReturn(meta.Info));
            bld.Append(';');
        }
    }

    private void AppendLine (string content, int level)
    {
        bld.Append('\n');
        Append(content, level);
    }

    private void Append (string content, int level)
    {
        for (int i = 0; i < level * 4; i++)
            bld.Append(' ');
        bld.Append(content);
    }

    private string GetNamespace (Type type)
    {
        return BuildJSSpace(type, prefs);
    }
}
