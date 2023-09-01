using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class ExportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can export various APIs.
        new object[] {
            """
            using System.Threading.Tasks;

            [assembly:JSExport(typeof(Bindings.IFoo))]

            namespace Bindings;

            public readonly record struct Item();

            public interface IFoo
            {
                void Foo (string? foo);
                ValueTask Bar ();
                Item? Baz ();
                Task<string> Nya ();
                string[] Far (int[] far);
            }
            """,
            """
            namespace Foo;

            public class JSFoo
            {
                private static global::Bindings.IFoo handler = null!;

                public JSFoo (global::Bindings.IFoo handler)
                {
                    JSFoo.handler = handler;
                }

                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSInvokable] public static void Foo (global::System.String? foo) => handler.Foo(foo);
                [JSInvokable] public static global::System.Threading.Tasks.ValueTask Bar () => handler.Bar();
                [JSInvokable] public static global::Bindings.Item? Baz () => handler.Baz();
                [JSInvokable] public static global::System.Threading.Tasks.Task<global::System.String> Nya () => handler.Nya();
                [JSInvokable] public static global::System.String[] Far (global::System.Int32[] far) => handler.Far(far);
            }
            """
        },
        // Can override name and invoke.
        new object[] {
            """
            [assembly:JSExport(typeof(Bindings.IFoo), NamePattern="Foo", NameReplacement="Bar", InvokePattern="(.+)", InvokeReplacement="Try($1)")]

            namespace Bindings;

            public interface IFoo
            {
                void Foo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo
            {
                private static global::Bindings.IFoo handler = null!;

                public JSFoo (global::Bindings.IFoo handler)
                {
                    JSFoo.handler = handler;
                }

                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSInvokable] public static void Bar (global::System.String foo) => Try(handler.Foo(foo));
            }
            """
        },
        // Can override namespace.
        new object[] {
            """
            [assembly:JSNamespace("Foo", "Bar")]
            [assembly:JSExport(typeof(A.B.C.IFoo))]

            namespace A.B.C;

            public interface IFoo
            {
                void Foo ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo
            {
                private static global::A.B.C.IFoo handler = null!;

                public JSFoo (global::A.B.C.IFoo handler)
                {
                    JSFoo.handler = handler;
                }

                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSInvokable] public static void Foo () => handler.Foo();
            }
            """
        }
    };
}
