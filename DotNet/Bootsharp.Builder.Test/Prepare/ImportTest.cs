namespace Bootsharp.Builder.Test;

public class ImportTest : PrepareTest
{
    protected override string TestedContent => GeneratedImports;

    [Fact]
    public void WhenNothingInspected ()
    {
        Execute();
        Contains(
            """

            """);
    }

    [Fact]
    public void WhenNoCrawledImports ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo () { }"));
        Execute();
        Contains(
            """

            """);
    }
}
