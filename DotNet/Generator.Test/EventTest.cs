using System.Collections.Generic;

namespace Generator.Test;

public static class EventTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            @"
partial class Foo
{
    [JSEvent]
    partial void OnBar ();
}",
            @"
partial class Foo
{
    partial void OnBar () => JS.Invoke(""dotnet.Bindings.OnBar.broadcast"");
}
"
        },
        new object[] {
            @"
namespace Space;

public static partial class Foo
{
    [JSEvent]
    public static partial void OnBar (string a, int b);
}",
            @"
namespace Space;

public static partial class Foo
{
    public static partial void OnBar (global::System.String a, global::System.Int32 b) => JS.Invoke(""dotnet.Space.OnBar.broadcast"", new object[] { a, b });
}
"
        }
    };
}
