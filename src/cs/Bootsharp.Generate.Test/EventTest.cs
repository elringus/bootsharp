namespace Bootsharp.Generate.Test;

public static class EventTest
{
    public static object[][] Data { get; } = [
        // Can generate event binding without namespace and arguments.
        [
            """
            partial class Foo
            {
                [JSEvent] partial void OnBar ();
            }
            """,
            """
            unsafe partial class Foo
            {
                private static delegate* managed<void> Proxy_Foo_OnBar;
                partial void OnBar () => Proxy_Foo_OnBar();
            }
            """
        ],
        // Can generate event binding with namespace and arguments.
        [
            """
            namespace Space;

            public static partial class Foo
            {
                [JSEvent] public static partial void OnBar (string a, int b);
            }
            """,
            """
            namespace Space;

            public static unsafe partial class Foo
            {
                private static delegate* managed<global::System.String, global::System.Int32, void> Proxy_Space_Foo_OnBar;
                public static partial void OnBar (global::System.String a, global::System.Int32 b) => Proxy_Space_Foo_OnBar(a, b);
            }
            """
        ]
    ];
}
