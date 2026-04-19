using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly InteropInitializerGenerator initGenerator = new();
    private readonly HashSet<string> methods = [];
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        foreach (var method in inspection.StaticMethods)
            if (method.Interop == InteropKind.Export) AddMethodExport(method);
            else AddMethodImport(method);
        foreach (var inter in inspection.StaticInterfaces)
        foreach (var member in inter.Members)
            AddMember(member);
        foreach (var inter in inspection.InstancedInterfaces)
        foreach (var member in inter.Members)
            AddMember(member);
        return
            $$"""
              #nullable enable
              #pragma warning disable

              using System.Runtime.CompilerServices;
              using System.Runtime.InteropServices.JavaScript;

              namespace Bootsharp.Generated;

              public static partial class Interop
              {
                  [System.Runtime.InteropServices.JavaScript.JSExport] internal static void DisposeExportedInstance (int id) => Instances.Dispose(id);
                  [System.Runtime.InteropServices.JavaScript.JSImport("disposeInstance", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);

                  {{initGenerator.Generate(inspection.StaticMethods)}}

                  {{JoinLines(methods)}}
              }
              """;
    }

    private void AddMember (MemberMeta member)
    {
        switch (member)
        {
            case PropertyMeta { Interop: InteropKind.Export } p: AddPropertyExport(p); break;
            case PropertyMeta { Interop: InteropKind.Import } p: AddPropertyImport(p); break;
            case MethodMeta { Interop: InteropKind.Export } m: AddMethodExport(m); break;
            case MethodMeta { Interop: InteropKind.Import } m: AddMethodImport(m); break;
        }
    }

    private void AddPropertyExport (PropertyMeta prop)
    {
        var instanced = TryInstanced(prop, out var instance);
        if (prop.CanGet)
        {
            var marshalAs = MarshalAmbiguous(prop.Value, true);
            var attr = $"[System.Runtime.InteropServices.JavaScript.JSExport] {marshalAs}";
            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var args = instanced ? PrependInstanceIdArgTypeAndName("") : "";
            var body = instanced
                ? $"(({instance!.TypeSyntax})Instances.Get(_id)).{prop.Name}"
                : $"global::{prop.Space}.GetProperty{prop.Name}()";
            if (prop.Value.IsInstance) body = $"Instances.Register({body})";
            else if (Serialized(prop.Value, out var id)) body = $"Serializer.Serialize({body}, {id})";
            methods.Add($"{attr}internal static {BuildValueSyntax(prop.Value)} {name} ({args}) => {body};");
        }
        if (prop.CanSet)
        {
            var attr = "[System.Runtime.InteropServices.JavaScript.JSExport] ";
            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = BuildParameter(prop.Value, "value");
            if (instanced) args = PrependInstanceIdArgTypeAndName(args);
            var value = prop.Value.InstanceType is { } it
                ? $"new global::{BuildInterfaceImplName(it, InteropKind.Import).full}(value)"
                : Serialized(prop.Value, out var id) ? $"Serializer.Deserialize(value, {id})" : "value";
            var body = instanced
                ? $"(({instance!.TypeSyntax})Instances.Get(_id)).{prop.Name} = {value}"
                : $"global::{prop.Space}.SetProperty{prop.Name}({value})";
            methods.Add($"{attr}internal static void {name} ({args}) => {body};");
        }
    }

    private void AddPropertyImport (PropertyMeta prop)
    {
        var instanced = TryInstanced(prop, out _);
        if (prop.CanGet)
        {
            var endpoint = $"""("{prop.JSSpace}.getProperty{prop.Name}Serialized", "Bootsharp")""";
            var marshalAs = MarshalAmbiguous(prop.Value, true);
            var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] {marshalAs}";
            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var args = instanced ? PrependInstanceIdArgTypeAndName("") : "";
            methods.Add($"{attr}internal static partial {BuildValueSyntax(prop.Value)} {name} ({args});");
        }
        if (prop.CanSet)
        {
            var endpoint = $"""("{prop.JSSpace}.setProperty{prop.Name}Serialized", "Bootsharp")""";
            var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] ";
            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = BuildParameter(prop.Value, "value");
            if (instanced) args = PrependInstanceIdArgTypeAndName(args);
            methods.Add($"{attr}internal static partial void {name} ({args});");
        }
        AddPropertyImportProxy(prop);
    }

    private void AddPropertyImportProxy (PropertyMeta prop)
    {
        var instanced = TryInstanced(prop, out _);
        if (prop.CanGet)
        {
            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var args = instanced ? PrependInstanceIdArgTypeAndName("") : "";
            var body = instanced ? $"{name}(_id)" : $"{name}()";
            if (prop.Value.InstanceType is { } it)
                body = $"({BuildSyntax(it)})new global::{BuildInterfaceImplName(it, InteropKind.Import).full}({body})";
            else if (Serialized(prop.Value, out var id)) body = $"Serializer.Deserialize({body}, {id})";
            methods.Add($"public static {prop.Value.TypeSyntax} Proxy_{name}({args}) => {body};");
        }
        if (prop.CanSet)
        {
            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = $"{prop.Value.TypeSyntax} value";
            if (instanced) args = PrependInstanceIdArgTypeAndName(args);
            var value = prop.Value.IsInstance ? "Instances.Register(value)" :
                Serialized(prop.Value, out var id) ? $"Serializer.Serialize(value, {id})" : "value";
            var body = instanced ? $"{name}(_id, {value})" : $"{name}({value})";
            methods.Add($"public static void Proxy_{name}({args}) => {body};");
        }
    }

    private void AddMethodExport (MethodMeta method)
    {
        var instanced = TryInstanced(method, out var instance);
        var wait = ShouldWait(method);
        var marshalAs = MarshalAmbiguous(method.Value, true);
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSExport] {marshalAs}";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var @return = BuildValueSyntax(method.Value);
        if (wait) @return = $"async global::System.Threading.Tasks.Task<{@return}>";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (instanced) sigArgs = PrependInstanceIdArgTypeAndName(sigArgs);
        var callArgs = string.Join(", ", method.Arguments.Select(BuildCallArg));
        var body = instanced
            ? $"(({instance!.TypeSyntax})Instances.Get(_id)).{method.Name}({callArgs})"
            : $"global::{method.Space}.{method.Name}({callArgs})";
        if (wait) body = $"await {body}";
        if (method.Value.IsInstance) body = $"Instances.Register({body})";
        else if (Serialized(method.Value, out var id)) body = $"Serializer.Serialize({body}, {id})";
        methods.Add($"{attr}internal static {@return} {name} ({sigArgs}) => {body};");

        string BuildCallArg (ArgumentMeta arg)
        {
            if (arg.Value.InstanceType is { } it)
                return $"new global::{BuildInterfaceImplName(it, InteropKind.Import).full}({arg.Name})";
            if (Serialized(arg.Value, out var id)) return $"Serializer.Deserialize({arg.Name}, {id})";
            return arg.Name;
        }
    }

    private void AddMethodImport (MethodMeta method)
    {
        var instanced = TryInstanced(method, out _);
        var marshalAs = MarshalAmbiguous(method.Value, true);
        var endpoint = $"""("{method.JSSpace}.{method.JSName}Serialized", "Bootsharp")""";
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] {marshalAs}";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var @return = BuildValueSyntax(method.Value);
        if (ShouldWait(method)) @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var args = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (instanced) args = PrependInstanceIdArgTypeAndName(args);
        methods.Add($"{attr}internal static partial {@return} {name} ({args});");
        AddMethodImportProxy(method);
    }

    private void AddMethodImportProxy (MethodMeta method)
    {
        var instanced = TryInstanced(method, out _);
        var wait = ShouldWait(method);
        var @return = $"{(wait ? "async " : "")}{method.Value.TypeSyntax}";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var sigArgs = string.Join(", ", method.Arguments.Select(arg => $"{arg.Value.TypeSyntax} {arg.Name}"));
        if (instanced) sigArgs = PrependInstanceIdArgTypeAndName(sigArgs);
        var callArgs = string.Join(", ", method.Arguments.Select(BuildCallArg));
        if (instanced) callArgs = PrependInstanceIdArgName(callArgs);
        var body = $"{name}({callArgs})";
        if (wait) body = $"await {body}";
        if (method.Value.InstanceType is { } it)
            body = $"({BuildSyntax(it)})new global::{BuildInterfaceImplName(it, InteropKind.Import).full}({body})";
        else if (Serialized(method.Value, out var id)) body = $"Serializer.Deserialize({body}, {id})";
        methods.Add($"public static {@return} Proxy_{name} ({sigArgs}) => {body};");

        string BuildCallArg (ArgumentMeta arg)
        {
            if (arg.Value.IsInstance) return $"Instances.Register({arg.Name})";
            if (Serialized(arg.Value, out var id)) return $"Serializer.Serialize({arg.Name}, {id})";
            return arg.Name;
        }
    }

    private string BuildParameter (ValueMeta value, string name)
    {
        var type = BuildValueSyntax(value);
        return $"{MarshalAmbiguous(value, false)}{type} {name}";
    }

    private string BuildValueSyntax (ValueMeta value)
    {
        var nil = value.Nullable && !value.IsSerialized ? "?" : "";
        if (value.IsInstance) return $"global::System.Int32{nil}";
        if (value.IsSerialized) return $"global::System.Int64{nil}";
        return value.TypeSyntax;
    }

    private bool TryInstanced (MemberMeta member, [NotNullWhen(true)] out InterfaceMeta? instance)
    {
        instance = instanced.FirstOrDefault(i => i.Members.Contains(member));
        return instance is not null;
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Value.IsSerialized || method.Value.IsInstance;
    }

    private static string MarshalAmbiguous (ValueMeta value, bool @return)
    {
        var stx = value.TypeSyntax;
        var promise = stx.StartsWith("global::System.Threading.Tasks.Task<");
        if (promise) stx = stx[36..];

        var result = "";
        if (value.IsSerialized || stx.StartsWith("global::System.Int64")) result = "JSType.BigInt";
        else if (stx.StartsWith("global::System.DateTime")) result = "JSType.Date";
        if (result == "") return "";

        if (promise) result = $"JSType.Promise<{result}>";
        if (@return) return $"[return: JSMarshalAs<{result}>] ";
        return $"[JSMarshalAs<{result}>] ";
    }

    private static bool Serialized (ValueMeta meta, [NotNullWhen(true)] out string? id)
    {
        if (!meta.IsSerialized) id = null;
        else id = $"SerializerContext.{meta.Serialized.Id}";
        return id != null;
    }
}
