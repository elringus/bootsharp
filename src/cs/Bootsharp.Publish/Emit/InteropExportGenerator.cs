namespace Bootsharp.Publish;

internal sealed class InteropExportGenerator
{
    public string Generate (AssemblyInspection inspection)
    {
        var bySpace = inspection.Methods
            .Where(m => m.Type == MethodType.Invokable)
            .GroupBy(i => i.Space).ToArray();
        return bySpace.Length == 0 ? "" :
            $"""
             #nullable enable
             #pragma warning disable

             using System.Runtime.InteropServices.JavaScript;
             using static Bootsharp.Serializer;

             namespace Bootsharp.Exports;

             {JoinLines(bySpace.Select(g => GenerateSpace(g.Key, g)), 0)}

             #pragma warning restore
             #nullable restore
             """;
    }

    private string GenerateSpace (string space, IEnumerable<MethodMeta> invokable) =>
        $$"""
          public partial class {{space.Replace('.', '_')}}
          {
              {{JoinLines(invokable.Select(GenerateExport))}}
          }
          """;

    private string GenerateExport (MethodMeta inv)
    {
        const string attr = "[System.Runtime.InteropServices.JavaScript.JSExport]";
        var date = MarshalAmbiguous(inv.ReturnValue.TypeSyntax, true);
        var wait = inv.ReturnValue.Async && inv.ReturnValue.Serialized;
        return $"{attr} {date}internal static {GenerateSignature(inv, wait)} => {GenerateBody(inv, wait)};";
    }

    private string GenerateSignature (MethodMeta inv, bool wait)
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

    private string GenerateBody (MethodMeta inv, bool wait)
    {
        var args = string.Join(", ", inv.Arguments.Select(GenerateBodyArg));
        var body = $"global::{inv.Space}.{inv.Name}({args})";
        if (wait) body = $"await {body}";
        if (inv.ReturnValue.Serialized) body = $"Serialize({body})";
        return body;
    }

    private string GenerateSignatureArg (ArgumentMeta arg)
    {
        var type = arg.Value.Serialized
            ? $"global::System.String{(arg.Value.Nullable ? "?" : "")}"
            : arg.Value.TypeSyntax;
        return $"{MarshalAmbiguous(arg.Value.TypeSyntax, false)}{type} {arg.Name}";
    }

    private string GenerateBodyArg (ArgumentMeta arg)
    {
        if (!arg.Value.Serialized) return arg.Name;
        return $"Deserialize<{arg.Value.TypeSyntax}>({arg.Name})";
    }
}
