﻿using System.Collections.Generic;

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
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void Bar () => Function.InvokeVoid("Global.bar");
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
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "File.Scoped.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

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
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "File.Scoped.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

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
                    public partial DateTime GetTime (DateTime time);
                    [JSFunction]
                    public partial Task<DateTime> GetTimeAsync (DateTime time);
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
                    [ModuleInitializer]
                    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Classic.Foo", "GeneratorTest")]
                    internal static void RegisterDynamicDependencies () { }

                    public partial DateTime GetTime (DateTime time) => Function.Invoke<DateTime>("Classic.getTime", time);
                    public partial Task<DateTime> GetTimeAsync (DateTime time) => Function.InvokeAsync<DateTime>("Classic.getTimeAsync", time);
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
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "A.B.C.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                public static partial void OnFun (Foo foo) => Function.InvokeVoid("C.onFun", foo);
            }
            """
        }
    };
}