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
        var date = MarshalAmbiguous(inv.ReturnValue.TypeSyntax, true);
        var wait = inv.ReturnValue.Async && inv.ReturnValue.Serialized;
        methods.Add($"{attr} {date}internal static {BuildSignature()} => {BuildBody()};");

        string BuildSignature ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildSignatureArg));
            var @return = inv.ReturnValue.Void ? "void" : (inv.ReturnValue.Serialized
                ? $"global::System.String{(inv.ReturnValue.Nullable ? "?" : "")}"
                : inv.ReturnValue.TypeSyntax);
            if (inv.ReturnValue.Serialized && inv.ReturnValue.Async)
                @return = $"global::System.Threading.Tasks.Task<{@return}>";
            var signature = $"{@return} {BuildMethodName(inv)} ({args})";
            if (wait) signature = $"async {signature}";
            return signature;
        }

        string BuildBody ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildBodyArg));
            var body = $"global::{inv.Space}.{inv.Name}({args})";
            if (wait) body = $"await {body}";
            if (inv.ReturnValue.Serialized) body = $"Serialize({body})";
            return body;
        }

        string BuildSignatureArg (ArgumentMeta arg)
        {
            var type = arg.Value.Serialized
                ? $"global::System.String{(arg.Value.Nullable ? "?" : "")}"
                : arg.Value.TypeSyntax;
            return $"{MarshalAmbiguous(arg.Value.TypeSyntax, false)}{type} {arg.Name}";
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (!arg.Value.Serialized) return arg.Name;
            return $"Deserialize<{arg.Value.TypeSyntax}>({arg.Name})";
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
            if (!method.ReturnValue.Serialized) return body;
            if (wait) body = $"await {body}";
            var type = method.ReturnValue.Async
                ? method.ReturnValue.TypeSyntax[36..^1]
                : method.ReturnValue.TypeSyntax;
            return $"Deserialize<{type}>({body})";
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (!arg.Value.Serialized) return arg.Name;
            return $"Serialize({arg.Name})";
        }
    }

    private void AddImportMethod (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(BuildArg));
        var @return = method.ReturnValue.Void ? "void" : (method.ReturnValue.Serialized
            ? $"global::System.String{(method.ReturnValue.Nullable ? "?" : "")}"
            : method.ReturnValue.TypeSyntax);
        if (method.ReturnValue.Serialized && method.ReturnValue.Async)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var endpoint = $"{method.JSSpace}.{method.JSName}Serialized";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{endpoint}", "Bootsharp")]""";
        var date = MarshalAmbiguous(method.ReturnValue.TypeSyntax, true);
        methods.Add($"{attr} {date}internal static partial {@return} {BuildMethodName(method)} ({args});");

        string BuildArg (ArgumentMeta arg)
        {
            var type = arg.Value.Serialized
                ? $"global::System.String{(arg.Value.Nullable ? "?" : "")}"
                : arg.Value.TypeSyntax;
            return $"{MarshalAmbiguous(arg.Value.TypeSyntax, false)}{type} {arg.Name}";
        }
    }

    private string BuildMethodName (MethodMeta method)
    {
        return $"{method.Space.Replace('.', '_')}_{method.Name}";
    }
}
