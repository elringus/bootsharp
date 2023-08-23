using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class ImportTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        new object[] {
            """
            using System.Threading.Tasks;

            [assembly:JSImport(typeof(Bindings.IFoo))]

            namespace Bindings;

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

            public class JSFoo : global::Bindings.IFoo
            {
                [JSEvent] public static void NotifyFoo (global::System.String foo) => Event.Invoke("Foo/onFoo", SerializeArgs(foo));
                [JSFunction] public static global::System.Boolean Bar () => Function.Invoke<global::System.Boolean>("Foo/bar");
                [JSFunction] public static global::System.Threading.Tasks.Task Nya () => Function.InvokeVoidAsync("Foo/nya");
                [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> Far () => Function.InvokeAsync<global::System.String>("Foo/far");

                void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => NotifyFoo(foo);
                global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
                global::System.Threading.Tasks.Task global::Bindings.IFoo.Nya () => Nya();
                global::System.Threading.Tasks.Task<global::System.String> global::Bindings.IFoo.Far () => Far();
            }
            """
        },
        new object[] {
            """
            [assembly:JSImport(typeof(Bindings.IFoo), NamePattern="Notify(.+)", NameReplacement="On$1", InvokePattern="(.+)", InvokeReplacement="Try($1)")]

            namespace Bindings;

            public interface IFoo
            {
                void NotifyFoo (string foo);
                bool Bar ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::Bindings.IFoo
            {
                [JSEvent] public static void OnFoo (global::System.String foo) => Try(JS.Invoke("Foo/onFoo/broadcast", new object[] { foo }));
                [JSFunction] public static global::System.Boolean Bar () => Try(JS.Invoke<global::System.Boolean>("/Foo/bar"));

                void global::Bindings.IFoo.NotifyFoo (global::System.String foo) => OnFoo(foo);
                global::System.Boolean global::Bindings.IFoo.Bar () => Bar();
            }
            """
        },
        new object[] {
            """
            [assembly:JSNamespace(@"Foo", "Bar")]
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
                [JSFunction] public static void F () => JS.Invoke("Bar/f");

                void global::A.B.C.IFoo.F () => F();
            }
            """
        },
        new object[] {
            """
            [assembly:JSImport(typeof(IFoo), NamePattern="Foo", InvokePattern="Foo")]

            public interface IFoo
            {
                void Foo ();
            }
            """,
            """
            namespace Foo;

            public class JSFoo : global::Bindings.IFoo
            {
                [JSFunction] public static void Foo () => JS.Invoke("/Foo/foo");

                void global::Bindings.IFoo.Foo () => Foo();
            }
            """
        }
    };
}
