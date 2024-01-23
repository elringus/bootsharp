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
            partial class Foo
            {
                partial void OnBar () => global::Bootsharp.Proxies.Get<global::System.Action>("Foo.OnBar")();
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

            public static partial class Foo
            {
                public static partial void OnBar (global::System.String a, global::System.Int32 b) => global::Bootsharp.Proxies.Get<global::System.Action<global::System.String, global::System.Int32>>("Space.Foo.OnBar")(a, b);
            }
            """
        ]
    ];
}
