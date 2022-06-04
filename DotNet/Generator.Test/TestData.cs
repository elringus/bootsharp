using System.Collections.Generic;

namespace Generator.Test;

public static class TestData
{
    public static IEnumerable<object[]> Functions { get; } = new[] {
        new object[] {
            @"
partial class Foo
{
    [JSFunction]
    partial void Bar ();
}",
            @"#nullable
partial class Foo
{
    partial void Bar () => JS.Invoke(""dotnet.Bindings.Bar"");
}"
        },
        new object[] {
            @"
namespace File.Scoped;
public static partial class Foo
{
    [JSFunction]
    private static partial Task BarAsync (string a, int b);
}",
            @"#nullable
namespace File.Scoped;
public static partial class Foo
{
    private static partial Task BarAsync (string a, int b) => JS.InvokeAsync(""dotnet.File.Scoped.BarAsync"", new object[] { a, b }).AsTask();
}"
        },
        new object[] {
            @"
namespace File.Scoped;
public static partial class Foo
{
    [JSFunction]
    private static partial Task<string?> BarAsync ();
}",
            @"#nullable
namespace File.Scoped;
public static partial class Foo
{
    private static partial Task<string?> BarAsync () => JS.InvokeAsync<string?>(""dotnet.File.Scoped.BarAsync"").AsTask();
}"
        },
        new object[] {
            @"
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
            @"#nullable
namespace Classic
{
partial class Foo
{
    partial DateTime GetTime (DateTime time) => JS.Invoke<DateTime>(""dotnet.Classic.GetTime"", new object[] { time });
    partial ValueTask<DateTime> GetTimeAsync (DateTime time) => JS.InvokeAsync<DateTime>(""dotnet.Classic.GetTimeAsync"", new object[] { time });
}
}"
        },
        new object[] {
            @"
using System;
[assembly:JSNamespace(@""Foo\.Bar\.(\S+)"", ""$1"")]
class JSNamespaceAttribute : Attribute { JSNamespaceAttribute (string _, string __) { } }

namespace Foo.Bar.Nya;

    public partial class Nya
    {
        [JSFunction]
        private static partial void OnFun (Nya nya);
    }",
            @"#nullable
using System;
namespace Foo.Bar.Nya;
public partial class Nya
{
    private static partial void OnFun (Nya nya) => JS.Invoke(""dotnet.Nya.OnFun"", new object[] { nya });
}"
        }
    };

    public static IEnumerable<object[]> Events { get; } = new[] {
        new object[] {
            @"
partial class Foo
{
    [JSEvent]
    partial void OnBar ();
}",
            @"#nullable
partial class Foo
{
    partial void OnBar () => JS.Invoke(""dotnet.Bindings.OnBar.broadcast"");
}"
        },
        new object[] {
            @"
namespace Space;
public static partial class Foo
{
    [JSEvent]
    public static partial void OnBar (string a, int b);
}",
            @"#nullable
namespace Space;
public static partial class Foo
{
    public static partial void OnBar (string a, int b) => JS.Invoke(""dotnet.Space.OnBar.broadcast"", new object[] { a, b });
}"
        }
    };
}
