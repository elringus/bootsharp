namespace Bootsharp.Publish;

internal sealed record InterfaceMethodMeta
{
    public required string Name { get; set; }
    public required MethodMeta Generated { get; set; }
}
