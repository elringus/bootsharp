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
    private static partial Task BarAsync (string a, int b) => JS.InvokeAsync(""dotnet.File.Scoped.BarAsync"", new object[] { a, b }).AsTask();
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
    private static partial Task<string?> BarAsync () => JS.InvokeAsync<string?>(""dotnet.File.Scoped.BarAsync"").AsTask();
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
    partial DateTime GetTime (DateTime time) => JS.Invoke<DateTime>(""dotnet.Classic.GetTime"", new object[] { time });
    partial ValueTask<DateTime> GetTimeAsync (DateTime time) => JS.InvokeAsync<DateTime>(""dotnet.Classic.GetTimeAsync"", new object[] { time });
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
    public static partial void OnFun (Foo foo);
}",
            @"
using DotNetJS;

namespace A.B.C;

public partial class Foo
{
    public static partial void OnFun (Foo foo) => JS.Invoke(""dotnet.C.OnFun"", new object[] { foo });
}
"
        },
        new object[] {
            @"
using DotNetJS;

[assembly:JSNamespace(@"".+\.(\S+)"", ""$1"", true)]

namespace A.B.C;

public partial class Foo
{
    [JSFunction]
    public static partial void OnFun (Foo foo);
}",
            @"
using DotNetJS;

namespace A.B.C;

public partial class Foo
{
    public static partial void OnFun (Foo foo) => JS.Invoke(""dotnet.Foo.OnFun"", new object[] { foo });
}
"
        }
    };
}
