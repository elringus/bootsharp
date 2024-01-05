namespace Bootsharp.Publish.Test;

public class AssemblyInspectionTest : PackTest
{
    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("Foo.dll");
        Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("Foo"));
        Assert.Contains(Engine.Messages, w => w.Contains("Bootsharp.Common"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Runtime"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Private.CoreLib"));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        File.WriteAllText(Path.Combine(Project.Root, "foo.dll"), "corrupted");
        Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }
}
