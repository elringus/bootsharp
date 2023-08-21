using System.Collections.Generic;

namespace Generator.Test;

public static class ImportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
using Bootsharp;
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
using Bootsharp;

namespace Foo;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void NotifyFoo (global::System.String foo) => JS.Invoke(""dotnet.Foo.notifyFoo.broadcast"", new object[] { foo });
    [JSFunction] public static global::System.Boolean Bar () => JS.Invoke<global::System.Boolean>(""dotnet.Foo.bar"");
    [JSFunction] public static global::System.Threading.Tasks.ValueTask Nya () => JS.InvokeAsync(""dotnet.Foo.nya"");
    [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> Far () => JS.InvokeAsync<global::System.String>(""dotnet.Foo.far"").AsTask();

    void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
    global::System.Threading.Tasks.ValueTask global::Bindings.IFoo.Nya () => Nya();
    global::System.Threading.Tasks.Task<global::System.String> global::Bindings.IFoo.Far () => Far();
}
"
        },
        new object[] {
            @"
using Bootsharp;

[assembly:JSImport(new[] { typeof(Bindings.IFoo) }, ""Notify(.+)"", ""On$1"", ""(.+)"", ""Try($1)"")]

namespace Bindings;

public interface IFoo
{
    void NotifyFoo (string foo);
    bool Bar ();
}
",
            @"
using Bootsharp;

namespace Foo;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void OnFoo (global::System.String foo) => Try(JS.Invoke(""dotnet.Foo.onFoo.broadcast"", new object[] { foo }));
    [JSFunction] public static global::System.Boolean Bar () => Try(JS.Invoke<global::System.Boolean>(""dotnet.Foo.bar""));

    void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => OnFoo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
}
"
        },
        new object[] {
            @"
using Bootsharp;

[assembly:JSNamespace(@""Foo"", ""Bar"")]
[assembly:JSImport(new[] { typeof(A.B.C.IFoo) })]

namespace A.B.C;

public interface IFoo
{
    void F ();
}
",
            @"
using Bootsharp;

namespace Foo;

public class JSFoo : global::A.B.C.IFoo
{
    [JSFunction] public static void F () => JS.Invoke(""dotnet.Bar.f"");

    void global::A.B.C.IFoo.F () => F();
}
"
        },
        new object[] {
            @"
using Bootsharp;

[assembly:JSImport(new[] { typeof(IFoo) }, ""Foo"", null, ""Foo"", null)]

public interface IFoo
{
    void Foo ();
}
",
            @"
using Bootsharp;

namespace Foo;

public class JSFoo : global::Bindings.IFoo
{
    [JSFunction] public static void Foo () => JS.Invoke(""dotnet.Foo.foo"");

    void global::Bindings.IFoo.Foo () => Foo();
}
"
        },
    };
}
