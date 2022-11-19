using System.Collections.Generic;

namespace Generator.Test;

public static class ImportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
using DotNetJS;

[assembly:JSImport(new[] { typeof(Bindings.IFoo) })]

namespace Bindings;

public interface IFoo
{
    void Foo (string foo);
    bool Bar ();
}
",
            @"
using DotNetJS;

namespace Bindings;

public class JSFoo : global::Bindings.IFoo
{
    [JSEvent] public static void Foo (global::System.String foo) => JS.Invoke(""dotnet.Bindings.Foo.broadcast"", new object[] { foo });
    [JSFunction] public static global::System.Boolean Bar () => JS.Invoke<global::System.Boolean>(""dotnet.Bindings.Bar"");

    void global::Bindings.IFoo.Foo (global::System.String foo) => Foo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
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
        }
    };
}
