using System.Collections.Generic;

namespace Generator.Test;

public static class ImportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
using DotNetJS;
using System.Threading.Tasks;

[assembly:JSImport(new[] { typeof(Bindings.IFoo) })]

namespace Bindings;

public interface IFoo
{
    void NotifyFoo (string foo);
    bool Bar ();
    ValueTask Nya ();
    Task<string> Far ();
}
",
            @"
using DotNetJS;

namespace Foo;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void NotifyFoo (global::System.String foo) => JS.Invoke(""dotnet.Foo.NotifyFoo.broadcast"", new object[] { foo });
    [JSFunction] public static global::System.Boolean Bar () => JS.Invoke<global::System.Boolean>(""dotnet.Foo.Bar"");
    [JSFunction] public static global::System.Threading.Tasks.ValueTask Nya () => JS.InvokeAsync(""dotnet.Foo.Nya"");
    [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> Far () => JS.InvokeAsync<global::System.String>(""dotnet.Foo.Far"").AsTask();

    void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
    global::System.Threading.Tasks.ValueTask global::Bindings.IFoo.Nya () => Nya();
    global::System.Threading.Tasks.Task<global::System.String> global::Bindings.IFoo.Far () => Far();
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSImport(new[] { typeof(Bindings.IFoo) }, ""Notify(.+)"", ""On$1"", ""(.+)"", ""Try($1)"")]

namespace Bindings;

public interface IFoo
{
    void NotifyFoo (string foo);
    bool Bar ();
}
",
            @"
using DotNetJS;

namespace Foo;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void OnFoo (global::System.String foo) => Try(JS.Invoke(""dotnet.Foo.OnFoo.broadcast"", new object[] { foo }));
    [JSFunction] public static global::System.Boolean Bar () => Try(JS.Invoke<global::System.Boolean>(""dotnet.Foo.Bar""));

    void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => OnFoo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSNamespace(@""Foo"", ""Bar"")]
[assembly:JSImport(new[] { typeof(A.B.C.IFoo) })]

namespace A.B.C;

public interface IFoo
{
    void Foo ();
}
",
            @"
using DotNetJS;

namespace Foo;

public class JSFoo : global::A.B.C.IFoo
{
    [JSFunction] public static void Foo () => JS.Invoke(""dotnet.Bar.Foo"");

    void global::A.B.C.IFoo.Foo () => Foo();
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSImport(new[] { typeof(IFoo) }, ""Foo"", null, ""Foo"", null)]

public interface IFoo
{
    void Foo ();
}
",
            @"
using DotNetJS;

namespace Foo;

public class JSFoo : global::Bindings.IFoo
{
    [JSFunction] public static void Foo () => JS.Invoke(""dotnet.Foo.Foo"");

    void global::Bindings.IFoo.Foo () => Foo();
}
"
        },
    };
}
