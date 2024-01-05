using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (NamespaceBuilder spaceBuilder)
{
    private readonly StringBuilder builder = new();

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];

    private Binding[] bindings = null!;
    private int index, level;

    public string Generate (AssemblyInspection inspection)
    {
        bindings = inspection.Methods
            .Select(m => new Binding(m, null, m.JSSpace))
            .Concat(inspection.Types.Where(t => t.IsEnum)
                .Select(t => new Binding(null, t, spaceBuilder.Build(t))))
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
        level = binding.Namespace.Count(c => c == '.');
        var prevParts = prevBinding?.Namespace.Split('.') ?? Array.Empty<string>();
        var parts = binding.Namespace.Split('.');
        for (var i = 0; i < parts.Length; i++)
            if (prevParts.ElementAtOrDefault(i) == parts[i]) continue;
            else if (i == 0) builder.Append($"\nexport const {parts[i]} = {{");
            else builder.Append($"{Comma()}\n{Pad(i)}{parts[i]}: {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextBinding is null) return true;
        return nextBinding.Namespace != binding.Namespace;
    }

    private void CloseNamespace ()
    {
        var target = nextBinding is null ? 0 : nextBinding.Namespace.Count(c => c == '.');
        for (; level >= target; level--)
            if (level == 0) builder.Append("\n};");
            else builder.Append($"\n{Pad(level)}}}");
    }

    private void EmitMethod (MethodMeta method)
    {
        if (method.Type == MethodType.Invokable) EmitInvokable(method);
        else if (method.Type == MethodType.Function) EmitFunction(method);
        else EmitEvent(method);
    }

    private void EmitInvokable (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var endpoint = $"getExports().{method.Space.Replace('.', '_')}.{method.Name}";
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", method.Arguments.Select(arg =>
            arg.Value.Serialized ? $"serialize({arg.JSName})" : arg.JSName
        ));
        var body = $"{(wait ? "await " : "")}{endpoint}({invArgs})";
        if (method.ReturnValue.Serialized) body = $"deserialize({body})";
        var func = $"{(wait ? "async " : "")}({funcArgs}) => {body}";
        builder.Append($"{Comma()}\n{Pad(level + 1)}{method.JSName}: {func}");
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
        builder.Append($"{Comma()}\n{Pad(level + 1)}get {name}() {{ return this.{name}Handler; }}");
        builder.Append($"{Comma()}\n{Pad(level + 1)}set {name}(handler) {{ {set} }}");
        builder.Append($"{Comma()}\n{Pad(level + 1)}get {name}Serialized() {{ {serde} }}");
    }

    private void EmitEvent (MethodMeta method)
    {
        var name = method.JSName;
        builder.Append($"{Comma()}\n{Pad(level + 1)}{name}: new Event()");
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        var invArgs = string.Join(", ", method.Arguments.Select(arg => arg.Value.Serialized ? $"deserialize({arg.JSName})" : arg.JSName));
        builder.Append($"{Comma()}\n{Pad(level + 1)}{name}Serialized: ({funcArgs}) => {method.JSSpace}.{name}.broadcast({invArgs})");
    }

    private void EmitEnum (Type @enum)
    {
        var values = Enum.GetValuesAsUnderlyingType(@enum).Cast<object>().ToArray();
        var fields = string.Join(", ", values
            .Select(v => $"\"{v}\": \"{Enum.GetName(@enum, v)}\"")
            .Concat(values.Select(v => $"\"{Enum.GetName(@enum, v)}\": {v}")));
        builder.Append($"{Comma()}\n{Pad(level + 1)}{@enum.Name}: {{ {fields} }}");
    }

    private string Pad (int level) => new(' ', level * 4);
    private string Comma () => builder[^1] == '{' ? "" : ",";
    private bool ShouldWait (MethodMeta method) =>
        (method.Arguments.Any(a => a.Value.Serialized) ||
         method.ReturnValue.Serialized) && method.ReturnValue.Async;
}
