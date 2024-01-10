namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by DotNet's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly HashSet<string> proxies = [];
    private readonly HashSet<string> methods = [];

    public string Generate (AssemblyInspection inspection)
    {
        foreach (var method in inspection.Methods)
            if (method.Type == MethodType.Invokable) AddExportMethod(method);
            else AddImportMethod(method);
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
                  {{JoinLines(methods)}}
              }
              """;
    }

    private void AddExportMethod (MethodMeta inv)
    {
        const string attr = "[System.Runtime.InteropServices.JavaScript.JSExport]";
        var date = MarshalAmbiguous(inv.ReturnValue.TypeSyntax, true);
        var wait = inv.ReturnValue.Async && inv.ReturnValue.Serialized;
        methods.Add($"{attr} {date}internal static {GenerateSignature(inv, wait)} => {GenerateBody(inv, wait)};");

        string GenerateSignature (MethodMeta inv, bool wait)
        {
            var args = string.Join(", ", inv.Arguments.Select(GenerateSignatureArg));
            var @return = inv.ReturnValue.Void ? "void" : (inv.ReturnValue.Serialized
                ? $"global::System.String{(inv.ReturnValue.Nullable ? "?" : "")}"
                : inv.ReturnValue.TypeSyntax);
            if (inv.ReturnValue.Serialized && inv.ReturnValue.Async)
                @return = $"global::System.Threading.Tasks.Task<{@return}>";
            var signature = $"{@return} {inv.Name} ({args})";
            if (wait) signature = $"async {signature}";
            return signature;
        }

        string GenerateBody (MethodMeta inv, bool wait)
        {
            var args = string.Join(", ", inv.Arguments.Select(GenerateBodyArg));
            var body = $"global::{inv.Space}.{inv.Name}({args})";
            if (wait) body = $"await {body}";
            if (inv.ReturnValue.Serialized) body = $"Serialize({body})";
            return body;
        }

        string GenerateSignatureArg (ArgumentMeta arg)
        {
            var type = arg.Value.Serialized
                ? $"global::System.String{(arg.Value.Nullable ? "?" : "")}"
                : arg.Value.TypeSyntax;
            return $"{MarshalAmbiguous(arg.Value.TypeSyntax, false)}{type} {arg.Name}";
        }

        string GenerateBodyArg (ArgumentMeta arg)
        {
            if (!arg.Value.Serialized) return arg.Name;
            return $"Deserialize<{arg.Value.TypeSyntax}>({arg.Name})";
        }
    }

    private void AddImportMethod (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(GenerateArg));
        var @return = method.ReturnValue.Void ? "void" : (method.ReturnValue.Serialized
            ? $"global::System.String{(method.ReturnValue.Nullable ? "?" : "")}"
            : method.ReturnValue.TypeSyntax);
        if (method.ReturnValue.Serialized && method.ReturnValue.Async)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{BuildEndpoint(method, true)}", "Bootsharp")]""";
        var date = MarshalAmbiguous(method.ReturnValue.TypeSyntax, true);
        methods.Add($"{attr} {date}internal static partial {@return} {method.Name} ({args});");
        proxies.Add($"""Function.Set("{BuildEndpoint(method, false)}", {method.Name});""");

        string GenerateArg (ArgumentMeta arg)
        {
            var type = arg.Value.Serialized
                ? $"global::System.String{(arg.Value.Nullable ? "?" : "")}"
                : arg.Value.TypeSyntax;
            return $"{MarshalAmbiguous(arg.Value.TypeSyntax, false)}{type} {arg.Name}";
        }

        string BuildEndpoint (MethodMeta method, bool import)
        {
            var name = char.ToLowerInvariant(method.Name[0]) + method.Name[1..];
            return $"{method.JSSpace}.{name}{(import ? "Serialized" : "")}";
        }
    }
}
