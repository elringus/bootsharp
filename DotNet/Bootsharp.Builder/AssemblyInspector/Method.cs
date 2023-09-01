using System.Collections.Generic;
using System.Linq;

namespace Bootsharp.Builder;

internal record Method
{
    public required string Name { get; init; }
    public required string Assembly { get; init; }
    public required string Namespace { get; init; }
    public required string DeclaringName { get; init; }
    public required IReadOnlyList<Argument> Arguments { get; init; }
    public required string ReturnType { get; init; }
    public required bool ReturnNullable { get; init; }
    public required bool Async { get; init; }
    public required MethodType Type { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Type}] {Namespace}.{Name} ({args}) => {ReturnType}";
    }
}
