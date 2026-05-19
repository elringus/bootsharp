using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator
{
    private readonly CodeBuilder bld = new();
    private readonly TypeSyntaxBuilder ts;
    private readonly DocumentationBuilder doc;
    private readonly SolutionInspection spec;
    private readonly JSModules mds;

    public DeclarationGenerator (SolutionInspection spec, JSModules mds)
    {
        this.spec = spec;
        this.mds = mds;
        ts = new(mds);
        doc = new(bld, spec.Docs);
    }

    public string Generate (JSModule module)
    {
        bld.Clear();
        ts.EnterModule(module);
        foreach (var node in module.Nodes)
            DeclareNode(node);
        return Fmt([EmitImports(module), bld.ToString()], 0, "\n\n");
    }

    private string EmitImports (JSModule md) => Fmt([
        $$"""import type { Event } from "{{md.To("event")}}";""",
        ..mds.GetImported(md).Select(imp =>
            $"""import type * as {imp.Alias} from "{md.ToGen(imp.Path)}";""")
    ], 0);

    private void DeclareNode (JSNode node)
    {
        var surf = node.Types.FirstOrDefault(s => s is SurfaceMeta and not InstanceMeta);
        var wrap = surf != null || node.Children.Count > 0;
        if (wrap)
        {
            if (surf != null) doc.Type(surf);
            bld.Enter($"export namespace {node.Name} {{");
        }
        // Distinct by CLR to discard the other side of a bidirectional (export+import)
        // instance surface, because both produce identical declarations.
        foreach (var type in node.Types.DistinctBy(t => t.Clr))
            if (type is SerializedEnumMeta enu) DeclareEnum(enu);
            else if (type is SerializedObjectMeta o) DeclareSerialized(o);
            else if (type is InstanceMeta it) DeclareInstance(it);
            else if (type is SurfaceMeta srf) DeclareSurface(srf);
        foreach (var child in node.Children)
            DeclareNode(child);
        if (wrap) bld.Exit("}");
    }

    private void DeclareEnum (SerializedEnumMeta enu)
    {
        doc.Type(enu);
        bld.Enter($$"""export enum {{ts.BuildName(enu.Clr)}} {""");
        var names = Enum.GetNames(enu.Clr);
        for (var i = 0; i < names.Length; i++)
        {
            doc.Property(enu.Clr.GetField(names[i])!);
            bld.Line(i == names.Length - 1 ? names[i] : $"{names[i]},");
        }
        bld.Exit("}");
    }

    private void DeclareSerialized (SerializedObjectMeta obj)
    {
        doc.Type(obj);
        var ext = spec.Types.HasBase(obj.Clr, out var bs) ? $"{ts.BuildFullName(bs.Clr)} & " : "";
        bld.Enter($$"""export type {{ts.BuildName(obj.Clr)}} = {{ext}}Readonly<{""");
        foreach (var prop in obj.Properties.Where(p => ShouldDeclareOn(obj.Clr, p.Info)))
        {
            doc.Property(prop.Info);
            bld.Line($"{prop.JSName}{ts.BuildProperty(prop.Info)};");
        }
        bld.Exit("}>;");
    }

    private void DeclareInstance (InstanceMeta it)
    {
        doc.Type(it);
        bld.Enter($$"""export interface {{ts.BuildName(it.Clr)}}{{BuildExtensions()}} {""");
        foreach (var member in it.Members.Where(m => ShouldDeclareOn(it.Clr, m.Info)))
            if (member is EventMeta evt) DeclareEvent(evt);
            else if (member is PropertyMeta prop) DeclareProperty(prop);
            else if (member is MethodMeta method) DeclareMethod(method);
        bld.Exit("}");

        string BuildExtensions ()
        {
            var ext = it.Clr.GetInterfaces().Where(IsUserType).ToList();
            if (spec.Types.HasBase(it.Clr, out var bs)) ext.Insert(0, bs.Clr);
            return ext.Count == 0 ? "" : $" extends {string.Join(", ", ext.Select(ts.BuildFullName))}";
        }

        void DeclareEvent (EventMeta evt)
        {
            doc.Event(evt);
            var args = string.Join(", ", evt.Args.Select(a => $"{a.JSName}: {ts.BuildArg(evt.Info, a.Info)}"));
            bld.Line($"{evt.JSName}: Event<[{args}]>;");
        }

        void DeclareProperty (PropertyMeta prop)
        {
            doc.Property(prop.Info);
            var name = prop.CanSet ? prop.JSName : $"readonly {prop.JSName}";
            bld.Line($"{name}{ts.BuildProperty(prop.Info)};");
        }

        void DeclareMethod (MethodMeta method)
        {
            doc.Method(method);
            var args = string.Join(", ", method.Args.Select(a => $"{a.JSName}: {ts.BuildArg(a.Info)}"));
            bld.Line($"{method.JSName}({args}): {ts.BuildReturn(method.Info)};");
        }
    }

    private void DeclareSurface (SurfaceMeta surf)
    {
        foreach (var member in surf.Members)
            if (member is EventMeta evt) DeclareEvent(evt);
            else if (member is PropertyMeta prop) DeclareProperty(prop);
            else if (member is MethodMeta method) DeclareMethod(method);

        void DeclareEvent (EventMeta evt)
        {
            doc.Event(evt);
            var args = string.Join(", ", evt.Args.Select(a => $"{a.JSName}: {ts.BuildArg(evt.Info, a.Info)}"));
            bld.Line($"export const {evt.JSName}: Event<[{args}]>;");
        }

        void DeclareProperty (PropertyMeta prop)
        {
            doc.Property(prop.Info);
            var stx = ts.BuildVariable(prop.Info);
            var mod = prop.CanGet && !prop.CanSet ? "const" : "let";
            if (prop.IK == InteropKind.Import)
                bld.Line($$"""export let {{prop.JSName}}: { {{Fmt([
                    prop.CanGet ? $"get: () => {stx}" : null,
                    prop.CanSet ? $"set: (value: {stx}) => void" : null
                ], 0, "; ")}} };""");
            else bld.Line($"export {mod} {prop.JSName}: {stx};");
        }

        void DeclareMethod (MethodMeta method)
        {
            doc.Method(method);
            var args = string.Join(", ", method.Args.Select(a => $"{a.JSName}: {ts.BuildArg(a.Info)}"));
            var result = ts.BuildReturn(method.Info);
            if (method.IK == InteropKind.Export)
                bld.Line($"export function {method.JSName}({args}): {result};");
            else bld.Line($"export let {method.JSName}: ({args}) => {result};");
        }
    }

    private bool ShouldDeclareOn (Type host, MemberInfo member)
    {
        if (member.DeclaringType == host) return true;
        return !spec.Types.HasBase(member.DeclaringType!, out _) && !spec.Types.HasBase(host, out _);
    }
}
