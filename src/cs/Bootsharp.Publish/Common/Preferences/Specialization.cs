namespace Bootsharp.Publish;

internal sealed record Specialization
{
    public required Type Import { get; init; }
    public required Type Export { get; init; }
    public string? CS { get; init; }
    public string? JS { get; init; }
    public string? JSCtor { get; init; }
    public string? Decl { get; init; }

    public Specialization For (Type specialized) => this with {
        Import = Close(Import, specialized),
        Export = Close(Export, specialized)
    };

    private static Type Close (Type specializer, Type specialized) =>
        specializer.IsGenericTypeDefinition && specialized.IsConstructedGenericType
            ? specializer.MakeGenericType(specialized.GenericTypeArguments)
            : specializer;
}
