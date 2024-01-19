namespace Bootsharp.Publish.Test;

public class InterfacesTest : EmitTest
{
    protected override string TestedContent => GeneratedInterfaces;

    [Fact]
    public void GeneratesInteropClassForExportedInterface ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExported))]

            public record Record;

            public interface IExported
            {
                void Inv (string? a);
                Task InvAsync ();
                Record? InvRecord ();
                Task<string> InvAsyncResult ();
                string[] InvArray (int[] a);
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Exports
            {
                public class JSExported
                {
                    private static global::IExported handler = null!;

                    public JSExported (global::IExported handler)
                    {
                        JSExported.handler = handler;
                    }

                    [JSInvokable] public static void Inv (global::System.String? a) => handler.Inv(a);
                    [JSInvokable] public static global::System.Threading.Tasks.Task InvAsync () => handler.InvAsync();
                    [JSInvokable] public static global::Record? InvRecord () => handler.InvRecord();
                    [JSInvokable] public static global::System.Threading.Tasks.Task<global::System.String> InvAsyncResult () => handler.InvAsyncResult();
                    [JSInvokable] public static global::System.String[] InvArray (global::System.Int32[] a) => handler.InvArray(a);
                }
            }
            """);
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(Bootsharp.Generated.Exports.JSExported), new ExportInterface(typeof(global::IExported), handler => new Bootsharp.Generated.Exports.JSExported(handler)));
                    }
                }
            }
            """);
    }

    [Fact]
    public void GeneratesImplementationForImportedInterface ()
    {
        AddAssembly(With(
            """
            [assembly:JSImport(typeof(IImported))]

            public record Record;

            public interface IImported
            {
                void Inv (string? a);
                Task InvAsync ();
                Record? InvRecord ();
                Task<string> InvAsyncResult ();
                string[] InvArray (int[] a);
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Imports
            {
                public class JSImported : global::IImported
                {
                    [JSFunction] public static void Inv (global::System.String? a) => Proxies.Get<Action<global::System.String?>>("Bootsharp.Generated.Imports.JSImported.Inv")(a);
                    [JSFunction] public static global::System.Threading.Tasks.Task InvAsync () => Proxies.Get<Func<global::System.Threading.Tasks.Task>>("Bootsharp.Generated.Imports.JSImported.InvAsync")();
                    [JSFunction] public static global::Record? InvRecord () => Proxies.Get<Func<global::Record?>>("Bootsharp.Generated.Imports.JSImported.InvRecord")();
                    [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> InvAsyncResult () => Proxies.Get<Func<global::System.Threading.Tasks.Task<global::System.String>>>("Bootsharp.Generated.Imports.JSImported.InvAsyncResult")();
                    [JSFunction] public static global::System.String[] InvArray (global::System.Int32[] a) => Proxies.Get<Func<global::System.Int32[], global::System.String[]>>("Bootsharp.Generated.Imports.JSImported.InvArray")(a);

                    void global::IImported.Inv (global::System.String? a) => Inv(a);
                    global::System.Threading.Tasks.Task global::IImported.InvAsync () => InvAsync();
                    global::Record? global::IImported.InvRecord () => InvRecord();
                    global::System.Threading.Tasks.Task<global::System.String> global::IImported.InvAsyncResult () => InvAsyncResult();
                    global::System.String[] global::IImported.InvArray (global::System.Int32[] a) => InvArray(a);
                }
            }
            """);
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(global::IImported), new ImportInterface(new Bootsharp.Generated.Imports.JSImported()));
                    }
                }
            }
            """);
    }

    [Fact]
    public void RespectsInterfaceNamespace ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(Space.IExported))]
            [assembly:JSImport(typeof(Space.IImported))]

            namespace Space;

            public record Record;

            public interface IExported { void Inv (Record a); }
            public interface IImported { void Fun (Record a); }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Exports.Space
            {
                public class JSExported
                {
                    private static global::Space.IExported handler = null!;

                    public JSExported (global::Space.IExported handler)
                    {
                        JSExported.handler = handler;
                    }

                    [JSInvokable] public static void Inv (global::Space.Record a) => handler.Inv(a);
                }
            }
            namespace Bootsharp.Generated.Imports.Space
            {
                public class JSImported : global::Space.IImported
                {
                    [JSFunction] public static void Fun (global::Space.Record a) => Proxies.Get<Action<global::Space.Record>>("Bootsharp.Generated.Imports.Space.JSImported.Fun")(a);

                    void global::Space.IImported.Fun (global::Space.Record a) => Fun(a);
                }
            }
            """);
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(Bootsharp.Generated.Exports.Space.JSExported), new ExportInterface(typeof(global::Space.IExported), handler => new Bootsharp.Generated.Exports.Space.JSExported(handler)));
                        Interfaces.Register(typeof(global::Space.IImported), new ImportInterface(new Bootsharp.Generated.Imports.Space.JSImported()));
                    }
                }
            }
            """);
    }

    [Fact]
    public void WhenImportedMethodStartsWithNotifyEmitsEvent ()
    {
        AddAssembly(With(
            """
            [assembly:JSImport(typeof(IImported))]

            public interface IImported { void NotifyFoo (); }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Imports
            {
                public class JSImported : global::IImported
                {
                    [JSEvent] public static void OnFoo () => Proxies.Get<Action>("Bootsharp.Generated.Imports.JSImported.OnFoo")();

                    void global::IImported.NotifyFoo () => OnFoo();
                }
            }
            """);
    }

    [Fact]
    public void RespectsResolveInterfacePref ()
    {
        AddAssembly(With(
            """
            [assembly:JSConfiguration<Prefs>]
            [assembly:JSImport(typeof(IImported))]

            public class Prefs : Bootsharp.Preferences
            {
                public override InterfaceMeta ResolveInterface (Type _, InterfaceKind __, InterfaceMeta @default)
                {
                    var method = ((IReadOnlyList<Bootsharp.InterfaceMethodMeta>)@default.Methods)[0];
                    return @default with {
                        Name = "Foo",
                        Methods = [method with { Generated = method.Generated with { Space = "Bootsharp.Generated.Imports.Foo" } }]
                    };
                }
            }

            public interface IImported { void NotifyEvt (); }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Imports
            {
                public class Foo : global::IImported
                {
                    [JSEvent] public static void OnEvt () => Proxies.Get<Action>("Bootsharp.Generated.Imports.Foo.OnEvt")();

                    void global::IImported.NotifyEvt () => OnEvt();
                }
            }
            """);
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(global::IImported), new ImportInterface(new Bootsharp.Generated.Imports.Foo()));
                    }
                }
            }
            """);
    }
}
