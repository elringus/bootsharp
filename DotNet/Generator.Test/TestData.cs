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
    partial void Bar () => JS.Invoke(""DotNetJS_functions_TestProject_Bar"");
}
"
        },
        new object[] {
            @"
namespace FileScoped;
public static partial class Foo
{
    [JSFunction]
    private static partial Task BarAsync (string a, int b);
}
",
            @"
namespace FileScoped;
public static partial class Foo
{
    private static partial Task BarAsync (string a, int b) => JS.InvokeAsync(""DotNetJS_functions_TestProject_BarAsync"", a, b);
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
    partial DateTime GetTime (DateTime time) => JS.Invoke<DateTime>(""DotNetJS_functions_TestProject_GetTime"", time);
    partial ValueTask<DateTime> GetTimeAsync (DateTime time) => JS.InvokeAsync<DateTime>(""DotNetJS_functions_TestProject_GetTimeAsync"", time);
}
}
"
        },
    };
}
