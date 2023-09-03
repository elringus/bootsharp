using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class ImportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can import various APIs; also, when event parameters are set to null, event is not detected.
        new object[] {
            """
            using System.Threading.Tasks;

            [assembly:JSImport(typeof(Global.IFoo), EventPattern=null, EventReplacement=null)]

            namespace Global;

            public interface IFoo
            {
                void NotifyFoo (string foo);
                bool Bar ();
                Task Nya ();
                Task<string> Far ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::Global.IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSFunction] public static void NotifyFoo (global::System.String foo) => Function.InvokeVoid("Foo.notifyFoo", foo);
                [JSFunction] public static global::System.Boolean Bar () => Function.Invoke<global::System.Boolean>("Foo.bar");
                [JSFunction] public static global::System.Threading.Tasks.Task Nya () => Function.InvokeVoidAsync("Foo.nya");
                [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> Far () => Function.InvokeAsync<global::System.String>("Foo.far");

                void global::Global.IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
                global::System.Boolean global::Global.IFoo.Bar () => Bar();
                global::System.Threading.Tasks.Task global::Global.IFoo.Nya () => Nya();
                global::System.Threading.Tasks.Task<global::System.String> global::Global.IFoo.Far () => Far();
            }
            """
        },
        // Can detect event methods.
        new object[] {
            """
            [assembly:JSImport(typeof(Global.IFoo), EventPattern=@"(^Notify)(\S+)", EventReplacement=null)]

            namespace Global;

            public interface IFoo
            {
                void NotifyFoo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::Global.IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSEvent] public static void NotifyFoo (global::System.String foo) => Event.Broadcast("Foo.notifyFoo", foo);

                void global::Global.IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
            }
            """
        },
        // Can detect and override event methods.
        new object[] {
            """
            [assembly:JSImport(typeof(Global.IFoo), EventPattern=@"(^Notify)(\S+)", EventReplacement="On$2")]

            namespace Global;

            public interface IFoo
            {
                void NotifyFoo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::Global.IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSEvent] public static void OnFoo (global::System.String foo) => Event.Broadcast("Foo.onFoo", foo);

                void global::Global.IFoo.NotifyFoo (global::System.String foo) => OnFoo(foo);
            }
            """
        },
        // Can override name and invoke.
        new object[] {
            """
            [assembly:JSImport(typeof(Global.IFoo), NamePattern="Nya(.+)", NameReplacement="Nah$1", InvokePattern="(.+)", InvokeReplacement="$1/**/")]

            namespace Global;

            public interface IFoo
            {
                void NyaFoo (string foo);
                bool Bar ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::Global.IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSFunction] public static void NahFoo (global::System.String foo) => Function.InvokeVoid("Foo.nahFoo", foo)/**/;
                [JSFunction] public static global::System.Boolean Bar () => Function.Invoke<global::System.Boolean>("Foo.bar")/**/;

                void global::Global.IFoo.NyaFoo (global::System.String foo) => NahFoo(foo);
                global::System.Boolean global::Global.IFoo.Bar () => Bar();
            }
            """
        },
        // When name and invoke don't have associated replacement parameter assigned, nothing is changed.
        new object[] {
            """
            [assembly:JSImport(typeof(IFoo), NamePattern="Foo", InvokePattern="(.+)")]

            public interface IFoo
            {
                void Foo ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSFunction] public static void Foo () => Function.InvokeVoid("Foo.foo");

                void global::IFoo.Foo () => Foo();
            }
            """
        },
        // Can override namespace.
        new object[] {
            """
            [assembly:JSNamespace("Foo", "Bar")]
            [assembly:JSImport(typeof(A.B.C.IFoo))]

            namespace A.B.C;

            public interface IFoo
            {
                void F ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::A.B.C.IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                [JSFunction] public static void F () => Function.InvokeVoid("Bar.f");

                void global::A.B.C.IFoo.F () => F();
            }
            """
        }
    };
}
