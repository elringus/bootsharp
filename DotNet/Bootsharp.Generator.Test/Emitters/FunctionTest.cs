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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void Bar () => Get<global::System.Action>("Global.bar")();
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "File.Scoped.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                private static partial global::System.Threading.Tasks.Task BarAsync (global::System.String a, global::System.Int32 b) => Get<global::System.Func<global::System.String, global::System.Int32, global::System.Threading.Tasks.Task>>("File.Scoped.barAsync")(a, b);
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "File.Scoped.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                private static partial global::System.Threading.Tasks.Task<global::System.String?> BarAsync () => Get<global::System.Func<global::System.Threading.Tasks.Task<global::System.String?>>>("File.Scoped.barAsync")();
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
                    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Classic.Foo", "GeneratorTest")]
                    internal static void RegisterDynamicDependencies () { }

                    public partial global::System.DateTime GetTime (global::System.DateTime time) => Get<global::System.Func<global::System.DateTime, global::System.DateTime>>("Classic.getTime")(time);
                    public partial global::System.Threading.Tasks.Task<global::System.DateTime> GetTimeAsync (global::System.DateTime time) => Get<global::System.Func<global::System.DateTime, global::System.Threading.Tasks.Task<global::System.DateTime>>>("Classic.getTimeAsync")(time);
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
                public static partial void OnFun (bool val);
            }
            """,
            """
            namespace A.B.C;

            public partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "A.B.C.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                public static partial void OnFun (global::System.Boolean val) => Get<global::System.Action<global::System.Boolean>>("C.onFun")(val);
            }
            """
        },
        // Can generate void binding with serialized parameters.
        new object[] {
            """
            using System.Threading.Tasks;

            public record Info(string Baz);

            partial class Foo
            {
                [JSFunction]
                public partial Info Bar (Info info1, Info info2);
                [JSFunction]
                public partial Task<Info> BarAsync (Info info);
            }
            """,
            """
            using System.Threading.Tasks;

            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                public partial global::Info Bar (global::Info info1, global::Info info2) => Deserialize<global::Info>(Get<global::System.Func<global::System.String, global::System.String, global::System.String>>("Global.bar")(Serialize(info1), Serialize(info2)));
                public async partial global::System.Threading.Tasks.Task<global::Info> BarAsync (global::Info info) => Deserialize<global::Info>(await Get<global::System.Func<global::System.String, global::System.Threading.Tasks.Task<global::System.String>>>("Global.barAsync")(Serialize(info)));
            }
            """
        },
        // Doesn't serialize types that can be transferred as-is.
        new object[] {
            """
            using System;

            partial class Foo
            {
                [JSFunction]
                partial void Bar (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16);
            }
            """,
            """
            using System;

            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void Bar (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, global::System.DateTime a10, global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16) => Get<global::System.Action<global::System.Boolean, global::System.Byte, global::System.Char, global::System.Int16, global::System.Int64, global::System.Int32, global::System.Single, global::System.Double, global::System.IntPtr, global::System.DateTime, global::System.DateTimeOffset, global::System.String, global::System.Byte[], global::System.Int32[], global::System.Double[], global::System.String[]>>("Global.bar")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);
            }
            """
        }
    };
}
