namespace Bootsharp.Publish;

internal class InteropMarshaller
{
    private readonly Dictionary<string, string> methodByName = [];

    public string Marshal (ValueMeta meta)
    {
        var typeSyntax = GetTypeSyntax(meta);
        var name = $"Marshal_{Mangle(typeSyntax)}";
        if (methodByName.TryGetValue(name, out var method)) return name;

        method = "";
        methodByName[name] = method;
        return name;
    }

    public string Unmarshal (ValueMeta meta)
    {
        var typeSyntax = GetTypeSyntax(meta);
        var name = $"Unmarshal_{Mangle(typeSyntax)}";
        if (methodByName.TryGetValue(name, out var method)) return name;

        method = "";
        methodByName[name] = method;
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

    private string Mangle (string typeSyntax)
    {
        return typeSyntax
            .Replace('.', '_').Replace('+', '_')
            .Replace("<", "_").Replace(">", "")
            .Replace("?", "_Nil").Replace("[", "_Array").Replace("]", "")
            .Replace("global::", "").Replace(" ", "");
    }
}
