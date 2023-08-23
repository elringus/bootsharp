using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

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
                partial void OnBar () => Function.InvokeVoid("Bindings.onBar.broadcast");
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
                public static partial void OnBar (string a, int b) => Function.InvokeVoid("Space.onBar.broadcast", SerializeArgs(a, b));
            }
            """
        }
    };
}
