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
        }
    };
}
