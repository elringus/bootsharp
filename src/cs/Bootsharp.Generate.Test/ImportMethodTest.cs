namespace Bootsharp.Generate.Test;

public static class ImportMethodTest
{
    public static TheoryData<string, string> Data { get; } = new() {
        // Void method under the root namespace.
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
        // Task method with array and nullable value parameters under a file-scoped namespace.
        {
            """
            namespace File.Scoped;

            public static partial class Foo
            {
                [Import] private static partial Task BarAsync (string[] a, int? b);
            }
            """,
            """
            namespace File.Scoped;

            public static unsafe partial class Foo
            {
                public static delegate* managed<global::System.String[], global::System.Int32?, global::System.Threading.Tasks.Task> Bootsharp_BarAsync;
                private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String[] a, global::System.Int32? b) => Bootsharp_BarAsync(a, b);
            }
            """
        },
        // Generic task method with a nullable reference type argument.
        {
            """
            namespace File.Scoped;

            public static partial class Foo
            {
                [Import] private static partial Task<string?> BarAsync ();
            }
            """,
            """
            namespace File.Scoped;

            public static unsafe partial class Foo
            {
                public static delegate* managed<global::System.Threading.Tasks.Task<global::System.String?>> Bootsharp_BarAsync;
                private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () => Bootsharp_BarAsync();
            }
            """
        },
        // Custom type under the global namespace.
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
        // Multiple methods under a classic namespace are emitted file-scoped.
        {
            """
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
            namespace Classic;

            unsafe partial class Foo
            {
                public static delegate* managed<global::System.DateTime, global::System.DateTime> Bootsharp_GetTime;
                public partial global::System.DateTime GetTime (global::System.DateTime time) => Bootsharp_GetTime(time);
                public static delegate* managed<global::System.DateTime, global::System.Threading.Tasks.Task<global::System.DateTime>> Bootsharp_GetTimeAsync;
                public partial global::System.Threading.Tasks.Task<global::System.DateTime> GetTimeAsync (global::System.DateTime time) => Bootsharp_GetTimeAsync(time);
            }
            """
        },
        // Method under a generic class.
        {
            """
            partial class Foo<T> where T : class
            {
                [Import] partial void Bar (T a);
            }
            """,
            """
            unsafe partial class Foo<T>
            {
                public static delegate* managed<T, void> Bootsharp_Bar;
                partial void Bar (T a) => Bootsharp_Bar(a);
            }
            """
        },
        // Existing 'unsafe' modifier is not duplicated.
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
