namespace Bootsharp.Builder.Test;

public class ExportTest : PrepareBootsharpTest
{
    protected override string TestedContent => GeneratedExports;

    [Fact]
    public void WhenNothingInspected ()
    {
        Execute();
        Contains(
            """

            """);
    }

    [Fact]
    public void WhenNoCrawledExports ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo () { }"));
        Execute();
        Contains(
            """

            """);
    }
}
