namespace Bootsharp.Publish.Test;

public class ImplementationsTest : EmitTest
{
    protected override string TestedContent => GeneratedImplementations;

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
            namespace Exports
            {
                public class JSExported : global::IExported
                {

                }
            }
            """);
    }
}
