namespace Bootsharp.Publish;

internal class InteropMarshaller
{
    private readonly Dictionary<string, string> methodByName = [];

    public string Marshal (ValueMeta meta)
    {
        var typeSyntax = GetTypeSyntax(meta);
        var name = $"Marshal_{Mangle(typeSyntax)}";
        if (methodByName.ContainsKey(name)) return name;

        var nil = meta.Nullable ? "?" : "";
        var nullable = meta.Nullable ? "if (obj is null) return null;" : null;

        var body = "";
        if (meta.Type.IsArray && !ShouldMarshall(meta.Type.GetElementType()!))
            body = "return obj;"; // https://github.com/elringus/bootsharp/issues/138
        else if (IsList(meta.Type)) body = "return MarshalList(obj)"; // ---------> Handle marshalling elements.
        else if (IsDictionary(meta.Type)) body = "return MarshalDictionary(obj)";
        else body = EmitStruct();

        methodByName[name] =
            $$"""
              private static object{{nil}} {{name}} (object{{nil}} obj)
              {
                  {{JoinLines(2, nullable, body)}}
              }
              """;
        return name;

        string EmitStruct ()
        {
            return "";
        }
    }

    public string Unmarshal (ValueMeta meta)
    {
        var typeSyntax = GetTypeSyntax(meta);
        var name = $"Unmarshal_{Mangle(typeSyntax)}";
        if (methodByName.ContainsKey(name)) return name;

        var nil = meta.Nullable ? "?" : "";
        methodByName[name] =
            $$"""
              private static object{{nil}} {{name}} (object{{nil}} raw)
              {
                  
              }
              """;
        return name;
    }

    public IReadOnlyCollection<string> GetGeneratedMethods ()
    {
        return methodByName.Values;
    }

    private string GetTypeSyntax (ValueMeta meta)
    {
        return meta.Async ? meta.TypeSyntax[36..^1] : meta.TypeSyntax;
    }

    private string Mangle (string typeSyntax) => typeSyntax
        .Replace('.', '_').Replace('+', '_')
        .Replace("<", "_").Replace(">", "")
        .Replace("?", "_Nil").Replace("[", "_Array").Replace("]", "")
        .Replace("global::", "").Replace(" ", "");
}
