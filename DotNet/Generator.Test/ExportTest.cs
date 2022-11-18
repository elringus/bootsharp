using System.Collections.Generic;

namespace Generator.Test;

public static class ExportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
using DotNetJS;

[assembly:JSExport(new[] { typeof(Bindings.IFoo) })]

namespace Bindings;

public interface IFoo
{
    void Foo (string foo);
}
",
            @"
using Microsoft.JSInterop;

namespace Bindings;

public class JSFoo
{
    private static global::Bindings.IFoo handler = null!;

    public JSFoo (global::Bindings.IFoo handler)
    {
        JSFoo.handler = handler;
    }

    [JSInvokable] public static void Foo (global::System.String foo) => handler.Foo(foo);
}
"
        }
    };
}
