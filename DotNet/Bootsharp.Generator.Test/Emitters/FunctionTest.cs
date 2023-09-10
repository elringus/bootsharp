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

                partial void Bar () => Get<Action>("Global.bar")();
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

                private static partial Task BarAsync (string a, int b) => Get<Func<string, int, Task>>("File.Scoped.barAsync")(a, b);
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

                private static partial Task<string?> BarAsync () => Get<Func<Task<string?>>>("File.Scoped.barAsync")();
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

                    public partial DateTime GetTime (DateTime time) => Get<Func<DateTime, DateTime>>("Classic.getTime")(time);
                    public partial Task<DateTime> GetTimeAsync (DateTime time) => Get<Func<DateTime, Task<DateTime>>>("Classic.getTimeAsync")(time);
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

                public static partial void OnFun (bool val) => Get<Action<bool>>("C.onFun")(val);
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

                public partial Info Bar (Info info1, Info info2) => Deserialize<Info>(Get<Func<string, string, string>>("Global.bar")(Serialize(info1), Serialize(info2)));
                public async partial Task<Info> BarAsync (Info info) => Deserialize<Info>(await Get<Func<string, Task<string>>>("Global.barAsync")(Serialize(info)));
            }
            """
        }
    };
}
