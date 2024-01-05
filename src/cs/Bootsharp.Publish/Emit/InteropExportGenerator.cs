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
        var date = MarshalAmbiguous(inv.ReturnType.Syntax, true);
        var wait = inv.ReturnType.TaskLike && inv.ReturnType.ShouldSerialize;
        return $"{attr} {date}internal static {GenerateSignature(inv, wait)} => {GenerateBody(inv, wait)};";
    }

    private string GenerateSignature (MethodMeta inv, bool wait)
    {
        var args = string.Join(", ", inv.Arguments.Select(GenerateSignatureArg));
        var @return = inv.ReturnType.Void ? "void" : (inv.ReturnType.ShouldSerialize
            ? $"global::System.String{(inv.ReturnType.Nullable ? "?" : "")}"
            : inv.ReturnType.Syntax);
        if (inv.ReturnType.ShouldSerialize && inv.ReturnType.TaskLike)
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
        if (inv.ReturnType.ShouldSerialize) body = $"Serialize({body})";
        return body;
    }

    private string GenerateSignatureArg (ArgumentMeta arg)
    {
        var type = arg.Type.ShouldSerialize
            ? $"global::System.String{(arg.Type.Nullable ? "?" : "")}"
            : arg.Type.Syntax;
        return $"{MarshalAmbiguous(arg.Type.Syntax, false)}{type} {arg.Name}";
    }

    private string GenerateBodyArg (ArgumentMeta arg)
    {
        if (!arg.Type.ShouldSerialize) return arg.Name;
        return $"Deserialize<{arg.Type.Syntax}>({arg.Name})";
    }
}
