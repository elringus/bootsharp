namespace Bootsharp.Publish;

/// <summary>
/// Generates JavaScript binding proxies for <see cref="InstanceMeta"/>.
/// Symmetrical to <see cref="CSInstanceGenerator"/>, which generates the same but for the C# side.
/// </summary>
internal sealed class JSInstanceGenerator (bool debug, JSModules md)
{
    public string Generate (IReadOnlyCollection<InstanceMeta> its) =>
        $$"""
          import { Event } from "../bcl/event.mjs";
          import { {{(debug ? "exports, getExport" : "exports")}} } from "../exports.mjs";
          import { instances as $i } from "../instances.mjs";
          import $s, { serialize } from "./serializer.g.mjs";
          {{Fmt(EmitImports(), 0)}}

          {{Fmt([
              ..its.Where(s => s.Importer != null).Select(EmitImporter),
              ..its.Where(s => s.IK == InteropKind.Export).Select(EmitProxy)
          ], 0)}}

          export default $i;
          """;

    private IEnumerable<string> EmitImports () => md.List
        .Where(m => m.Nodes.Any(o => o.Any(t => t is InstanceMeta { IK: InteropKind.Export })))
        .Select(m => $"""import * as {m.Alias} from "./modules/{m.Path}.g.mjs";""");

    private string EmitImporter (InstanceMeta it)
    {
        var evt = it.Members.OfType<EventMeta>().ToArray();
        return
            $$"""
              $i.{{it.Importer}} = function (it) {
                  return $i.import(it, _id => {
                      {{Fmt(evt.Select(e => $"it.{e.JSName}.subscribe(handle{e.Name});"))}}
                      return () => {
                          {{Fmt(evt.Select(e => $"it.{e.JSName}.unsubscribe(handle{e.Name});"), 2)}}
                      };

                      {{Fmt(evt.Select(EmitHandler))}}
                  });
              };
              """;

        string EmitHandler (EventMeta e)
        {
            var fnName = $"{it.Proxy.Id}_Invoke{e.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var args = string.Join(", ", e.Args.Select(a => a.JSName));
            var invArgs = PrependIdArg(string.Join(", ", e.Args.Select(ImportJS)));
            return $"function handle{e.Name}({args}) {{ {invName}({invArgs}); }}";
        }
    }

    private string EmitProxy (InstanceMeta it) => it switch {
        DelegateMeta del => EmitDelegateProxy(del),
        { Proxy: SpecializedProxy sp } => EmitSpecializedProxy(it, sp),
        _ => EmitOpaqueProxy(it)
    };

    private string EmitDelegateProxy (DelegateMeta del)
    {
        var inv = del.Invoker;
        var args = string.Join(", ", inv.Args.Select(a => a.JSName));
        var invArgs = PrependIdArg(args);
        return $$"""
                 $i.{{del.Id}} = class {{del.Proxy.Id}} {
                     constructor(_id) {
                         const fn = ({{args}}) => {{md.Ref(inv.Surf)}}.{{inv.JSName}}({{invArgs}});
                         fn._id = _id;
                         return fn;
                     }
                 };
                 """;
    }

    private string EmitOpaqueProxy (InstanceMeta it) =>
        $$"""
          $i.{{it.Id}} = class {{it.Proxy.Id}} {
              {{Fmt([
                  "constructor(_id) { this._id = _id; }",
                  ..it.Members.Select(EmitMember)
              ])}}
          };
          """;

    private string EmitSpecializedProxy (InstanceMeta it, SpecializedProxy sp) =>
        $$"""
          $i.{{it.Id}} = class {{it.Proxy.Id}} {
              {{Fmt([
                  "constructor(_id) { this._id = _id; }",
                  ..it.Members.Select(EmitMember),
                  sp.JS
              ])}}
          };
          """;

    private string EmitMember (MemberMeta member) => member switch {
        EventMeta evt => EmitEvent(evt),
        PropertyMeta prop => EmitProperty(prop),
        _ => EmitMethod((MethodMeta)member)
    };

    private string EmitEvent (EventMeta evt)
    {
        var args = string.Join(", ", evt.Args.Select(a => a.JSName));
        return Fmt(0,
            $"{evt.JSName} = new Event();",
            $"broadcast{evt.Name}({args}) {{ this.{evt.JSName}.broadcast({args}); }}"
        );
    }

    private string EmitMethod (MethodMeta method)
    {
        var args = string.Join(", ", method.Args.Select(a => a.JSName));
        var invArgs = args.Length > 0 ? $"this._id, {args}" : "this._id";
        var bodyExp = $"{md.Ref(method.Surf)}.{method.JSName}({invArgs})";
        if (!method.Void) bodyExp = $"return {bodyExp}";
        return $"{method.JSName}({args}) {{ {bodyExp}; }}";
    }

    private string EmitProperty (PropertyMeta p) => Fmt(0,
        p.CanGet ? $"get {p.JSName}() {{ return {md.Ref(p.Surf)}.get{p.Name}(this._id); }}" : null,
        p.CanSet ? $"set {p.JSName}(value) {{ {md.Ref(p.Surf)}.set{p.Name}(this._id, value); }}" : null
    );
}
