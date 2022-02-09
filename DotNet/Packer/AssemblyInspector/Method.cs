using System.Collections.Generic;
using System.Linq;

namespace Packer;

internal record Method
{
    public string Name { get; init; }
    public string Assembly { get; init; }
    public string Namespace { get; init; }
    public IReadOnlyList<Argument> Arguments { get; init; }
    public string ReturnType { get; init; }
    public bool Async { get; init; }
    public MethodType Type { get; init; }

    public override string ToString ()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"[{Type}] {Assembly}.{Name} ({args}) => {ReturnType}";
    }
}
