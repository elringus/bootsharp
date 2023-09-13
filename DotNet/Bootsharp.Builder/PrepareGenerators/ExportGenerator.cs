namespace Bootsharp.Builder;

internal sealed class ExportGenerator
{
    private bool wait;
    private Method inv = null!;

    public string Generate (AssemblyInspector inspector)
    {
        var bySpace = inspector.Methods
            .Where(m => m.Type == MethodType.Invokable)
            .GroupBy(i => i.DeclaringName).ToArray();
        return bySpace.Length == 0 ? "" :
            $"""
             using System.Runtime.InteropServices.JavaScript;
             using static Bootsharp.Serializer;

             namespace Bootsharp;

             {JoinLines(bySpace.Select(g => GenerateSpace(g.Key, g)), 0)}
             """;
    }

    private string GenerateSpace (string space, IEnumerable<Method> invokable) =>
        $$"""
          public partial class InteropExports_{{space.Replace('.', '_')}}
          {
              {{JoinLines(invokable.Select(GenerateExport))}}
          }
          """;

    private string GenerateExport (Method invokable)
    {
        inv = invokable;
        wait = inv.Arguments.Any(a => a.ShouldSerialize) && inv.ReturnsTaskLike;
        return $"[JSExport] internal static {GenerateSignature()} => {GenerateBody()};";
    }

    private string GenerateSignature ()
    {
        var args = string.Join(", ", inv.Arguments.Select(GenerateSignatureArg));
        var @return = inv.ReturnsVoid ? "void" : (inv.ShouldSerializeReturnType
            ? $"global::System.String{(inv.ReturnsNullable ? "?" : "")}"
            : inv.ReturnType);
        if (inv.ShouldSerializeReturnType && inv.ReturnsTaskLike)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var signature = $"{@return} {inv.Name} ({args})";
        if (wait) signature = $"async {signature}";
        return signature;
    }

    private string GenerateBody ()
    {
        var args = string.Join(", ", inv.Arguments.Select(GenerateBodyArg));
        var body = $"global::{inv.DeclaringName}.{inv.Name}({args})";
        if (wait) body = $"await {body}";
        if (inv.ShouldSerializeReturnType) body = $"Serialize({body})";
        return body;
    }

    private string GenerateSignatureArg (Argument arg)
    {
        var type = arg.ShouldSerialize
            ? $"global::System.String{(arg.Nullable ? "?" : "")}"
            : arg.Type;
        return $"{type} {arg.Name}";
    }

    private string GenerateBodyArg (Argument arg)
    {
        if (!arg.ShouldSerialize) return arg.Name;
        return $"Deserialize<{arg.Type}>({arg.Name})";
    }
}
