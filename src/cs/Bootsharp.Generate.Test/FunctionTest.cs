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
            partial class Foo
            {
                partial void Bar () => global::Bootsharp.Proxies.Get<global::System.Action>("Foo.Bar")();
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

            public static partial class Foo
            {
                private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String[] a, global::System.Int32? b) => global::Bootsharp.Proxies.Get<global::System.Func<global::System.String[], global::System.Int32?, global::System.Threading.Tasks.Task>>("File.Scoped.Foo.BarAsync")(a, b);
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

            public static partial class Foo
            {
                private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () => global::Bootsharp.Proxies.Get<global::System.Func<global::System.Threading.Tasks.Task<global::System.String?>>>("File.Scoped.Foo.BarAsync")();
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
            partial class Foo
            {
                partial void Bar (global::Record a) => global::Bootsharp.Proxies.Get<global::System.Action<global::Record>>("Foo.Bar")(a);
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
                partial class Foo
                {
                    public partial global::System.DateTime GetTime (global::System.DateTime time) => global::Bootsharp.Proxies.Get<global::System.Func<global::System.DateTime, global::System.DateTime>>("Classic.Foo.GetTime")(time);
                    public partial global::System.Threading.Tasks.Task<global::System.DateTime> GetTimeAsync (global::System.DateTime time) => global::Bootsharp.Proxies.Get<global::System.Func<global::System.DateTime, global::System.Threading.Tasks.Task<global::System.DateTime>>>("Classic.Foo.GetTimeAsync")(time);
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

            partial class Foo
            {
                partial void Bar () => global::Bootsharp.Proxies.Get<global::System.Action>("Foo.Bar")();
            }
            """
        ]
    ];
}
