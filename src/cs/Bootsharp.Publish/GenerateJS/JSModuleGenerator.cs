using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

internal sealed class JSModuleGenerator (bool debug)
{
    private readonly CodeBuilder bld = new();

    [MemberNotNullWhen(true, nameof(it))]
    private bool isIt => srf is InstanceMeta;

    private string id = null!;
    private SurfaceMeta srf = null!;
    private InstanceMeta? it => srf as InstanceMeta;

    public string Generate (JSModule module)
    {
        bld.Clear();
        foreach (var node in module.Nodes)
            EmitNode(node, $"export const {node.Name} = {{", "};");
        return Fmt([EmitImports(module), bld.ToString()], 0, "\n\n");
    }

    private string EmitImports (JSModule md) =>
        $$"""
          import { Event } from "{{md.To("event")}}";
          import { {{(debug ? "exports, getExport" : "exports")}} } from "{{md.To("exports")}}";
          import { {{(debug ? "importEvent, getImport" : "importEvent")}} } from "{{md.To("imports")}}";
          import $i from "{{md.ToGen("instances")}}";
          import $s, { serialize, deserialize } from "{{md.ToGen("serializer")}}";
          """;

    private void EmitNode (JSNode node, string header, string footer)
    {
        if (!node.Any(t => t is SurfaceMeta || t is SerializedEnumMeta)) return;
        bld.Enter(header, ",");
        foreach (var type in node.Types)
            if (type is SerializedEnumMeta enu)
                EmitEnum(enu);
            else if (type is SurfaceMeta srf)
                foreach (var member in srf.Members)
                    EmitMember(member, srf);
        foreach (var child in node.Children)
            EmitNode(child, $"{child.Name}: {{", "}");
        bld.Exit(footer);
    }

    private void EmitEnum (SerializedEnumMeta enu)
    {
        var values = Enum.GetValuesAsUnderlyingType(enu.Clr).Cast<object>().ToArray();
        foreach (var value in values)
            bld.Line($"\"{value}\": \"{Enum.GetName(enu.Clr, value)}\"");
        foreach (var value in values)
            bld.Line($"\"{Enum.GetName(enu.Clr, value)}\": {value}");
    }

    private void EmitMember (MemberMeta member, SurfaceMeta surf)
    {
        srf = surf;
        id = (surf as ProxyMeta)?.Proxy.Id ?? surf.Id;
        switch (member)
        {
            case EventMeta { IK: InteropKind.Export } e: EmitEventExport(e); break;
            case EventMeta { IK: InteropKind.Import } e: EmitEventImport(e); break;
            case PropertyMeta { IK: InteropKind.Export } p: EmitPropertyExport(p); break;
            case PropertyMeta { IK: InteropKind.Import } p: EmitPropertyImport(p); break;
            case MethodMeta { IK: InteropKind.Export } m: EmitMethodExport(m); break;
            default: EmitMethodImport((MethodMeta)member); break;
        }
    }

    private void EmitEventExport (EventMeta evt)
    {
        var name = $"broadcast{evt.Name}Serialized";
        var args = string.Join(", ", evt.Args.Select(a => a.JSName));
        var invArgs = string.Join(", ", evt.Args.Select(arg =>
            // By default, we use 'null' for missing collection items, but here the event args array
            // represents args specified to the event's 'broadcast' function, so user expects 'undefined'.
            $"{ExportJS(arg)}{(arg.Value.Nullable ? " ?? undefined" : "")}"));
        if (isIt)
        {
            var invName = $"$i.resolve(_id, $i.{it.Id}).broadcast{evt.Name}";
            bld.Line($"{name}: ({PrependIdArg(args)}) => {invName}({invArgs})");
        }
        else
        {
            var invName = $"{srf.JSNode}.{evt.JSName}.broadcast";
            bld.Line($"{evt.JSName}: new Event()");
            bld.Line($"{name}: ({args}) => {invName}({invArgs})");
        }
    }

    private void EmitEventImport (EventMeta evt)
    {
        if (isIt) return; // instance import events handled in instances.g.mjs
        var fnName = $"{id}_Invoke{evt.Name}";
        var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
        var args = string.Join(", ", evt.Args.Select(a => a.JSName));
        var invArgs = string.Join(", ", evt.Args.Select(ImportJS));
        bld.Line($"{evt.JSName}: importEvent(({args}) => {invName}({invArgs}))");
    }

    private void EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var fnName = $"{id}_Get{prop.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var body = ExportJS(prop.Get, isIt ? $"{invName}(_id)" : $"{invName}()");
            if (prop.Get.Nullable && !prop.Get.IsInstanced) body += " ?? undefined";
            bld.Line(isIt
                ? $"get{prop.Name}(_id) {{ return {body}; }}"
                : $"get {prop.JSName}() {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            var fnName = $"{id}_Set{prop.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var value = ImportJS(prop.Set, "value");
            var call = isIt ? $"{invName}(_id, {value})" : $"{invName}({value})";
            bld.Line(isIt
                ? $"set{prop.Name}(_id, value) {{ {call}; }}"
                : $"set {prop.JSName}(value) {{ {call}; }}");
        }
    }

    private void EmitPropertyImport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var exp = ImportJS(prop.Get, isIt
                ? $"$i.imported(_id).{prop.JSName}"
                : $"this.{prop.JSName}.get()");
            var argsList = isIt ? "_id" : "";
            bld.Line($"get{prop.Name}Serialized({argsList}) {{ return {exp}; }}");
        }
        if (prop.CanSet)
        {
            var value = ExportJS(prop.Set, "value");
            var argsList = isIt ? "_id, value" : "value";
            var bodyExp = isIt
                ? $"$i.imported(_id).{prop.JSName} = {value}"
                : $"this.{prop.JSName}.set({value})";
            bld.Line($"set{prop.Name}Serialized({argsList}) {{ {bodyExp}; }}");
        }
    }

    private void EmitMethodExport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var fnName = $"{id}_{method.Name}";
        var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
        var args = string.Join(", ", method.Args.Select(a => a.JSName));
        if (isIt) args = PrependIdArg(args);
        var invArgs = string.Join(", ", method.Args.Select(ImportJS));
        if (isIt) invArgs = PrependIdArg(invArgs);
        var bodyExp = ExportJS(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        bld.Line($"{method.JSName}: {(wait ? "async " : "")}({args}) => {bodyExp}");
    }

    private void EmitMethodImport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var name = method.JSName;
        var args = string.Join(", ", method.Args.Select(a => a.JSName));
        if (isIt) args = PrependIdArg(args);
        var invArgs = string.Join(", ", method.Args.Select(ExportJS));
        var invName = isIt
            ? $"$i.imported(_id).{name}"
            : $"this.{name}Handler";
        var bodyExp = ImportJS(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        var srdHandler = $"{(wait ? "async " : "")}({args}) => {bodyExp}";
        if (isIt) bld.Line($"{name}Serialized: {srdHandler}");
        else
        {
            var srd = $"this.{name}SerializedHandler";
            var srdExp = debug ? $"getImport({invName}, {srd}, \"{srf.JSNode}.{name}\")" : srd;
            bld.Line($"get {name}() {{ return {invName}; }}");
            bld.Line($"set {name}(handler) {{ {invName} = handler; {srd} = {srdHandler}; }}");
            bld.Line($"get {name}Serialized() {{ return {srdExp}; }}");
        }
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Args.Any(a => a.Value.IsSerialized || a.Value.IsInstanced) ||
               method.Return.IsSerialized || method.Return.IsInstanced;
    }
}
