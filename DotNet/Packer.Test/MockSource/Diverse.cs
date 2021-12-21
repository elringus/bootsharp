using System.Diagnostics.CodeAnalysis;
using DotNetJS;
using Microsoft.JSInterop;

namespace Packer.Test;

[ExcludeFromCodeCoverage]
public class Diverse : MockSource
{
    [JSInvokable]
    public static void Foo () { }

    [JSFunction]
    public static string Bar () => "";

    public override string[] GetExpectedInitLines () => new[] {
        "",
        ""
    };

    public override string[] GetExpectedBootLines () => new[] {
        "",
        ""
    };

    public override string[] GetExpectedTypeLines () => new[] {
        "",
        ""
    };
}
