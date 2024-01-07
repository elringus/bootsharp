namespace Bootsharp.Publish.Test;

public class EventTest : EmitTest
{
    protected override string TestedContent => GeneratedEvents;

    [Fact]
    public void WhenNothingInspectedNothingIsGenerated ()
    {
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void WhenNoEventsNothingIsGenerated ()
    {
        AddAssembly(WithClass("[JSFunction] public static void Foo () {}"));
        Execute();
        Assert.Empty(TestedContent);
    }
}
