namespace Bootsharp.Builder;

internal record Method
{
    public required MethodType Type { get; init; }
    public required string Assembly { get; init; }
    public required string DeclaringName { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<Argument> Arguments { get; init; }
    public required Type ReturnType { get; init; }
    public required string ReturnTypeSyntax { get; init; }
    public required bool ReturnsVoid { get; init; }
    public required bool ReturnsNullable { get; init; }
    public required bool ReturnsTaskLike { get; init; }
    public required bool ShouldSerializeReturnType { get; init; }
    public required string JSSpace { get; init; }
    public required IReadOnlyList<Argument> JSArguments { get; init; }
    public required string JSReturnTypeSyntax { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Type}] {Assembly}.{DeclaringName}.{Name} ({args}) => {ReturnTypeSyntax}";
    }
}
