namespace Bootsharp.Publish;

internal sealed record InterfaceMeta
{
    public required InterfaceKind Kind { get; init; }
    public required string TypeSyntax { get; init; }
    public required string Namespace { get; init; }
    public required string Name { get; init; }
    public string FullName => $"{Namespace}.{Name}";
    public required IReadOnlyCollection<InterfaceMethodMeta> Methods { get; init; }
}
