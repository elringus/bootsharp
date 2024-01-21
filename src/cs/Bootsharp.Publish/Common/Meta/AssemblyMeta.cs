namespace Bootsharp.Publish;

internal sealed record AssemblyMeta
{
    public required string Name { get; init; }
    public required byte[] Bytes { get; init; }
}
