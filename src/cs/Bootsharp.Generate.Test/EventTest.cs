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
                partial void OnBar () =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_Foo_OnBar();
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
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
                public static partial void OnBar (global::System.String a, global::System.Int32 b) =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_Space_Foo_OnBar(a, b);
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
            }
            """
        ]
    ];
}
