namespace Bootsharp.Publish;

internal sealed record Specialization
{
    public required Type Import { get; init; }
    public required Type Export { get; init; }
    public string? JS { get; init; }
    public string? Decl { get; init; }

    public Type For (Type specialized, InteropKind ik)
    {
        var specializer = ik == InteropKind.Import ? Import : Export;
        if (!specializer.IsGenericTypeDefinition) return specializer;
        return specializer.MakeGenericType(specialized.GenericTypeArguments);
    }
}
