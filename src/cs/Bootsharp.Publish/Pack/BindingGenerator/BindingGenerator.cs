using System.Text;

namespace Bootsharp.Publish;

internal sealed class BindingGenerator (Preferences prefs, bool debug)
{
    private record Binding (MemberMeta? Member, Type? Enum, string Namespace);

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
        bindings = inspection.StaticMethods
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
            case EventMeta e: EmitEvent(e); break;
            case PropertyMeta { Interop: InteropKind.Export } p: EmitPropertyExport(p); break;
            case PropertyMeta { Interop: InteropKind.Import } p: EmitPropertyImport(p); break;
            case MethodMeta { Interop: InteropKind.Export } m: EmitMethodExport(m); break;
            case MethodMeta { Interop: InteropKind.Import } m: EmitMethodImport(m); break;
        }
    }

    private void EmitPropertyExport (PropertyMeta prop)
    {
        var instanced = this.instanced.Any(i => i.Members.Contains(prop));
        if (prop.CanGet)
        {
            var fnName = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var endpoint = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var body = instanced ? $"{endpoint}(_id)" : $"{endpoint}()";
            if (prop.Value.InstanceType is { } it)
                body = $"new {BuildJSInteropInstanceClassName(this.instanced.First(i => i.Type == it))}({body})";
            else if (prop.Value.IsSerialized) body = $"deserialize({body}, {prop.Value.Serialized.Id})";
            if (instanced) builder.Append($"{Br}getProperty{prop.Name}(_id) {{ return {body}; }}");
            else builder.Append($"{Br}get {prop.JSName}() {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            var fnName = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var endpoint = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
            var value = prop.Value.IsInstance ? "registerInstance(value)" :
                prop.Value.IsSerialized ? $"serialize(value, {prop.Value.Serialized.Id})" : "value";
            var body = instanced ? $"{endpoint}(_id, {value})" : $"{endpoint}({value})";
            if (instanced) builder.Append($"{Br}setProperty{prop.Name}(_id, value) {{ {body}; }}");
            else builder.Append($"{Br}set {prop.JSName}(value) {{ {body}; }}");
        }
    }

    private void EmitPropertyImport (PropertyMeta prop)
    {
        var instanced = this.instanced.Any(i => i.Members.Contains(prop));
        if (prop.CanGet)
        {
            if (!instanced) builder.Append($"{Br}get {prop.JSName}() {{ return this._{prop.JSName}; }}");
            var args = instanced ? "_id" : "";
            var body = instanced ? $"getInstance(_id).{prop.JSName}" : $"this.{prop.JSName}";
            if (prop.Value.IsInstance) body = $"registerInstance({body})";
            else if (prop.Value.IsSerialized) body = $"serialize({body}, {prop.Value.Serialized.Id})";
            builder.Append($"{Br}getProperty{prop.Name}Serialized({args}) {{ return {body}; }}");
        }
        if (prop.CanSet)
        {
            if (!instanced) builder.Append($"{Br}set {prop.JSName}(value) {{ this._{prop.JSName} = value; }}");
            var value = prop.Value.InstanceType is { } it
                ? $"new {BuildJSInteropInstanceClassName(this.instanced.First(i => i.Type == it))}(value)"
                : prop.Value.IsSerialized ? $"deserialize(value, {prop.Value.Serialized.Id})" : "value";
            var args = instanced ? "_id, value" : "value";
            var body = instanced ? $"getInstance(_id).{prop.JSName} = {value}" : $"this.{prop.JSName} = {value}";
            builder.Append($"{Br}setProperty{prop.Name}Serialized({args}) {{ {body}; }}");
        }
    }

    private void EmitEvent (EventMeta method)
    {
        var instanced = this.instanced.Any(i => i.Members.Contains(method));
        var name = method.JSName;
        if (!instanced) builder.Append($"{Br}{name}: new Event()");
        var sigArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) sigArgs = PrependInstanceIdArgName(sigArgs);
        var callArgs = string.Join(", ", method.Arguments.Select(BuildCallArg));
        var handler = instanced ? "getInstance(_id)" : method.JSSpace;
        builder.Append($"{Br}{name}Serialized: ({sigArgs}) => {handler}.{name}.broadcast({callArgs})");

        string BuildCallArg (ArgumentMeta arg)
        {
            if (!arg.Value.IsSerialized) return arg.JSName;
            // By default, we use 'null' for missing collection items, but here the event args array
            // represents args specified to the event's 'broadcast' function, so user expects 'undefined'.
            var toUndefined = arg.Value.Nullable ? " ?? undefined" : "";
            return $"deserialize({arg.JSName}, {arg.Value.Serialized.Id}){toUndefined}";
        }
    }

    private void EmitMethodExport (MethodMeta method)
    {
        var instanced = this.instanced.Any(i => i.Members.Contains(method));
        var wait = ShouldWait(method);
        var fnName = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var endpoint = debug ? $"""getExport("{fnName}")""" : $"exports.{fnName}";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) sigArgs = PrependInstanceIdArgName(sigArgs);
        var callArgs = string.Join(", ", method.Arguments.Select(BuildCallArg));
        if (instanced) callArgs = PrependInstanceIdArgName(callArgs);
        var body = $"{(wait ? "await " : "")}{endpoint}({callArgs})";
        if (method.Value.InstanceType is { } it)
            body = $"new {BuildJSInteropInstanceClassName(this.instanced.First(i => i.Type == it))}({body})";
        else if (method.Value.IsSerialized) body = $"deserialize({body}, {method.Value.Serialized.Id})";
        builder.Append($"{Br}{method.JSName}: {(wait ? "async " : "")}({sigArgs}) => {body}");

        string BuildCallArg (ArgumentMeta arg)
        {
            var name = arg.JSName;
            if (arg.Value.IsInstance) name = $"registerInstance({name})";
            else if (arg.Value.IsSerialized) name = $"serialize({name}, {arg.Value.Serialized.Id})";
            return name;
        }
    }

    private void EmitMethodImport (MethodMeta method)
    {
        var instanced = this.instanced.Any(i => i.Members.Contains(method));
        var wait = ShouldWait(method);
        var fnName = method.JSName;
        var sigArgs = string.Join(", ", method.Arguments.Select(a => a.JSName));
        if (instanced) sigArgs = PrependInstanceIdArgName(sigArgs);
        var callArgs = string.Join(", ", method.Arguments.Select(BuildCallArg));
        var handler = instanced ? $"getInstance(_id).{fnName}" : $"this.{fnName}Handler";
        var body = $"{(wait ? "await " : "")}{handler}({callArgs})";
        if (method.Value.IsInstance) body = $"registerInstance({body})";
        else if (method.Value.IsSerialized) body = $"serialize({body}, {method.Value.Serialized.Id})";
        var serdeHandler = $"{(wait ? "async " : "")}({sigArgs}) => {body}";
        if (instanced) builder.Append($"{Br}{fnName}Serialized: {serdeHandler}");
        else
        {
            var serde = $"this.{fnName}SerializedHandler";
            var serdeExp = debug ? $"getImport({handler}, {serde}, \"{binding.Namespace}.{fnName}\")" : serde;
            builder.Append($"{Br}get {fnName}() {{ return {handler}; }}");
            builder.Append($"{Br}set {fnName}(handler) {{ {handler} = handler; {serde} = {serdeHandler}; }}");
            builder.Append($"{Br}get {fnName}Serialized() {{ return {serdeExp}; }}");
        }

        string BuildCallArg (ArgumentMeta arg)
        {
            var name = arg.JSName;
            if (arg.Value.InstanceType is { } it)
                name = $"new {BuildJSInteropInstanceClassName(this.instanced.First(i => i.Type == it))}({name})";
            else if (arg.Value.IsSerialized) name = $"deserialize({name}, {arg.Value.Serialized.Id})";
            return name;
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
