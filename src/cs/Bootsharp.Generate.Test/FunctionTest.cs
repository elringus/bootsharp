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
                partial void Bar () =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_Foo_Bar();
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
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
                private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String[] a, global::System.Int32? b) =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_File_Scoped_Foo_BarAsync(a, b);
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
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
                private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_File_Scoped_Foo_BarAsync();
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
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
                partial void Bar (global::Record a) =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_Foo_Bar(a);
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
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
                    public partial global::System.DateTime GetTime (global::System.DateTime time) =>
                    #if BOOTSHARP_EMITTED
                    global::Bootsharp.Generated.Interop.Proxy_Classic_Foo_GetTime(time);
                    #else
                    throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                    #endif
                    public partial global::System.Threading.Tasks.Task<global::System.DateTime> GetTimeAsync (global::System.DateTime time) =>
                    #if BOOTSHARP_EMITTED
                    global::Bootsharp.Generated.Interop.Proxy_Classic_Foo_GetTimeAsync(time);
                    #else
                    throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                    #endif
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
                partial void Bar () =>
                #if BOOTSHARP_EMITTED
                global::Bootsharp.Generated.Interop.Proxy_Foo_Bar();
                #else
                throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                #endif
            }
            """
        ]
    ];
}
