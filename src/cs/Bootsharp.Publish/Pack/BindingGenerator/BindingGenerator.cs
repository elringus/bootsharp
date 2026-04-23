using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (Preferences prefs, bool debug)
{
    private record Binding (MemberMeta? Member, Type? Enum, string Namespace);

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];

    private readonly StringBuilder builder = new();
    private IReadOnlyCollection<InterfaceMeta> instanced = [];
    private Binding[] bindings = [];
    private int index, level;

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        bindings = inspection.StaticMembers
            .Select(m => new Binding(m, null, m.JSSpace))
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Members
                .Select(m => new Binding(m, null, m.JSSpace))))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Members
                .Select(m => new Binding(m, null, m.JSSpace))))
            .Concat(inspection.Serialized.Where(t => t.Type.IsEnum)
                .Select(t => new Binding(null, t.Type, BuildJSSpace(t.Type, prefs))))
            .OrderBy(m => m.Namespace).ToArray();
        if (bindings.Length == 0) return "";

        EmitImports();
        builder.Append("\n\n");

        if (debug)
        {
            EmitDebugHelpers();
            builder.Append("\n\n");
        }

        EmitHelpers();
        builder.Append("\n\n");

        builder.Append(new BindingSerializerGenerator().Generate(inspection.Serialized));
        builder.Append("\n\n");

        foreach (var instance in inspection.InstancedInterfaces
                     .Where(i => i.Interop == InteropKind.Import && i.Members.OfType<EventMeta>().Any()))
            EmitInstanceRegistrar(instance);
        builder.Append("\n\n");

        if (inspection.InstancedInterfaces.Count > 0)
            builder.Append(new BindingClassGenerator().Generate(inspection.InstancedInterfaces));

        for (index = 0; index < bindings.Length; index++)
            EmitBinding();

        return builder.ToString();
    }

    private void EmitImports ()
    {
        builder.Append(
            """
            import { exports } from "./exports";
            import { Event } from "./event";
            import { instances } from "./instances";
            import { serialize, deserialize, binary, types } from "./serialization";
            """
        );
    }

    private void EmitDebugHelpers ()
    {
        builder.Append(
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
        builder.Append(
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
        return prevBinding.Namespace != binding.Namespace;
    }

    private void OpenNamespace ()
    {
        level = 0;
        var prevParts = prevBinding?.Namespace.Split('.') ?? [];
        var parts = binding.Namespace.Split('.');
        while (prevParts.ElementAtOrDefault(level) == parts[level]) level++;
        for (var i = level; i < parts.Length; level = i, i++)
            if (i == 0) builder.Append($"\nexport const {parts[i]} = {{");
            else builder.Append($"{Comma}\n{Pad(i)}{parts[i]}: {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextBinding is null) return true;
        return nextBinding.Namespace != binding.Namespace;
    }

    private void CloseNamespace ()
    {
        var target = GetCloseLevel();
        for (; level >= target; level--)
            if (level == 0) builder.Append("\n};");
            else builder.Append($"\n{Pad(level)}}}");

        int GetCloseLevel ()
        {
            if (nextBinding is null) return 0;
            var closeLevel = 0;
            var parts = binding.Namespace.Split('.');
            var nextParts = nextBinding.Namespace.Split('.');
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
        var inst = TryInstanced(evt, out var instance);
        var name = $"broadcast{evt.Name}Serialized";
        var args = string.Join(", ", evt.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", evt.Arguments.Select(arg =>
            // By default, we use 'null' for missing collection items, but here the event args array
            // represents args specified to the event's 'broadcast' function, so user expects 'undefined'.
            $"{Deserialize(arg)}{(arg.Value.Nullable ? " ?? undefined" : "")}"));
        if (inst)
        {
            var invName = $"instances.export(_id, id => new {instance!.JSName}(id)).broadcast{evt.Name}";
            builder.Append($"{Br}{name}({PrependIdArg(args)}) {{ {invName}({invArgs}); }}");
        }
        else
        {
            var invName = $"{evt.JSSpace}.{evt.JSName}.broadcast";
            builder.Append($"{Br}{evt.JSName}: new Event()");
            builder.Append($"{Br}{name}: ({args}) => {invName}({invArgs})");
        }
    }

    private void EmitEventImport (EventMeta evt)
    {
        if (TryInstanced(evt, out _)) return; // instanced import event handlers are emitted in the registrar
        var name = $"{evt.Space.Replace('.', '_')}_Invoke{evt.Name}";
        var invName = debug ? $"""getExport("{name}")""" : $"exports.{name}";
        var args = string.Join(", ", evt.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", evt.Arguments.Select(Serialize));
        builder.Append($"{Br}{evt.JSName}: importEvent(({args}) => {invName}({invArgs}))");
    }

    private void EmitPropertyExport (PropertyMeta prop)
    {
        var inst = TryInstanced(prop, out _);
        if (prop.CanGet)
        {
            var fnName = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var body = Deserialize(prop.Value, inst ? $"{invName}(_id)" : $"{invName}()");
            if (prop.Value.Nullable && !prop.Value.IsInstance) body += " ?? undefined";
            if (inst) builder.Append($"{Br}getProperty{prop.Name}(_id) {{ return {body}; }}");
            else builder.Append($"{Br}get {prop.JSName}() {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            var fnName = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var value = Serialize(prop.Value, "value");
            var body = inst ? $"{invName}(_id, {value})" : $"{invName}({value})";
            if (inst) builder.Append($"{Br}setProperty{prop.Name}(_id, value) {{ {body}; }}");
            else builder.Append($"{Br}set {prop.JSName}(value) {{ {body}; }}");
        }
    }

    private void EmitPropertyImport (PropertyMeta prop)
    {
        var inst = TryInstanced(prop, out _);
        if (prop.CanGet)
        {
            if (!inst) builder.Append($"{Br}get {prop.JSName}() {{ return this._{prop.JSName}; }}");
            var args = inst ? "_id" : "";
            var body = Serialize(prop.Value, inst ? $"instances.imported(_id).{prop.JSName}" : $"this.{prop.JSName}");
            builder.Append($"{Br}getProperty{prop.Name}Serialized({args}) {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            if (!inst) builder.Append($"{Br}set {prop.JSName}(value) {{ this._{prop.JSName} = value; }}");
            var value = Deserialize(prop.Value, "value");
            var args = inst ? "_id, value" : "value";
            var body = inst ? $"instances.imported(_id).{prop.JSName} = {value}" : $"this.{prop.JSName} = {value}";
            builder.Append($"{Br}setProperty{prop.Name}Serialized({args}) {{ {body}; }}");
        }
    }

    private void EmitMethodExport (MethodMeta method)
    {
        var inst = TryInstanced(method, out _);
        var wait = ShouldWait(method);
        var fnName = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
        var args = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (inst) args = PrependIdArg(args);
        var invArgs = string.Join(", ", method.Arguments.Select(Serialize));
        if (inst) invArgs = PrependIdArg(invArgs);
        var body = Deserialize(method.Value, $"{(wait ? "await " : "")}{invName}({invArgs})");
        builder.Append($"{Br}{method.JSName}: {(wait ? "async " : "")}({args}) => {body}");
    }

    private void EmitMethodImport (MethodMeta method)
    {
        var inst = TryInstanced(method, out _);
        var wait = ShouldWait(method);
        var name = method.JSName;
        var args = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (inst) args = PrependIdArg(args);
        var invArgs = string.Join(", ", method.Arguments.Select(Deserialize));
        var invName = inst ? $"instances.imported(_id).{name}" : $"this.{name}Handler";
        var body = Serialize(method.Value, $"{(wait ? "await " : "")}{invName}({invArgs})");
        var serdeHandler = $"{(wait ? "async " : "")}({args}) => {body}";
        if (inst) builder.Append($"{Br}{name}Serialized: {serdeHandler}");
        else
        {
            var serde = $"this.{name}SerializedHandler";
            var serdeExp = debug ? $"getImport({invName}, {serde}, \"{binding.Namespace}.{name}\")" : serde;
            builder.Append($"{Br}get {name}() {{ return {invName}; }}");
            builder.Append($"{Br}set {name}(handler) {{ {invName} = handler; {serde} = {serdeHandler}; }}");
            builder.Append($"{Br}get {name}Serialized() {{ return {serdeExp}; }}");
        }
    }

    private void EmitEnum (Type @enum)
    {
        var values = Enum.GetValuesAsUnderlyingType(@enum).Cast<object>().ToArray();
        var fields = string.Join(", ", values
            .Select(v => $"\"{v}\": \"{Enum.GetName(@enum, v)}\"")
            .Concat(values.Select(v => $"\"{Enum.GetName(@enum, v)}\": {v}")));
        builder.Append($"{Br}{@enum.Name}: {{ {fields} }}");
    }

    private void EmitInstanceRegistrar (InterfaceMeta instance)
    {
        var events = instance.Members.OfType<EventMeta>().ToArray();
        builder.Append(
            $$"""
              function {{BuildRegistrarName(instance)}}(instance) {
                  return instances.import(instance, _id => {
                      {{Fmt(events.Select(e => $"instance.{e.JSName}.subscribe(handle{e.Name});"))}}
                      return () => {
                          {{Fmt(events.Select(e => $"instance.{e.JSName}.unsubscribe(handle{e.Name});"), 2)}}
                      };

                      {{Fmt(events.Select(e => {
                          var fnName = $"{e.Space.Replace('.', '_')}_Invoke{e.Name}";
                          var invName = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
                          var args = string.Join(", ", e.Arguments.Select(a => a.JSName));
                          var invArgs = PrependIdArg(string.Join(", ", e.Arguments.Select(Serialize)));
                          return $"function handle{e.Name}({args}) {{ {invName}({invArgs}); }}";
                      }))}}
                  });
              }
              """
        );
    }

    private string Serialize (ArgumentMeta arg) => Serialize(arg.Value, arg.JSName);
    private string Serialize (ValueMeta value, string exp)
    {
        if (value.IsInstance) return RegisterInstance(value, exp);
        if (value.IsSerialized) return $"serialize({exp}, {value.Serialized.Id})";
        return exp;
    }

    private string Deserialize (ArgumentMeta arg) => Deserialize(arg.Value, arg.JSName);
    private string Deserialize (ValueMeta value, string exp)
    {
        if (value.InstanceType is { } it)
        {
            var instance = instanced.First(i => i.Type == it);
            if (instance.Interop == InteropKind.Import) return $"instances.imported({exp})";
            return $"instances.export({exp}, id => new {instance.JSName}(id))";
        }
        if (value.IsSerialized) return $"deserialize({exp}, {value.Serialized.Id})";
        return exp;
    }

    private string RegisterInstance (ValueMeta value, string exp)
    {
        var it = instanced.First(i => i.Type == value.InstanceType);
        if (it.Interop == InteropKind.Export) return $"{exp}._id";
        if (it.Members.OfType<EventMeta>().Any()) return $"{BuildRegistrarName(it)}({exp})";
        return $"instances.import({exp})";
    }

    private static string BuildRegistrarName (InterfaceMeta it)
    {
        return $"register_{it.Type.FullName!.Replace('.', '_').Replace('+', '_')}";
    }

    private bool TryInstanced (MemberMeta member, [NotNullWhen(true)] out InterfaceMeta? instance)
    {
        instance = instanced.FirstOrDefault(i => i.Members.Contains(member));
        return instance is not null;
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Arguments.Any(a => a.Value.IsSerialized || a.Value.IsInstance) ||
               method.Value.IsSerialized || method.Value.IsInstance;
    }

    private string Br => $"{Comma}\n{Pad(level + 1)}";
    private string Pad (int level) => new(' ', level * 4);
    private string Comma => builder[^1] == '{' ? "" : ",";
}
