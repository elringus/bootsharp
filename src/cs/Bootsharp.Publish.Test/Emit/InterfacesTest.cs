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
                        Interfaces.Register(typeof(JSExported), new ExportInterface(typeof(global::IExported), handler => new JSExported(handler)));
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
                        Interfaces.Register(typeof(global::IImported), new ImportInterface(new JSImported()));
                    }
                }
            }
            """);
    }

    // TODO: Events
}
