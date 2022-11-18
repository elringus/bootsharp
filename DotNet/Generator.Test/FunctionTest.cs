using System.Collections.Generic;

namespace Generator.Test;

public static class FunctionTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
using DotNetJS;

partial class Foo
{
    [JSFunction]
    partial void Bar ();
}",
            @"
using DotNetJS;

partial class Foo
{
    partial void Bar () => JS.Invoke(""dotnet.Bindings.Bar"");
}
"
        },
        new object[] {
            @"
using System.Threading.Tasks;

namespace File.Scoped;

public static partial class Foo
{
    [JSFunction]
    private static partial Task BarAsync (string a, int b);
}",
            @"
using System.Threading.Tasks;

namespace File.Scoped;

public static partial class Foo
{
    private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String a, global::System.Int32 b) => JS.InvokeAsync(""dotnet.File.Scoped.BarAsync"", new object[] { a, b }).AsTask();
}
"
        },
        new object[] {
            @"
using System.Threading.Tasks;

namespace File.Scoped;

public static partial class Foo
{
    [JSFunction]
    private static partial Task<string?> BarAsync ();
}",
            @"
using System.Threading.Tasks;

namespace File.Scoped;

public static partial class Foo
{
    private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () => JS.InvokeAsync<global::System.String?>(""dotnet.File.Scoped.BarAsync"").AsTask();
}
"
        },
        new object[] {
            @"
using System;
using System.Threading.Tasks;

namespace Classic
{
    partial class Foo
    {
        [JSFunction]
        partial DateTime GetTime (DateTime time);
        [JSFunction]
        partial ValueTask<DateTime> GetTimeAsync (DateTime time);
    }
}",
            @"
using System;
using System.Threading.Tasks;

namespace Classic
{
partial class Foo
{
    partial global::System.DateTime GetTime (global::System.DateTime time) => JS.Invoke<global::System.DateTime>(""dotnet.Classic.GetTime"", new object[] { time });
    partial global::System.Threading.Tasks.ValueTask<global::System.DateTime> GetTimeAsync (global::System.DateTime time) => JS.InvokeAsync<global::System.DateTime>(""dotnet.Classic.GetTimeAsync"", new object[] { time });
}
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSNamespace(@""A\.B\.(\S+)"", ""$1"")]

namespace A.B.C;

public partial class Foo
{
    [JSFunction]
    private static partial void OnFun (Foo foo);
}",
            @"
using DotNetJS;

namespace A.B.C;

public partial class Foo
{
    private static partial void OnFun (global::A.B.C.Foo foo) => JS.Invoke(""dotnet.C.OnFun"", new object[] { foo });
}
"
        }
    };
}
