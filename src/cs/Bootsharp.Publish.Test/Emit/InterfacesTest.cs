namespace Bootsharp.Publish.Test;

public class InterfacesTest : EmitTest
{
    protected override string TestedContent => GeneratedInterfaces;

    [Fact]
    public void GeneratesImplementationForExportedInterface ()
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
}
