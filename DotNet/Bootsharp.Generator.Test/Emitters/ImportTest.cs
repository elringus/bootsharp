namespace Bootsharp.Generator.Test;

public static class ImportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can import various APIs; also, when event parameters are set to null, event is not detected.
        new object[] {
            """
            using System.Threading.Tasks;

            [assembly:JSImport(typeof(IFoo), EventPattern=null, EventReplacement=null)]

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

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSFunction] public static void NotifyFoo (global::System.String foo) => Get<global::System.Action<global::System.String>>("Foo.notifyFoo")(foo);
                [JSFunction] public static global::System.Boolean Bar () => Get<global::System.Func<global::System.Boolean>>("Foo.bar")();
                [JSFunction] public static global::System.Threading.Tasks.Task Nya () => Get<global::System.Func<global::System.Threading.Tasks.Task>>("Foo.nya")();
                [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> Far () => Get<global::System.Func<global::System.Threading.Tasks.Task<global::System.String>>>("Foo.far")();
            
                void global::IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
                global::System.Boolean global::IFoo.Bar () => Bar();
                global::System.Threading.Tasks.Task global::IFoo.Nya () => Nya();
                global::System.Threading.Tasks.Task<global::System.String> global::IFoo.Far () => Far();
            }
            """
        },
        // Detects and overrides event methods with defaults.
        new object[] {
            """
            [assembly:JSImport(typeof(IFoo))]

            public interface IFoo
            {
                void NotifyFoo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSEvent] public static void OnFoo (global::System.String foo) => Get<global::System.Action<global::System.String>>("Foo.onFoo")(foo);
            
                void global::IFoo.NotifyFoo (global::System.String foo) => OnFoo(foo);
            }
            """
        },
        // Can detect but not override event methods.
        new object[] {
            """
            [assembly:JSImport(typeof(IFoo), EventPattern=@"(^Notify)(\S+)", EventReplacement=null)]

            public interface IFoo
            {
                void NotifyFoo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSEvent] public static void NotifyFoo (global::System.String foo) => Get<global::System.Action<global::System.String>>("Foo.notifyFoo")(foo);
            
                void global::IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
            }
            """
        },
        // Can detect and override event methods.
        new object[] {
            """
            [assembly:JSImport(typeof(IFoo), EventPattern=@"(^Fire)(\S+)", EventReplacement="Handle$2")]

            public interface IFoo
            {
                void FireFoo (string foo);
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSEvent] public static void HandleFoo (global::System.String foo) => Get<global::System.Action<global::System.String>>("Foo.handleFoo")(foo);
            
                void global::IFoo.FireFoo (global::System.String foo) => HandleFoo(foo);
            }
            """
        },
        // Can override name and invoke.
        new object[] {
            """
            [assembly:JSImport(typeof(IFoo), NamePattern="Nya(.+)", NameReplacement="Nah$1", InvokePattern="(.+)", InvokeReplacement="$1/**/")]

            public interface IFoo
            {
                void NyaFoo (string foo);
                bool Bar ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSFunction] public static void NahFoo (global::System.String foo) => Get<global::System.Action<global::System.String>>("Foo.nahFoo")(foo)/**/;
                [JSFunction] public static global::System.Boolean Bar () => Get<global::System.Func<global::System.Boolean>>("Foo.bar")()/**/;
            
                void global::IFoo.NyaFoo (global::System.String foo) => NahFoo(foo);
                global::System.Boolean global::IFoo.Bar () => Bar();
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSFunction] public static void Foo () => Get<global::System.Action>("Foo.foo")();
            
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::A.B.C.IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSFunction] public static void F () => Get<global::System.Action>("Bar.f")();
            
                void global::A.B.C.IFoo.F () => F();
            }
            """
        },
        // Can import with serialized parameters.
        new object[] {
            """
            using System.Threading.Tasks;

            [assembly:JSImport(typeof(IFoo))]

            public record Info(string Baz);

            public interface IFoo
            {
                void NotifyFoo (Info info1, Info info2);
                Info Bar ();
                Task<Info> Far (Info info);
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::IFoo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo.JSFoo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies ()
                {
                    Bootsharp.BindingRegistry.Register(typeof(global::IFoo), new ImportBinding(new JSFoo()));
                }
            
                [JSEvent] public static void OnFoo (global::Info info1, global::Info info2) => Get<global::System.Action<global::System.String, global::System.String>>("Foo.onFoo")(Serialize(info1), Serialize(info2));
                [JSFunction] public static global::Info Bar () => Deserialize<global::Info>(Get<global::System.Func<global::System.String>>("Foo.bar")());
                [JSFunction] public static async global::System.Threading.Tasks.Task<global::Info> Far (global::Info info) => Deserialize<global::Info>(await Get<global::System.Func<global::System.String, global::System.Threading.Tasks.Task<global::System.String>>>("Foo.far")(Serialize(info)));
            
                void global::IFoo.NotifyFoo (global::Info info1, global::Info info2) => OnFoo(info1, info2);
                global::Info global::IFoo.Bar () => Bar();
                global::System.Threading.Tasks.Task<global::Info> global::IFoo.Far (global::Info info) => Far(info);
            }
            """
        }
    };
}
