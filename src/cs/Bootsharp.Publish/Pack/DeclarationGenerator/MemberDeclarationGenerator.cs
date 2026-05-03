using System.Text;

namespace Bootsharp.Publish;

internal sealed class MemberDeclarationGenerator (Preferences prefs)
{
    private readonly StringBuilder builder = new();
    private readonly TypeSyntaxBuilder typeBuilder = new(prefs);

    private MemberMeta member => members[index];
    private MemberMeta? prevMember => index == 0 ? null : members[index - 1];
    private MemberMeta? nextMember => index == members.Length - 1 ? null : members[index + 1];

    private DocumentationBuilder docs = null!;
    private MemberMeta[] members = null!;
    private int index;

    public string Generate (SolutionInspection spec)
    {
        docs = new(spec.Documentation);
        members = spec.Static
            .Concat(spec.Modules.SelectMany(i => i.Members))
            .OrderBy(m => m.JSSpace).ToArray();
        for (index = 0; index < members.Length; index++)
            DeclareMember();
        return builder.ToString();
    }

    private void DeclareMember ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        switch (member)
        {
            case EventMeta e: DeclareEvent(e); break;
            case PropertyMeta p: DeclareProperty(p); break;
            case MethodMeta { Interop: InteropKind.Export } m: DeclareMethodExport(m); break;
            case MethodMeta { Interop: InteropKind.Import } m: DeclareMethodImport(m); break;
        }
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevMember is null) return true;
        return prevMember.JSSpace != member.JSSpace;
    }

    private void OpenNamespace ()
    {
        builder.Append(docs.BuildType(member.Info.DeclaringType!, 0));
        builder.Append($"\nexport namespace {member.JSSpace} {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextMember is null) return true;
        return nextMember.JSSpace != member.JSSpace;
    }

    private void CloseNamespace ()
    {
        builder.Append("\n}");
    }

    private void DeclareEvent (EventMeta evt)
    {
        builder.Append(docs.BuildEvent(evt, 1));
        var type = evt.Interop == InteropKind.Export ? "EventSubscriber" : "EventBroadcaster";
        builder.Append($"\n    export const {evt.JSName}: {type}<[");
        builder.AppendJoin(", ", evt.Arguments.Select(a => $"{a.JSName}: {typeBuilder.BuildArg(a)}"));
        builder.Append("]>;");
    }

    private void DeclareProperty (PropertyMeta prop)
    {
        var value = prop.GetValue ?? prop.SetValue!;
        builder.Append(docs.BuildProperty(prop.Info, 1));
        builder.Append($"\n    export {(prop.CanGet && !prop.CanSet ? "const" : "let")} {prop.JSName}: ");
        builder.Append(typeBuilder.Build(value.Type.Clr, value.Nullability));
        if (value.Nullable) builder.Append(" | undefined");
        builder.Append(';');
    }

    private void DeclareMethodExport (MethodMeta method)
    {
        builder.Append(docs.BuildFunction(method, 1));
        builder.Append($"\n    export function {method.JSName}(");
        builder.AppendJoin(", ", method.Arguments.Select(a => $"{a.JSName}: {typeBuilder.BuildArg(a)}"));
        builder.Append($"): {typeBuilder.BuildReturn(method)};");
    }

    private void DeclareMethodImport (MethodMeta method)
    {
        builder.Append(docs.BuildFunction(method, 1));
        builder.Append($"\n    export let {method.JSName}: (");
        builder.AppendJoin(", ", method.Arguments.Select(a => $"{a.JSName}: {typeBuilder.BuildArg(a)}"));
        builder.Append($") => {typeBuilder.BuildReturn(method)};");
    }
}
