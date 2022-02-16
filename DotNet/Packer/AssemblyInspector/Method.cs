using System.Collections.Generic;
using System.Linq;

namespace Packer;

internal record Method
{
    public string Name { get; init; } = null!;
    public string Assembly { get; init; } = null!;
    public string Namespace { get; init; } = null!;
    public IReadOnlyList<Argument> Arguments { get; init; } = null!;
    public string ReturnType { get; init; } = null!;
    public bool Async { get; init; }
    public MethodType Type { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Type}] {Namespace}.{Name} ({args}) => {ReturnType}";
    }
}
