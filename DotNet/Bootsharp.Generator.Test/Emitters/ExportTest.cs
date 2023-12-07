namespace Bootsharp.Generator.Test;

public static class ExportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can export various APIs.
        new object[] {
            """
            using System.Threading.Tasks;

            [assembly:JSExport(typeof(IFoo))]

            public readonly record struct Item();

            public interface IFoo
            {
                void Foo (string? foo);
                Task Bar ();
                Item? Baz ();
                Task<string> Nya ();
                string[] Far (int[] far);
            }
            """,
            """
            namespace Foo;

            public class JSFoo
            {
                private static global::IFoo handler = null!;
            
                public JSFoo (global::IFoo handler)
                {
                    JSFoo.handler = handler;
                }
            
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(JSFoo), new ExportBinding(typeof(global::IFoo), handler => new JSFoo((global::IFoo)handler)));
                }
            
                [JSInvokable] public static void Foo (global::System.String? foo) => handler.Foo(foo);
                [JSInvokable] public static global::System.Threading.Tasks.Task Bar () => handler.Bar();
                [JSInvokable] public static global::Item? Baz () => handler.Baz();
                [JSInvokable] public static global::System.Threading.Tasks.Task<global::System.String> Nya () => handler.Nya();
                [JSInvokable] public static global::System.String[] Far (global::System.Int32[] far) => handler.Far(far);
            }
            """
        },
        // Can override name and invoke.
        new object[] {
            """
            [assembly:JSExport(typeof(IFoo), NamePattern="Foo", NameReplacement="Bar", InvokePattern="(.+)", InvokeReplacement="$1/**/")]

            public interface IFoo
            {
                void Foo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo
            {
                private static global::IFoo handler = null!;
            
                public JSFoo (global::IFoo handler)
                {
                    JSFoo.handler = handler;
                }
            
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(JSFoo), new ExportBinding(typeof(global::IFoo), handler => new JSFoo((global::IFoo)handler)));
                }
            
                [JSInvokable] public static void Bar (global::System.String foo) => handler.Foo(foo)/**/;
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(JSFoo), new ExportBinding(typeof(global::A.B.C.IFoo), handler => new JSFoo((global::A.B.C.IFoo)handler)));
                }
            
                [JSInvokable] public static void Foo () => handler.Foo();
            }
            """
        }
    };
}
