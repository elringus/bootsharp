namespace Bootsharp.Builder;

internal record Argument
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool Nullable { get; init; }

    public override string ToString () => $"{Name}: {Type}";
}
