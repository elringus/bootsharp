using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (Preferences prefs, bool debug)
{
    private record Binding (MemberMeta? Member, Type? Enum, InstancedMeta? It, string Id, string Space);

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];
    [MemberNotNullWhen(true, nameof(it))]
    private bool isIt => it != null;
    private InstancedMeta? it => binding.It;
    private string space => binding.Space;
    private string id => binding.Id;

    private readonly StringBuilder bld = new();
    private Binding[] bindings = [];
    private int index, level;

    public string Generate (SolutionInspection spec)
    {
        bindings = spec.Static
            .Select(m => new Binding(m, null, null, m.Space.Replace('.', '_'), m.JSSpace))
            .Concat(spec.Modules.SelectMany(md => md.Members
                .Select(m => new Binding(m, null, null, md.FullName.Replace('.', '_'), m.JSSpace))))
            .Concat(spec.Instanced.SelectMany(it => it.Members
                .Select(m => new Binding(m, null, it, it.FullName.Replace('.', '_'), m.JSSpace))))
            .Concat(spec.Serialized.Where(t => t.Clr.IsEnum)
                .Select(t => new Binding(null, t.Clr, null, "", BuildJSSpace(t.Clr, prefs))))
            .OrderBy(m => m.Space).ToArray();
        if (bindings.Length == 0) return "";

        EmitImports();
        bld.Append("\n\n");

        if (debug)
        {
            EmitDebugHelpers();
            bld.Append("\n\n");
        }

        EmitHelpers();
        bld.Append("\n\n");

        bld.Append(new BindingSerializerGenerator().Generate(spec.Serialized));
        bld.Append("\n\n");

        foreach (var it in spec.Instanced.Where(i => i.Importer != null))
            EmitImporter(it);
        bld.Append("\n\n");

        if (spec.Instanced.Count > 0)
            bld.Append(new BindingClassGenerator().Generate(spec));

        for (index = 0; index < bindings.Length; index++)
            EmitBinding();

        return bld.ToString();
    }

    private void EmitImports ()
    {
        bld.Append(
            """
            import { exports } from "../exports.mjs";
            import { Event } from "../event.mjs";
            import { instances } from "../instances.mjs";
            import { serialize, deserialize, binary, types } from "../serialization.mjs";
            """
        );
    }

    private void EmitDebugHelpers ()
    {
        bld.Append(
            """
            function getExport(name) {
                return (...args) => {
                    if (exports == null) throw Error("Boot the runtime before invoking C# APIs.");
                    let result;
                    try { result = exports[name](...args); }
                    catch (error) { throw Error(`${error.message}\n${error.stack}`); }
                    if (typeof result?.then === "function")
                        return result.catch(error => { throw Error(`${error.message}\n${error.stack}`); });
                    return result;
                };
            }

            function getImport(handler, serializedHandler, name) {
                if (typeof handler !== "function") throw Error(`Failed to invoke '${name}' from C#. Make sure to assign the function in JavaScript.`);
                return serializedHandler;
            }
            """
        );
    }

    private void EmitHelpers ()
    {
        bld.Append(
            """
            function importEvent(handler) {
                const event = new Event();
                const broadcast = event.broadcast.bind(event);
                event.broadcast = (...args) => { broadcast(...args); handler(...args); };
                return event;
            }
            """
        );
    }

    private void EmitBinding ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (binding.Member != null) EmitMember(binding.Member);
        else EmitEnum(binding.Enum!);
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevBinding is null) return true;
        return prevBinding.Space != binding.Space;
    }

    private void OpenNamespace ()
    {
        level = 0;
        var prevParts = prevBinding?.Space.Split('.') ?? [];
        var parts = binding.Space.Split('.');
        while (prevParts.ElementAtOrDefault(level) == parts[level]) level++;
        for (var i = level; i < parts.Length; level = i, i++)
            if (i == 0) bld.Append($"\nexport const {parts[i]} = {{");
            else bld.Append($"{Comma}\n{Pad(i)}{parts[i]}: {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextBinding is null) return true;
        return nextBinding.Space != binding.Space;
    }

    private void CloseNamespace ()
    {
        var target = GetCloseLevel();
        for (; level >= target; level--)
            if (level == 0) bld.Append("\n};");
            else bld.Append($"\n{Pad(level)}}}");

        int GetCloseLevel ()
        {
            if (nextBinding is null) return 0;
            var closeLevel = 0;
            var parts = binding.Space.Split('.');
            var nextParts = nextBinding.Space.Split('.');
            for (var i = 0; i < parts.Length; i++)
                if (parts[i] == nextParts[i]) closeLevel++;
                else break;
            return closeLevel;
        }
    }

    private void EmitMember (MemberMeta member)
    {
        switch (member)
        {
            case EventMeta { Interop: InteropKind.Export } e: EmitEventExport(e); break;
            case EventMeta { Interop: InteropKind.Import } e: EmitEventImport(e); break;
            case PropertyMeta { Interop: InteropKind.Export } p: EmitPropertyExport(p); break;
            case PropertyMeta { Interop: InteropKind.Import } p: EmitPropertyImport(p); break;
            case MethodMeta { Interop: InteropKind.Export } m: EmitMethodExport(m); break;
            case MethodMeta { Interop: InteropKind.Import } m: EmitMethodImport(m); break;
        }
    }

    private void EmitEventExport (EventMeta evt)
    {
        var name = $"broadcast{evt.Name}Serialized";
        var args = string.Join(", ", evt.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", evt.Arguments.Select(arg =>
            // By default, we use 'null' for missing collection items, but here the event args array
            // represents args specified to the event's 'broadcast' function, so user expects 'undefined'.
            $"{ImportJS(arg)}{(arg.Value.Nullable ? " ?? undefined" : "")}"));
        if (isIt)
        {
            var invName = $"instances.export(_id, id => new {it.JSName}(id)).broadcast{evt.Name}";
            bld.Append($"{Br}{name}: ({PrependIdArg(args)}) => {invName}({invArgs})"
                .IgnoreV8("id =>")); // Uncoverable, as finalization in Node is not controllable.
        }
        else
        {
            var invName = $"{evt.JSSpace}.{evt.JSName}.broadcast";
            bld.Append($"{Br}{evt.JSName}: new Event()");
            bld.Append($"{Br}{name}: ({args}) => {invName}({invArgs})");
        }
    }

    private void EmitEventImport (EventMeta evt)
    {
        if (isIt) return; // instanced import event handlers are emitted in the registrar
        var name = $"{id}_Invoke{evt.Name}";
        var invName = debug ? $"""getExport("{name}")""" : $"exports.{name}";
        var args = string.Join(", ", evt.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", evt.Arguments.Select(ExportJS));
        bld.Append($"{Br}{evt.JSName}: importEvent(({args}) => {invName}({invArgs}))");
    }

    private void EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var fnName = $"{id}_GetProperty{prop.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var body = ImportJS(prop.GetValue, isIt ? $"{invName}(_id)" : $"{invName}()");
            if (prop.GetValue.Nullable && !prop.GetValue.IsInstanced) body += " ?? undefined";
            if (isIt) bld.Append($"{Br}getProperty{prop.Name}(_id) {{ return {body}; }}");
            else bld.Append($"{Br}get {prop.JSName}() {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            var fnName = $"{id}_SetProperty{prop.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var value = ExportJS(prop.SetValue, "value");
            var body = isIt ? $"{invName}(_id, {value})" : $"{invName}({value})";
            if (isIt) bld.Append($"{Br}setProperty{prop.Name}(_id, value) {{ {body}; }}");
            else bld.Append($"{Br}set {prop.JSName}(value) {{ {body}; }}");
        }
    }

    private void EmitPropertyImport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var args = isIt ? "_id" : "";
            var body = ExportJS(prop.GetValue,
                isIt ? $"instances.imported(_id).{prop.JSName}" : $"this.{prop.JSName}.get()");
            bld.Append($"{Br}getProperty{prop.Name}Serialized({args}) {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            var value = ImportJS(prop.SetValue, "value");
            var args = isIt ? "_id, value" : "value";
            var body = isIt ? $"instances.imported(_id).{prop.JSName} = {value}" : $"this.{prop.JSName}.set({value})";
            bld.Append($"{Br}setProperty{prop.Name}Serialized({args}) {{ {body}; }}");
        }
    }

    private void EmitMethodExport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var fnName = $"{id}_{method.Name}";
        var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
        var args = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (isIt) args = PrependIdArg(args);
        var invArgs = string.Join(", ", method.Arguments.Select(ExportJS));
        if (isIt) invArgs = PrependIdArg(invArgs);
        var body = ImportJS(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        bld.Append($"{Br}{method.JSName}: {(wait ? "async " : "")}({args}) => {body}");
    }

    private void EmitMethodImport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var name = method.JSName;
        var args = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (isIt) args = PrependIdArg(args);
        var invArgs = string.Join(", ", method.Arguments.Select(ImportJS));
        var invName = isIt ? $"instances.imported(_id).{name}" : $"this.{name}Handler";
        var body = ExportJS(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        var serdeHandler = $"{(wait ? "async " : "")}({args}) => {body}";
        if (isIt) bld.Append($"{Br}{name}Serialized: {serdeHandler}");
        else
        {
            var serde = $"this.{name}SerializedHandler";
            var serdeExp = debug ? $"getImport({invName}, {serde}, \"{space}.{name}\")" : serde;
            bld.Append($"{Br}get {name}() {{ return {invName}; }}");
            bld.Append($"{Br}set {name}(handler) {{ {invName} = handler; {serde} = {serdeHandler}; }}");
            bld.Append($"{Br}get {name}Serialized() {{ return {serdeExp}; }}");
        }
    }

    private void EmitEnum (Type @enum)
    {
        var values = Enum.GetValuesAsUnderlyingType(@enum).Cast<object>().ToArray();
        var fields = string.Join(", ", values
            .Select(v => $"\"{v}\": \"{Enum.GetName(@enum, v)}\"")
            .Concat(values.Select(v => $"\"{Enum.GetName(@enum, v)}\": {v}")));
        bld.Append($"{Br}{@enum.Name}: {{ {fields} }}");
    }

    private void EmitImporter (InstancedMeta it)
    {
        var evt = it.Members.OfType<EventMeta>().ToArray();
        bld.Append(
            $$"""
              function {{it.Importer}}(instance) {
                  return instances.import(instance, _id => {
                      {{Fmt(evt.Select(e => $"instance.{e.JSName}.subscribe(handle{e.Name});"))}}
                      return () => {
                          {{Fmt(evt.Select(e => $"instance.{e.JSName}.unsubscribe(handle{e.Name});"), 2)}}
                      };

                      {{Fmt(evt.Select(e => {
                          var fnName = $"{it.FullName.Replace('.', '_')}_Invoke{e.Name}";
                          var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
                          var args = string.Join(", ", e.Arguments.Select(a => a.JSName));
                          var invArgs = PrependIdArg(string.Join(", ", e.Arguments.Select(ExportJS)));
                          return $"function handle{e.Name}({args}) {{ {invName}({invArgs}); }}";
                      }))}}
                  });
              }
              """
        );
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Arguments.Any(a => a.Value.IsSerialized || a.Value.IsInstanced) ||
               method.Return.IsSerialized || method.Return.IsInstanced;
    }

    private string Br => $"{Comma}\n{Pad(level + 1)}";
    private string Pad (int level) => new(' ', level * 4);
    private string Comma => bld[^1] == '{' ? "" : ",";
}
