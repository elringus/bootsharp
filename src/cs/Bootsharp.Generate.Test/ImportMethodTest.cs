namespace Bootsharp.Generate.Test;

public static class ImportMethodTest
{
    public static TheoryData<string, string> Data { get; } = new() {
        // Can generate void import method under root namespace.
        {
            """
            partial class Foo
            {
                [Import] partial void Bar ();
            }
            """,
            """
            unsafe partial class Foo
            {
                public static delegate* managed<void> Bootsharp_Bar;
                partial void Bar () => Bootsharp_Bar();
            }
            """
        },
        // Can generate void task import method under file-scoped namespace.
        {
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                [Import] private static partial Task BarAsync (string[] a, int? b);
            }
            """,
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static unsafe partial class Foo
            {
                public static delegate* managed<global::System.String[], global::System.Int32?, global::System.Threading.Tasks.Task> Bootsharp_BarAsync;
                private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String[] a, global::System.Int32? b) => Bootsharp_BarAsync(a, b);
            }
            """
        },
        // Can generate value task import method.
        {
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                [Import] private static partial Task<string?> BarAsync ();
            }
            """,
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static unsafe partial class Foo
            {
                public static delegate* managed<global::System.Threading.Tasks.Task<global::System.String?>> Bootsharp_BarAsync;
                private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () => Bootsharp_BarAsync();
            }
            """
        },
        // Can generate custom types.
        {
            """
            public record Record;

            partial class Foo
            {
                [Import] partial void Bar (Record a);
            }
            """,
            """
            unsafe partial class Foo
            {
                public static delegate* managed<global::Record, void> Bootsharp_Bar;
                partial void Bar (global::Record a) => Bootsharp_Bar(a);
            }
            """
        },
        // Can generate under classic namespace.
        {
            """
            using System;
            using System.Threading.Tasks;

            namespace Classic
            {
                partial class Foo
                {
                    [Import] public partial DateTime GetTime (DateTime time);
                    [Import] public partial Task<DateTime> GetTimeAsync (DateTime time);
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
                    public static delegate* managed<global::System.DateTime, global::System.DateTime> Bootsharp_GetTime;
                    public partial global::System.DateTime GetTime (global::System.DateTime time) => Bootsharp_GetTime(time);
                    public static delegate* managed<global::System.DateTime, global::System.Threading.Tasks.Task<global::System.DateTime>> Bootsharp_GetTimeAsync;
                    public partial global::System.Threading.Tasks.Task<global::System.DateTime> GetTimeAsync (global::System.DateTime time) => Bootsharp_GetTimeAsync(time);
                }
            }
            """
        },
        // Special corner case when UsingDirectiveSyntax.Name is null.
        {
            """
            using x = (System.String, System.Boolean);

            partial class Foo
            {
                [Import] partial void Bar ();
            }
            """,
            """
            using x = (System.String, System.Boolean);

            unsafe partial class Foo
            {
                public static delegate* managed<void> Bootsharp_Bar;
                partial void Bar () => Bootsharp_Bar();
            }
            """
        },
        // Doesn't add 'unsafe' class modified when it's already specified.
        {
            """
            unsafe partial class Foo
            {
                [Import] partial void Bar ();
            }
            """,
            """
            unsafe partial class Foo
            {
                public static delegate* managed<void> Bootsharp_Bar;
                partial void Bar () => Bootsharp_Bar();
            }
            """
        }
    };
}
