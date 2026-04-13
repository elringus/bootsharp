using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly HashSet<string> methods = [];
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        var @static = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods));
        foreach (var meta in @static) // @formatter:off
            if (meta.Kind == MethodKind.Invokable) AddExportMethod(meta);
            else { AddImportMethod(meta); AddImportProxy(meta); } // @formatter:on
        return
            $$"""
              #nullable enable
              #pragma warning disable

              using System.Runtime.InteropServices.JavaScript;

              namespace Bootsharp.Generated;

              public static partial class Interop
              {
                  [System.Runtime.InteropServices.JavaScript.JSExport] internal static void DisposeExportedInstance (int id) => Instances.Dispose(id);
                  [System.Runtime.InteropServices.JavaScript.JSImport("disposeInstance", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);

                  {{JoinLines(methods)}}
              }
              """;
    }

    private void AddExportMethod (MethodMeta inv)
    {
        var instanced = TryInstanced(inv, out var instance);
        var marshalAs = MarshalAmbiguous(inv.ReturnValue, true);
        var wait = ShouldWait(inv);
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSExport] {marshalAs}";
        methods.Add($"{attr}internal static {BuildSignature()} => {BuildBody()};");

        string BuildSignature ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildSignatureArg));
            if (instanced) args = args = PrependInstanceIdArgTypeAndName(args);
            var @return = BuildReturnValue(inv);
            var signature = $"{@return} {BuildMethodName(inv)} ({args})";
            if (wait) signature = $"async {signature}";
            return signature;
        }

        string BuildBody ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildBodyArg));
            var body = instanced
                ? $"(({instance!.TypeSyntax})Instances.Get(_id)).{inv.Name}({args})"
                : $"global::{inv.Space}.{inv.Name}({args})";
            if (wait) body = $"await {body}";
            if (inv.ReturnValue.IsInstance) body = $"Instances.Register({body})";
            else if (Serialized(inv.ReturnValue, out var id)) body = $"Serializer.Serialize({body}, {id})";
            return body;
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (arg.Value.IsInstance)
            {
                var (_, _, full) = BuildInteropInterfaceImplementationName(arg.Value.InstanceType, InterfaceKind.Import);
                return $"new global::{full}({arg.Name})";
            }
            if (Serialized(arg.Value, out var id)) return $"Serializer.Deserialize({arg.Name}, {id})";
            return arg.Name;
        }
    }

    private void AddImportMethod (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(BuildSignatureArg));
        if (TryInstanced(method, out _)) args = PrependInstanceIdArgTypeAndName(args);
        var @return = BuildReturnValue(method);
        var endpoint = $"{method.JSSpace}.{method.JSName}Serialized";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{endpoint}", "Bootsharp")]""";
        var marsh = MarshalAmbiguous(method.ReturnValue, true);
        methods.Add($"{attr} {marsh}internal static partial {@return} {BuildMethodName(method)} ({args});");
    }

    private void AddImportProxy (MethodMeta method)
    {
        var instanced = TryInstanced(method, out _);
        var name = $"Proxy_{BuildMethodName(method)}";
        var @return = method.ReturnValue.TypeSyntax;
        var args = string.Join(", ", method.Arguments.Select(arg => $"{arg.Value.TypeSyntax} {arg.Name}"));
        if (instanced) args = args = PrependInstanceIdArgTypeAndName(args);
        var wait = ShouldWait(method);
        var async = wait ? "async " : "";
        methods.Add($"public static {async}{@return} {name}({args}) => {BuildBody()};");

        string BuildBody ()
        {
            var args = string.Join(", ", method.Arguments.Select(BuildBodyArg));
            if (instanced) args = PrependInstanceIdArgName(args);
            var body = $"{BuildMethodName(method)}({args})";
            if (wait) body = $"await {body}";
            if (method.ReturnValue.InstanceType is { } itp)
            {
                var (_, _, full) = BuildInteropInterfaceImplementationName(itp, InterfaceKind.Import);
                return $"({BuildSyntax(itp)})new global::{full}({body})";
            }
            if (Serialized(method.ReturnValue, out var id)) return $"Serializer.Deserialize({body}, {id})";
            return body;
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (arg.Value.IsInstance) return $"Instances.Register({arg.Name})";
            if (Serialized(arg.Value, out var id)) return $"Serializer.Serialize({arg.Name}, {id})";
            return arg.Name;
        }
    }

    private string BuildValueType (ValueMeta value)
    {
        var nil = value.Nullable && !value.IsSerialized ? "?" : "";
        if (value.IsInstance) return $"global::System.Int32{nil}";
        if (value.IsSerialized) return $"global::System.Int64{nil}";
        return value.TypeSyntax;
    }

    private string BuildSignatureArg (ArgumentMeta arg)
    {
        var type = BuildValueType(arg.Value);
        return $"{MarshalAmbiguous(arg.Value, false)}{type} {arg.Name}";
    }

    private string BuildReturnValue (MethodMeta method)
    {
        var syntax = BuildValueType(method.ReturnValue);
        if (ShouldWait(method)) syntax = $"global::System.Threading.Tasks.Task<{syntax}>";
        return syntax;
    }

    private string BuildMethodName (MethodMeta method)
    {
        return $"{method.Space.Replace('.', '_')}_{method.Name}";
    }

    private bool TryInstanced (MethodMeta method, [NotNullWhen(true)] out InterfaceMeta? instance)
    {
        instance = instanced.FirstOrDefault(i => i.Methods.Contains(method));
        return instance is not null;
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.ReturnValue.IsSerialized || method.ReturnValue.IsInstance;
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
