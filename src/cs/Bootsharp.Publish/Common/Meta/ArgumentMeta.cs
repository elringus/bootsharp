namespace Bootsharp.Publish;

internal sealed record ArgumentMeta
{
    public required string Name { get; init; }
    public required string JSName { get; init; }
    public required ValueMeta Value { get; init; }

    public override string ToString () => $"{Name}: {Value.JSTypeSyntax}";
}
