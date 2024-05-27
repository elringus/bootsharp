using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (Preferences prefs)
{
    private record Binding (MethodMeta? Method, Type? Enum, string Namespace);

    private readonly StringBuilder builder = new();
    private readonly BindingMarshaler marshaler = new();
    private readonly BindingClassGenerator classGenerator = new();
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];

    private Binding[] bindings = null!;
    private int index, level;

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        bindings = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods))
            .Select(m => new Binding(m, null, m.JSSpace))
            .Concat(inspection.Crawled.Where(t => t.IsEnum)
                .Select(t => new Binding(null, t, BuildJSSpace(t, prefs))))
            .OrderBy(m => m.Namespace).ToArray();
        if (bindings.Length == 0) return "";
        EmitImports();
        builder.Append("\n\n");
        if (inspection.InstancedInterfaces.Count > 0)
            builder.Append(classGenerator.Generate(inspection.InstancedInterfaces));
        for (index = 0; index < bindings.Length; index++)
            EmitBinding();
        builder.Append("\n\n");
        foreach (var marshalFn in marshaler.GetGenerated())
            builder.Append(marshalFn + "\n");
        return builder.ToString();
    }

    private void EmitImports () => builder.Append(
        """
        import { exports } from "./exports";
        import { Event } from "./event";
        import { registerInstance, getInstance, disposeOnFinalize } from "./instances";

        function getExports() { if (exports == null) throw Error("Boot the runtime before invoking C# APIs."); return exports; }

        /* v8 ignore start */
        """
    );

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
        var endpoint = $"getExports().{method.Space.Replace('.', '_')}_{method.Name}";
        var fnArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) fnArgs = PrependInstanceIdArgName(fnArgs);
        var invArgs = string.Join(", ", method.Arguments.Select(BuildInvArg));
        if (instanced) invArgs = PrependInstanceIdArgName(invArgs);
        var body = $"{(wait ? "await " : "")}{endpoint}({invArgs})";
        if (method.ReturnValue.Instance) body = $"new {BuildInstanceClassName(method.ReturnValue.InstanceType)}({body})";
        else if (method.ReturnValue.Marshaled) body = $"{marshaler.Unmarshal(method.ReturnValue.Type)}({body})";
        var fn = $"{(wait ? "async " : "")}({fnArgs}) => {body}";
        builder.Append($"{Break()}{method.JSName}: {fn}");

        string BuildInvArg (ArgumentMeta arg)
        {
            if (arg.Value.Instance) return $"registerInstance({arg.JSName})";
            if (arg.Value.Marshaled) return $"{marshaler.Marshal(arg.Value.Type)}({arg.JSName})";
            return arg.JSName;
        }
    }

    private void EmitFunction (MethodMeta method)
    {
        var instanced = IsInstanced(method);
        var wait = ShouldWait(method);
        var name = method.JSName;
        var fnArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) fnArgs = PrependInstanceIdArgName(fnArgs);
        var invArgs = string.Join(", ", method.Arguments.Select(BuildInvArg));
        var handler = instanced ? $"getInstance(_id).{name}" : $"this.{name}Handler";
        var body = $"{(wait ? "await " : "")}{handler}({invArgs})";
        if (method.ReturnValue.Instance) body = $"registerInstance({body})";
        else if (method.ReturnValue.Marshaled) body = $"{marshaler.Marshal(method.ReturnValue.Type)}({body})";
        var serdeHandler = $"{(wait ? "async " : "")}({fnArgs}) => {body}";
        if (instanced) builder.Append($"{Break()}{name}Marshaled: {serdeHandler}");
        else
        {
            var set = $"{handler} = handler; this.{name}MarshaledHandler = {serdeHandler};";
            var error = $"""throw Error("Failed to invoke '{binding.Namespace}.{name}' from C#. Make sure to assign function in JavaScript.")""";
            var serde = $"""if (typeof {handler} !== "function") {error}; return this.{name}MarshaledHandler;""";
            builder.Append($"{Break()}get {name}() {{ return {handler}; }}");
            builder.Append($"{Break()}set {name}(handler) {{ {set} }}");
            builder.Append($"{Break()}get {name}Marshaled() {{ {serde} }}");
        }

        string BuildInvArg (ArgumentMeta arg)
        {
            if (arg.Value.Instance) return $"new {BuildInstanceClassName(arg.Value.InstanceType)}({arg.JSName})";
            if (arg.Value.Marshaled) return $"{marshaler.Unmarshal(arg.Value.Type)}({arg.JSName})";
            return arg.JSName;
        }
    }

    private void EmitEvent (MethodMeta method)
    {
        var instanced = IsInstanced(method);
        var name = method.JSName;
        if (!instanced) builder.Append($"{Break()}{name}: new Event()");
        var fnArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) fnArgs = PrependInstanceIdArgName(fnArgs);
        var invArgs = string.Join(", ", method.Arguments.Select(arg => arg.Value.Marshaled
            ? $"{marshaler.Unmarshal(arg.Value.Type)}({arg.JSName})" : arg.JSName));
        var handler = instanced ? "getInstance(_id)" : method.JSSpace;
        builder.Append($"{Break()}{name}Marshaled: ({fnArgs}) => {handler}.{name}.broadcast({invArgs})");
    }

    private void EmitEnum (Type @enum)
    {
        var values = Enum.GetValuesAsUnderlyingType(@enum).Cast<object>().ToArray();
        var fields = string.Join(", ", values
            .Select(v => $"\"{v}\": \"{Enum.GetName(@enum, v)}\"")
            .Concat(values.Select(v => $"\"{Enum.GetName(@enum, v)}\": {v}")));
        builder.Append($"{Break()}{@enum.Name}: {{ {fields} }}");
    }

    private bool ShouldWait (MethodMeta method) =>
        (method.Arguments.Any(a => a.Value.Marshaled || a.Value.Instance) ||
         method.ReturnValue.Marshaled || method.ReturnValue.Instance) && method.ReturnValue.Async;

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
