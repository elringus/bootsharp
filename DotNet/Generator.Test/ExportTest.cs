using System.Collections.Generic;

namespace Generator.Test;

public static class ExportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
using DotNetJS;
using System.Threading.Tasks;

[assembly:JSExport(new[] { typeof(Bindings.IFoo) })]

namespace Bindings;

public interface IFoo
{
    void Foo (string foo);
    ValueTask Bar ();
    Task<string> Nya ();
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
    [JSInvokable] public static global::System.Threading.Tasks.ValueTask Bar () => handler.Bar();
    [JSInvokable] public static global::System.Threading.Tasks.Task<global::System.String> Nya () => handler.Nya();
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSExport(new[] { typeof(Bindings.IFoo) }, ""Foo"", ""Bar"", ""(.+)"", ""Try($1)"")]

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

    [JSInvokable] public static void Bar (global::System.String foo) => Try(handler.Foo(foo));
}
"
        }
    };
}
