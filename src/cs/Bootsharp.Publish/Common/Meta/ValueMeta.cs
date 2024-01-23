namespace Bootsharp.Publish;

internal sealed record ValueMeta
{
    public required Type Type { get; init; }
    public required string TypeSyntax { get; init; }
    public required string JSTypeSyntax { get; init; }
    public required bool Nullable { get; init; }
    public required bool Async { get; init; }
    public required bool Void { get; init; }
    public required bool Serialized { get; init; }
}
