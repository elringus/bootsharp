namespace Bootsharp.Generate.Test;

public static class FunctionTest
{
    public static object[][] Data { get; } = [
        // Can generate void binding under root namespace.
        [
            """
            partial class Foo
            {
                [JSFunction] partial void Bar ();
            }
            """,
            """
            unsafe partial class Foo
            {
                private static delegate* managed<void> Proxy_Foo_Bar;
                partial void Bar () => Proxy_Foo_Bar();
            }
            """
        ],
        // Can generate void task binding under file-scoped namespace.
        [
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                [JSFunction] private static partial Task BarAsync (string[] a, int? b);
            }
            """,
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static unsafe partial class Foo
            {
                private static delegate* managed<global::System.String[], global::System.Int32?, global::System.Threading.Tasks.Task> Proxy_File_Scoped_Foo_BarAsync;
                private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String[] a, global::System.Int32? b) => Proxy_File_Scoped_Foo_BarAsync(a, b);
            }
            """
        ],
        // Can generate value task binding.
        [
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                [JSFunction] private static partial Task<string?> BarAsync ();
            }
            """,
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static unsafe partial class Foo
            {
                private static delegate* managed<global::System.Threading.Tasks.Task<global::System.String?>> Proxy_File_Scoped_Foo_BarAsync;
                private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () => Proxy_File_Scoped_Foo_BarAsync();
            }
            """
        ],
        // Can generate custom types.
        [
            """
            public record Record;

            partial class Foo
            {
                [JSFunction] partial void Bar (Record a);
            }
            """,
            """
            unsafe partial class Foo
            {
                private static delegate* managed<global::Record, void> Proxy_Foo_Bar;
                partial void Bar (global::Record a) => Proxy_Foo_Bar(a);
            }
            """
        ],
        // Can generate under classic namespace.
        [
            """
            using System;
            using System.Threading.Tasks;

            namespace Classic
            {
                partial class Foo
                {
                    [JSFunction] public partial DateTime GetTime (DateTime time);
                    [JSFunction] public partial Task<DateTime> GetTimeAsync (DateTime time);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            namespace Classic
            {
                unsafe partial class Foo
                {
                    private static delegate* managed<global::System.DateTime, global::System.DateTime> Proxy_Classic_Foo_GetTime;
                    public partial global::System.DateTime GetTime (global::System.DateTime time) => Proxy_Classic_Foo_GetTime(time);
                    private static delegate* managed<global::System.DateTime, global::System.Threading.Tasks.Task<global::System.DateTime>> Proxy_Classic_Foo_GetTimeAsync;
                    public partial global::System.Threading.Tasks.Task<global::System.DateTime> GetTimeAsync (global::System.DateTime time) => Proxy_Classic_Foo_GetTimeAsync(time);
                }
            }
            """
        ],
        // Special corner case when UsingDirectiveSyntax.Name is null.
        [
            """
            using x = (System.String, System.Boolean);

            partial class Foo
            {
                [JSFunction] partial void Bar ();
            }
            """,
            """
            using x = (System.String, System.Boolean);

            unsafe partial class Foo
            {
                private static delegate* managed<void> Proxy_Foo_Bar;
                partial void Bar () => Proxy_Foo_Bar();
            }
            """
        ],
        // Doesn't add 'unsafe' class modified when it's already specified.
        [
            """
            unsafe partial class Foo
            {
                [JSFunction] partial void Bar ();
            }
            """,
            """
            unsafe partial class Foo
            {
                private static delegate* managed<void> Proxy_Foo_Bar;
                partial void Bar () => Proxy_Foo_Bar();
            }
            """
        ]
    ];
}
