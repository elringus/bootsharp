using System.Collections.Generic;

namespace Generator.Test;

public static class EventTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            """
            partial class Foo
            {
                [JSEvent]
                partial void OnBar ();
            }
            """,
            """
            partial class Foo
            {
                partial void OnBar () => JS.Invoke("dotnet.Bindings.onBar.broadcast");
            }
            """
        },
        new object[] {
            """
            namespace Space;

            public static partial class Foo
            {
                [JSEvent]
                public static partial void OnBar (string a, int b);
            }
            """,
            """
            namespace Space;

            public static partial class Foo
            {
                public static partial void OnBar (string a, int b) => JS.Invoke("dotnet.Space.onBar.broadcast", new object[] { a, b });
            }
            """
        }
    };
}
