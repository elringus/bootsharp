using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (Preferences prefs)
{
    private record Binding (MethodMeta? Method, Type? Enum, string Namespace);

    private readonly StringBuilder builder = new();

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];

    private Binding[] bindings = null!;
    private int index, level;

    public string Generate (SolutionInspection inspection)
    {
        bindings = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods))
            .Select(m => new Binding(m, null, m.JSSpace))
            .Concat(inspection.Crawled.Where(t => t.IsEnum)
                .Select(t => new Binding(null, t, BuildJSSpace(t, prefs))))
            .OrderBy(m => m.Namespace).ToArray();
        if (bindings.Length == 0) return "";
        EmitImports();
        for (index = 0; index < bindings.Length; index++)
            EmitBinding();
        return builder.ToString();
    }

    private void EmitImports ()
    {
        builder.Append("import { exports } from \"./exports\";\n");
        builder.Append("import { Event } from \"./event\";\n");
        builder.Append("import { getInstanceId, getInstance } from \"./instances\";\n");
        builder.Append("function getExports () { if (exports == null) throw Error(\"Boot the runtime before invoking C# APIs.\"); return exports; }\n");
        builder.Append("function serialize(obj) { return JSON.stringify(obj); }\n");
        builder.Append("function deserialize(json) { const result = JSON.parse(json); if (result === null) return undefined; return result; }\n");
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
        var wait = ShouldWait(method);
        var endpoint = $"getExports().{method.Space.Replace('.', '_')}_{method.Name}";
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", method.Arguments.Select(arg =>
            arg.Value.Serialized ? $"serialize({arg.JSName})" : arg.JSName
        ));
        var body = $"{(wait ? "await " : "")}{endpoint}({invArgs})";
        if (method.ReturnValue.Serialized) body = $"deserialize({body})";
        var func = $"{(wait ? "async " : "")}({funcArgs}) => {body}";
        builder.Append($"{Break()}{method.JSName}: {func}");
    }

    private void EmitFunction (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var name = method.JSName;
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", method.Arguments.Select(arg =>
            arg.Value.Serialized ? $"deserialize({arg.JSName})" : arg.JSName
        ));
        var body = $"{(wait ? "await " : "")}this.{name}Handler({invArgs})";
        if (method.ReturnValue.Serialized) body = $"serialize({body})";
        var set = $"this.{name}Handler = handler; this.{name}SerializedHandler = {(wait ? "async " : "")}({funcArgs}) => {body};";
        var error = $"throw Error(\"Failed to invoke '{binding.Namespace}.{name}' from C#. Make sure to assign function in JavaScript.\")";
        var serde = $"if (typeof this.{name}Handler !== \"function\") {error}; return this.{name}SerializedHandler;";
        builder.Append($"{Break()}get {name}() {{ return this.{name}Handler; }}");
        builder.Append($"{Break()}set {name}(handler) {{ {set} }}");
        builder.Append($"{Break()}get {name}Serialized() {{ {serde} }}");
    }

    private void EmitEvent (MethodMeta method)
    {
        var name = method.JSName;
        builder.Append($"{Break()}{name}: new Event()");
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", method.Arguments.Select(arg => arg.Value.Serialized ? $"deserialize({arg.JSName})" : arg.JSName));
        builder.Append($"{Break()}{name}Serialized: ({funcArgs}) => {method.JSSpace}.{name}.broadcast({invArgs})");
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
        (method.Arguments.Any(a => a.Value.Serialized) ||
         method.ReturnValue.Serialized) && method.ReturnValue.Async;

    private string Break () => $"{Comma()}\n{Pad(level + 1)}";
    private string Pad (int level) => new(' ', level * 4);
    private string Comma () => builder[^1] == '{' ? "" : ",";
}
