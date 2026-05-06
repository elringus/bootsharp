using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Bootsharp.Publish;

internal sealed class TypeDeclarationGenerator (Preferences prefs)
{
    private readonly StringBuilder bld = new();
    private readonly TypeSyntaxBuilder ts = new(prefs);

    private TypeMeta meta => metas[index];
    private TypeMeta? prevMeta => index == 0 ? null : metas[index - 1];
    private TypeMeta? nextMeta => index == metas.Count - 1 ? null : metas[index + 1];
    private int indent => !string.IsNullOrEmpty(GetNamespace(meta)) ? 1 : 0;

    private DocumentationBuilder docs = null!;
    private readonly Dictionary<Type, TypeMeta> metaByClr = [];
    private readonly List<TypeMeta> metas = [];
    private int index;

    public string Generate (SolutionInspection spec)
    {
        docs = new(spec.Documentation);
        CollectMetas(spec);
        for (index = 0; index < metas.Count; index++)
            DeclareType();
        return bld.ToString();
    }

    private void CollectMetas (SolutionInspection spec)
    {
        foreach (var meta in spec.Instanced)
            metas.Add(metaByClr[meta.Clr] = meta);
        foreach (var meta in spec.Serialized)
            if (metaByClr.TryAdd(meta.Clr, meta) && IsUserType(meta.Clr))
                metas.Add(meta);
        metas.Sort((a, b) => string.Compare(GetNamespace(a), GetNamespace(b), StringComparison.Ordinal));
    }

    private void DeclareType ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (meta is InstancedMeta it) DeclareInstanced(it);
        else if (meta is SerializedEnumMeta enu) DeclareEnum(enu);
        else if (meta is SerializedObjectMeta obj) DeclareSerialized(obj);
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (string.IsNullOrEmpty(GetNamespace(meta))) return false;
        if (prevMeta == null) return true;
        return GetNamespace(prevMeta) != GetNamespace(meta);
    }

    private void OpenNamespace ()
    {
        var space = GetNamespace(meta);
        AppendLine($"export namespace {space} {{", 0);
    }

    private bool ShouldCloseNamespace ()
    {
        if (string.IsNullOrEmpty(GetNamespace(meta))) return false;
        if (nextMeta is null) return true;
        return GetNamespace(nextMeta) != GetNamespace(meta);
    }

    private void CloseNamespace ()
    {
        AppendLine("}", 0);
    }

    private void DeclareEnum (SerializedEnumMeta enu)
    {
        bld.Append(docs.BuildType(enu.Clr, indent));
        AppendLine($"export enum {enu.Clr.Name} {{", indent);
        var names = Enum.GetNames(enu.Clr);
        for (int i = 0; i < names.Length; i++)
        {
            bld.Append(docs.BuildProperty(enu.Clr.GetField(names[i])!, indent + 1));
            if (i == names.Length - 1) AppendLine(names[i], indent + 1);
            else AppendLine($"{names[i]},", indent + 1);
        }
        AppendLine("}", indent);
    }

    private void DeclareSerialized (SerializedObjectMeta obj)
    {
        bld.Append(docs.BuildType(obj.Clr, indent));
        AppendLine($"export type {ts.BuildName(obj.Clr)} = ", indent);
        if (TryGetBase(obj.Clr, out var baseType))
            bld.Append(ts.BuildFullName(baseType)).Append(" & ");
        bld.Append("Readonly<{");
        foreach (var prop in obj.Properties)
            if (ShouldDeclareOn(obj.Clr, prop.Info))
                AppendProperty(prop);
        AppendLine("}>;", indent);

        void AppendProperty (SerializedPropertyMeta prop)
        {
            bld.Append(docs.BuildProperty(prop.Info, indent + 1));
            AppendLine(prop.JSName, indent + 1);
            bld.Append(ts.BuildProperty(prop.Info));
            bld.Append(';');
        }
    }

    private void DeclareInstanced (InstancedMeta it)
    {
        bld.Append(docs.BuildType(it.Clr, indent));
        AppendLine($"export interface {ts.BuildName(it.Clr)}", indent);
        AppendExtensions();
        bld.Append(" {");
        foreach (var member in it.Members)
            if (!ShouldDeclareOn(it.Clr, member.Info)) continue;
            else if (member is EventMeta evt) AppendEvent(evt);
            else if (member is PropertyMeta prop) AppendProperty(prop);
            else AppendMethod((MethodMeta)member);
        AppendLine("}", indent);

        void AppendExtensions ()
        {
            var extTypes = new List<Type>(it.Clr.GetInterfaces().Where(IsUserType));
            if (TryGetBase(it.Clr, out var baseType))
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

    private string GetNamespace (TypeMeta meta)
    {
        return BuildJSSpace(meta.Clr, prefs);
    }

    private bool TryGetBase (Type clr, [NotNullWhen(true)] out Type? baseType)
    {
        if ((baseType = clr.BaseType) == null || !IsUserType(baseType)) return false;
        return metaByClr.ContainsKey(baseType);
    }

    private bool ShouldDeclareOn (Type host, MemberInfo member)
    {
        if (member.DeclaringType == host) return true;
        return !TryGetBase(member.DeclaringType!, out _) && !TryGetBase(host, out _);
    }
}
