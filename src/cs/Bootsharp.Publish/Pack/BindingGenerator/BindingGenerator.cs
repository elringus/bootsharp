using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (Preferences prefs, bool debug)
{
    private record Binding (MethodMeta? Method, Type? Enum, string Namespace);

    private readonly StringBuilder builder = new();
    private readonly BindingClassGenerator classGenerator = new();
    private readonly BindingSerializerGenerator serdeGenerator = new();
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];

    private Binding[] bindings = null!;
    private int index, level;

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        var methods = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods))
            .ToArray();
        bindings = methods
            .Select(m => new Binding(m, null, m.JSSpace))
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

        builder.Append(serdeGenerator.Generate(inspection.Serialized));
        builder.Append('\n');

        if (inspection.InstancedInterfaces.Count > 0)
            builder.Append(classGenerator.Generate(inspection.InstancedInterfaces));
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
            import { registerInstance, getInstance, disposeOnFinalize } from "./instances";
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

    private void EmitBinding ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (binding.Method != null) EmitMethod(binding.Method);
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
            else builder.Append($"{Comma()}\n{Pad(i)}{parts[i]}: {{");
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

    private void EmitMethod (MethodMeta method)
    {
        if (method.Kind == MethodKind.Invokable) EmitInvokable(method);
        else if (method.Kind == MethodKind.Function) EmitFunction(method);
        else EmitEvent(method);
    }

    private void EmitInvokable (MethodMeta method)
    {
        var instanced = IsInstanced(method);
        var wait = ShouldWait(method);
        var fn = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var endpoint = debug ? $"""getExport("{fn}")""" : $"exports.{fn}";
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) funcArgs = PrependInstanceIdArgName(funcArgs);
        var invArgs = string.Join(", ", method.Arguments.Select(BuildInvArg));
        if (instanced) invArgs = PrependInstanceIdArgName(invArgs);
        var body = $"{(wait ? "await " : "")}{endpoint}({invArgs})";
        if (method.ReturnValue.InstanceType is { } itp) body = $"new {BuildInstanceClassName(itp)}({body})";
        else if (method.ReturnValue.IsSerialized) body = $"deserialize({body}, {method.ReturnValue.Serialized.Id})";
        var func = $"{(wait ? "async " : "")}({funcArgs}) => {body}";
        builder.Append($"{Break()}{method.JSName}: {func}");

        string BuildInvArg (ArgumentMeta arg)
        {
            if (arg.Value.IsInstance) return $"registerInstance({arg.JSName})";
            if (arg.Value.IsSerialized) return $"serialize({arg.JSName}, {arg.Value.Serialized.Id})";
            return arg.JSName;
        }
    }

    private void EmitFunction (MethodMeta method)
    {
        var instanced = IsInstanced(method);
        var wait = ShouldWait(method);
        var name = method.JSName;
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) funcArgs = PrependInstanceIdArgName(funcArgs);
        var invArgs = string.Join(", ", method.Arguments.Select(BuildInvArg));
        var handler = instanced ? $"getInstance(_id).{name}" : $"this.{name}Handler";
        var body = $"{(wait ? "await " : "")}{handler}({invArgs})";
        if (method.ReturnValue.IsInstance) body = $"registerInstance({body})";
        else if (method.ReturnValue.IsSerialized) body = $"serialize({body}, {method.ReturnValue.Serialized.Id})";
        var serdeHandler = $"{(wait ? "async " : "")}({funcArgs}) => {body}";
        if (instanced) builder.Append($"{Break()}{name}Serialized: {serdeHandler}");
        else
        {
            var serde = $"this.{name}SerializedHandler";
            var serdeExp = debug ? $"getImport({handler}, {serde}, \"{binding.Namespace}.{name}\")" : serde;
            builder.Append($"{Break()}get {name}() {{ return {handler}; }}");
            builder.Append($"{Break()}set {name}(handler) {{ {handler} = handler; {serde} = {serdeHandler}; }}");
            builder.Append($"{Break()}get {name}Serialized() {{ return {serdeExp}; }}");
        }

        string BuildInvArg (ArgumentMeta arg)
        {
            if (arg.Value.IsInstance) return $"new {BuildInstanceClassName(arg.Value.InstanceType)}({arg.JSName})";
            if (arg.Value.IsSerialized) return $"deserialize({arg.JSName}, {arg.Value.Serialized.Id})";
            return arg.JSName;
        }
    }

    private void EmitEvent (MethodMeta method)
    {
        var instanced = IsInstanced(method);
        var name = method.JSName;
        if (!instanced) builder.Append($"{Break()}{name}: new Event()");
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) funcArgs = PrependInstanceIdArgName(funcArgs);
        var evtArgs = string.Join(", ", method.Arguments.Select(BuildEvtArg));
        var handler = instanced ? "getInstance(_id)" : method.JSSpace;
        builder.Append($"{Break()}{name}Serialized: ({funcArgs}) => {handler}.{name}.broadcast({evtArgs})");

        string BuildEvtArg (ArgumentMeta arg)
        {
            if (!arg.Value.IsSerialized) return arg.JSName;
            // By default, we use 'null' for missing collection items, but here the event args array
            // represents args specified to the event's 'broadcast' function, so user expects 'undefined'.
            var toUndefined = arg.Value.Nullable ? " ?? undefined" : "";
            return $"deserialize({arg.JSName}, {arg.Value.Serialized.Id}){toUndefined}";
        }
    }

    private void EmitEnum (Type @enum)
    {
        var values = Enum.GetValuesAsUnderlyingType(@enum).Cast<object>().ToArray();
        var fields = string.Join(", ", values
            .Select(v => $"\"{v}\": \"{Enum.GetName(@enum, v)}\"")
            .Concat(values.Select(v => $"\"{Enum.GetName(@enum, v)}\": {v}")));
        builder.Append($"{Break()}{@enum.Name}: {{ {fields} }}");
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Arguments.Any(a => a.Value.IsSerialized || a.Value.IsInstance) ||
               method.ReturnValue.IsSerialized || method.ReturnValue.IsInstance;
    }

    private string Break () => $"{Comma()}\n{Pad(level + 1)}";
    private string Pad (int level) => new(' ', level * 4);
    private string Comma () => builder[^1] == '{' ? "" : ",";

    private string BuildInstanceClassName (Type instanceType)
    {
        var instance = instanced.First(i => i.Type == instanceType);
        return BuildJSInteropInstanceClassName(instance);
    }

    private bool IsInstanced (MethodMeta method)
    {
        return instanced.Any(i => i.Methods.Contains(method));
    }
}
