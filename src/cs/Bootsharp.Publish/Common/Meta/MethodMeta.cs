namespace Bootsharp.Publish;

internal sealed record MethodMeta
{
    public required MethodKind Kind { get; init; }
    public required string Assembly { get; init; }
    public required string Space { get; init; }
    public required string JSSpace { get; init; }
    public required string Name { get; init; }
    public required string JSName { get; init; }
    public required IReadOnlyList<ArgumentMeta> Arguments { get; init; }
    public required ValueMeta ReturnValue { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Kind}] {Assembly}.{Space}.{Name} ({args}) => {ReturnValue}";
    }
}
