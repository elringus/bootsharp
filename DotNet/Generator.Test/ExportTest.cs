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
    void Foo (string? foo);
    ValueTask Bar ();
    Task<string> Nya ();
    string[] Far (int[] far);
}
",
            @"
using Microsoft.JSInterop;

namespace Foo;

public class JSFoo
{
    private static global::Bindings.IFoo handler = null!;

    public JSFoo (global::Bindings.IFoo handler)
    {
        JSFoo.handler = handler;
    }

    [JSInvokable] public static void Foo (global::System.String? foo) => handler.Foo(foo);
    [JSInvokable] public static global::System.Threading.Tasks.ValueTask Bar () => handler.Bar();
    [JSInvokable] public static global::System.Threading.Tasks.Task<global::System.String> Nya () => handler.Nya();
    [JSInvokable] public static global::System.String[] Far (global::System.Int32[] far) => handler.Far(far);
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

namespace Foo;

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
        },
        new object[] {
            @"
using DotNetJS;
using Microsoft.JSInterop;

[assembly:JSNamespace(@""Foo"", ""Bar"")]
[assembly:JSExport(new[] { typeof(A.B.C.IFoo) })]

namespace A.B.C;

public interface IFoo
{
    void Foo ();
}
",
            @"
using Microsoft.JSInterop;

namespace Foo;

public class JSFoo
{
    private static global::A.B.C.IFoo handler = null!;

    public JSFoo (global::A.B.C.IFoo handler)
    {
        JSFoo.handler = handler;
    }

    [JSInvokable] public static void Foo () => handler.Foo();
}
"
        }
    };
}
