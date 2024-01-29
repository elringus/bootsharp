namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly HashSet<string> proxies = [];
    private readonly HashSet<string> methods = [];

    public string Generate (SolutionInspection inspection)
    {
        var metas = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods));
        foreach (var meta in metas) // @formatter:off
            if (meta.Kind == MethodKind.Invokable) AddExportMethod(meta);
            else { AddProxy(meta); AddImportMethod(meta); } // @formatter:on
        return
            $$"""
              #nullable enable
              #pragma warning disable

              using System.Runtime.InteropServices.JavaScript;
              using static Bootsharp.Serializer;

              namespace Bootsharp.Generated;

              public static partial class Interop
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  internal static void RegisterProxies ()
                  {
                      {{JoinLines(proxies, 2)}}
                  }

                  [System.Runtime.InteropServices.JavaScript.JSExport] internal static void DisposeExportedInstance (global::System.Int32 id) => global::Bootsharp.Instances.Dispose(id);
                  [System.Runtime.InteropServices.JavaScript.JSImport("disposeImportedInstance", "Bootsharp")] internal static partial void DisposeImportedInstance (global::System.Int32 id);

                  {{JoinLines(methods)}}
              }
              """;
    }

    private void AddExportMethod (MethodMeta inv)
    {
        const string attr = "[System.Runtime.InteropServices.JavaScript.JSExport]";
        var marshalAs = MarshalAmbiguous(inv.ReturnValue.TypeSyntax, true);
        var wait = inv.ReturnValue.Async && inv.ReturnValue.Serialized;
        methods.Add($"{attr} {marshalAs}internal static {BuildSignature()} => {BuildBody()};");

        string BuildSignature ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildSignatureArg));
            var @return = BuildReturnValue(inv.ReturnValue);
            var signature = $"{@return} {BuildMethodName(inv)} ({args})";
            if (wait) signature = $"async {signature}";
            return signature;
        }

        string BuildBody ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildBodyArg));
            var body = $"global::{inv.Space}.{inv.Name}({args})";
            if (wait) body = $"await {body}";
            if (inv.ReturnValue.Instance) body = $"global::Bootsharp.Instances.GetId({body})";
            else if (inv.ReturnValue.Serialized) body = $"Serialize({body})";
            return body;
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (arg.Value.Instance)
            {
                var (_, _, full) = BuildInteropInterfaceImplementationName(arg.Value.Type, InterfaceKind.Import);
                return $"new global::{full}({arg.Name})";
            }
            if (arg.Value.Serialized) return $"Deserialize<{arg.Value.TypeSyntax}>({arg.Name})";
            return arg.Name;
        }
    }

    private void AddProxy (MethodMeta method)
    {
        var id = $"{method.Space}.{method.Name}";
        var args = string.Join(", ", method.Arguments.Select(arg => $"{arg.Value.TypeSyntax} {arg.Name}"));
        var wait = method.ReturnValue.Async && method.ReturnValue.Serialized;
        var async = wait ? "async " : "";
        proxies.Add($"""Proxies.Set("{id}", {async}({args}) => {BuildBody()});""");

        string BuildBody ()
        {
            var args = string.Join(", ", method.Arguments.Select(BuildBodyArg));
            var body = $"{BuildMethodName(method)}({args})";
            if (method.ReturnValue.Instance)
            {
                var (_, _, full) = BuildInteropInterfaceImplementationName(method.ReturnValue.Type, InterfaceKind.Import);
                return $"new global::{full}({body})";
            }
            if (!method.ReturnValue.Serialized) return body;
            if (wait) body = $"await {body}";
            var type = method.ReturnValue.Async
                ? method.ReturnValue.TypeSyntax[36..^1]
                : method.ReturnValue.TypeSyntax;
            return $"Deserialize<{type}>({body})";
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (arg.Value.Instance)
                return $"({arg.Value.TypeSyntax})global::Bootsharp.Instances.GetInstance({arg.Name})";
            if (arg.Value.Serialized) return $"Serialize({arg.Name})";
            return arg.Name;
        }
    }

    private void AddImportMethod (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(BuildSignatureArg));
        var @return = BuildReturnValue(method.ReturnValue);
        var endpoint = $"{method.JSSpace}.{method.JSName}Serialized";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{endpoint}", "Bootsharp")]""";
        var date = MarshalAmbiguous(method.ReturnValue.TypeSyntax, true);
        methods.Add($"{attr} {date}internal static partial {@return} {BuildMethodName(method)} ({args});");
    }

    private string BuildValueType (ValueMeta value)
    {
        if (value.Void) return "void";
        var nil = value.Nullable ? "?" : "";
        if (value.Instance) return $"global::System.Int32{(nil)}";
        if (value.Serialized) return $"global::System.String{(nil)}";
        return value.TypeSyntax;
    }

    private string BuildSignatureArg (ArgumentMeta arg)
    {
        var type = BuildValueType(arg.Value);
        return $"{MarshalAmbiguous(arg.Value.TypeSyntax, false)}{type} {arg.Name}";
    }

    private string BuildReturnValue (ValueMeta value)
    {
        var syntax = BuildValueType(value);
        if (value.Serialized && value.Async)
            syntax = $"global::System.Threading.Tasks.Task<{syntax}>";
        return syntax;
    }

    private string BuildMethodName (MethodMeta method)
    {
        return $"{method.Space.Replace('.', '_')}_{method.Name}";
    }
}
