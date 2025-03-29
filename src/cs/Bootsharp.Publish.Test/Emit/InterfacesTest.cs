﻿namespace Bootsharp.Publish.Test;

public class InterfacesTest : EmitTest
{
    protected override string TestedContent => GeneratedInterfaces;

    [Fact]
    public void GeneratesImplementationForExportedStaticInterface ()
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
                        Interfaces.Register(typeof(Bootsharp.Generated.Exports.JSExported), new ExportInterface(typeof(global::IExported), handler => new Bootsharp.Generated.Exports.JSExported((global::IExported)handler)));
                    }
                }
            }
            """);
    }

    [Fact]
    public void GeneratesImplementationForImportedStaticInterface ()
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
                    [JSFunction] public static void Inv (global::System.String? a) => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_Inv(a);
                    [JSFunction] public static global::System.Threading.Tasks.Task InvAsync () => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_InvAsync();
                    [JSFunction] public static global::Record? InvRecord () => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_InvRecord();
                    [JSFunction] public static global::System.Threading.Tasks.Task<global::System.String> InvAsyncResult () => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_InvAsyncResult();
                    [JSFunction] public static global::System.String[] InvArray (global::System.Int32[] a) => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_InvArray(a);

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
    public void GeneratesImplementationForInstancedImportInterface ()
    {
        AddAssembly(With(
            """
            public interface IExported { void Inv (string arg); }
            public interface IImported { void Fun (string arg); void NotifyEvt(string arg); }

            public class Class
            {
                [JSInvokable] public static IExported GetExported () => default;
                [JSFunction] public static IImported GetImported () => Proxies.Get<Func<IImported>>("Class.GetImported")();
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Imports
            {
                public class JSImported(global::System.Int32 _id) : global::IImported
                {
                    ~JSImported() => global::Bootsharp.Generated.Interop.DisposeImportedInstance(_id);

                    [JSFunction] public static void Fun (global::System.Int32 _id, global::System.String arg) => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_Fun(_id, arg);
                    [JSEvent] public static void OnEvt (global::System.Int32 _id, global::System.String arg) => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_OnEvt(_id, arg);

                    void global::IImported.Fun (global::System.String arg) => Fun(_id, arg);
                    void global::IImported.NotifyEvt (global::System.String arg) => OnEvt(_id, arg);
                }
            }
            """);
        DoesNotContain("JSExported"); // Exported instances are authored by user and registered on initial interop.
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
                    [JSFunction] public static void Fun (global::Space.Record a) => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_Space_JSImported_Fun(a);

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
                        Interfaces.Register(typeof(Bootsharp.Generated.Exports.Space.JSExported), new ExportInterface(typeof(global::Space.IExported), handler => new Bootsharp.Generated.Exports.Space.JSExported((global::Space.IExported)handler)));
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
                    [JSEvent] public static void OnFoo () => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_OnFoo();

                    void global::IImported.NotifyFoo () => OnFoo();
                }
            }
            """);
    }

    [Fact]
    public void RespectsEventPreference ()
    {
        AddAssembly(With(
            """
            [assembly:JSPreferences(Event = [@"^Broadcast(\S+)", "On$1"])]
            [assembly:JSImport(typeof(IImported))]

            public interface IImported
            {
                void NotifyFoo ();
                void BroadcastBar ();
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Imports
            {
                public class JSImported : global::IImported
                {
                    [JSFunction] public static void NotifyFoo () => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_NotifyFoo();
                    [JSEvent] public static void OnBar () => global::Bootsharp.Generated.Interop.Proxy_Bootsharp_Generated_Imports_JSImported_OnBar();

                    void global::IImported.NotifyFoo () => NotifyFoo();
                    void global::IImported.BroadcastBar () => OnBar();
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
    public void IgnoresImplementedInterfaceMethods ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]
            [assembly:JSImport(typeof(IImportedStatic))]

            public interface IExportedStatic { int Foo () => 0; }
            public interface IImportedStatic { int Foo () => 0; }
            public interface IExportedInstanced { int Foo () => 0; }
            public interface IImportedInstanced { int Foo () => 0; }

            public class Class
            {
                [JSInvokable] public static IExportedInstanced GetExported () => default;
                [JSFunction] public static IImportedInstanced GetImported () => default;
            }
            """));
        Execute();
        DoesNotContain("Foo");
    }
}
