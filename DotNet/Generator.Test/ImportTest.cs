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
    void Foo (string foo);
    bool Bar ();
    ValueTask Nya ();
    Task<string> Far ();
}
",
            @"
using DotNetJS;

namespace Bindings;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void Foo (global::System.String foo) => JS.Invoke(""dotnet.Bindings.Foo.broadcast"", new object[] { foo });
    [JSFunction] public static global::System.Boolean Bar () => JS.Invoke<global::System.Boolean>(""dotnet.Bindings.Bar"");
    [JSFunction] public static global::System.Threading.Tasks.ValueTask Nya () => JS.InvokeAsync(""dotnet.Bindings.Nya"");
    [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> Far () => JS.InvokeAsync<global::System.String>(""dotnet.Bindings.Far"").AsTask();

    void global::Bindings.IFoo.Foo (global::System.String foo) => Foo(foo);
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

namespace Bindings;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void OnFoo (global::System.String foo) => Try(JS.Invoke(""dotnet.Bindings.OnFoo.broadcast"", new object[] { foo }));
    [JSFunction] public static global::System.Boolean Bar () => Try(JS.Invoke<global::System.Boolean>(""dotnet.Bindings.Bar""));

    void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => OnFoo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSNamespace(@"".+\.I(\S+)"", ""$1"", true)]
[assembly:JSImport(new[] { typeof(A.B.C.IFoo) })]

namespace A.B.C;

public interface IFoo
{
    void Foo ();
}
",
            @"
using DotNetJS;

namespace A.B.C;

public class JSFoo : global::A.B.C.IFoo
{
    [JSEvent] public static void Foo () => JS.Invoke(""dotnet.Foo.Foo.broadcast"");

    void global::A.B.C.IFoo.Foo () => Foo();
}
"
        }
    };
}
