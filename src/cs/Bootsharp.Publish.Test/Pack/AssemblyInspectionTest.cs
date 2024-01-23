namespace Bootsharp.Publish.Test;

public class AssemblyInspectionTest : PackTest
{
    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("foo.dll",
            WithClass("[JSInvokable] public static void Inv () {}")
        );
        Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("foo"));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        AddAssembly("foo.dll",
            WithClass("[JSInvokable] public static void InvFoo () {}")
        );
        AddAssembly("bar.dll",
            WithClass("[JSInvokable] public static void InvBar () {}")
        );
        File.WriteAllText(Path.Combine(Project.Root, "foo.dll"), "corrupted");
        Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }
}
