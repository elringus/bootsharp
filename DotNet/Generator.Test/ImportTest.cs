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
    public static void Foo (global::System.String foo) => JS.Invoke(""dotnet.Bindings.Foo.broadcast"", new object[] { foo });
    public static global::System.Boolean Bar () => JS.Invoke<global::System.Boolean>(""dotnet.Bindings.Bar"");

    void global::Bindings.IFoo.Foo (global::System.String foo) => Foo(foo);
    global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
}
"
        }
    };
}
