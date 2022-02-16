using System.Collections.Generic;

namespace Generator.Test;

public static class TestData
{
    public static IEnumerable<object[]> Functions { get; } = new[] {
        new object[] { "", "" },
        new object[] { "partial class Foo {}", "" },
        new object[] {
            @"
partial class Foo
{
    [JSFunction]
    partial void Bar ();
}
",
            @"
partial class Foo
{
    partial void Bar () => JS.Invoke(""dotnet.Bindings.Bar"");
}
"
        },
        new object[] {
            @"
namespace File.Scoped;
public static partial class Foo
{
    [JSFunction]
    private static partial Task BarAsync (string a, int b);
}
",
            @"
namespace File.Scoped;
public static partial class Foo
{
    private static partial Task BarAsync (string a, int b) => JS.InvokeAsync(""dotnet.File.Scoped.BarAsync"", a, b);
}
"
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
}
",
            @"
namespace Classic
{
partial class Foo
{
    partial DateTime GetTime (DateTime time) => JS.Invoke<DateTime>(""dotnet.Classic.GetTime"", time);
    partial ValueTask<DateTime> GetTimeAsync (DateTime time) => JS.InvokeAsync<DateTime>(""dotnet.Classic.GetTimeAsync"", time);
}
}
"
        },
        new object[] {
            @"
[assembly:JSNamespace(@""Foo\.Bar\.(\S+)"", ""$1"")]
namespace Foo.Bar.Nya;
public static partial class Nya
{
    [JSFunction]
    private static partial void OnFun (Nya nya);
}
",
            @"
namespace Foo.Bar.Nya;
public static partial class Nya
{
    private static partial void OnFun (Nya nya) => JS.Invoke(""dotnet.Nya.OnFun"", nya);
}
"
        }
    };
}
