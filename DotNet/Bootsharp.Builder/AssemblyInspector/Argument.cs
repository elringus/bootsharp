namespace Bootsharp.Builder;

internal record Argument
{
    public required string Name { get; init; }
    public required Type Type { get; init; }
    public required string TypeSyntax { get; init; }
    public required bool Nullable { get; init; }
    public required bool ShouldSerialize { get; init; }

    public override string ToString () => $"{Name}: {TypeSyntax}";
}
