using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class FunctionTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can generate void binding under root namespace.
        new object[] {
            """
            partial class Foo
            {
                [JSFunction]
                partial void Bar ();
            }
            """,
            """
            partial class Foo
            {
                partial void Bar () => Function.InvokeVoid("Bindings.bar");
            }
            """
        },
        // Can generate void task binding under file-scoped namespace.
        new object[] {
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                [JSFunction]
                private static partial Task BarAsync (string a, int b);
            }
            """,
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                private static partial Task BarAsync (string a, int b) => Function.InvokeVoidAsync("File.Scoped.barAsync", a, b);
            }
            """
        },
        // Can generate value task binding.
        new object[] {
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                [JSFunction]
                private static partial Task<string?> BarAsync ();
            }
            """,
            """
            using System.Threading.Tasks;

            namespace File.Scoped;

            public static partial class Foo
            {
                private static partial Task<string?> BarAsync () => Function.InvokeAsync<string?>("File.Scoped.barAsync");
            }
            """
        },
        // Can generate under classic namespace.
        new object[] {
            """
            using System;
            using System.Threading.Tasks;

            namespace Classic
            {
                partial class Foo
                {
                    [JSFunction]
                    partial DateTime GetTime (DateTime time);
                    [JSFunction]
                    partial Task<DateTime> GetTimeAsync (DateTime time);
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
                    partial DateTime GetTime (DateTime time) => Function.Invoke<DateTime>("Classic.getTime", time);
                    partial Task<DateTime> GetTimeAsync (DateTime time) => Function.InvokeAsync<DateTime>("Classic.getTimeAsync", time);
                }
            }
            """
        },
        // Can override namespace.
        new object[] {
            """
            [assembly:JSNamespace(@"A\.B\.(\S+)", "$1")]

            namespace A.B.C;

            public partial class Foo
            {
                [JSFunction]
                public static partial void OnFun (Foo foo);
            }
            """,
            """
            namespace A.B.C;

            public partial class Foo
            {
                public static partial void OnFun (Foo foo) => Function.InvokeVoid("C.onFun", foo);
            }
            """
        }
    };
}
