namespace Bootsharp.Publish.Test;

public class AssemblyInspectionTest : PackTest
{
    [Fact]
    public void Records ()
    {
        // TODO: Remove when coverlet bug is resolved: https://github.com/coverlet-coverage/coverlet/issues/1561
        _ = new Method { Name = "", Arguments = default, Assembly = "", Type = default, DeclaringName = "", ReturnsNullable = true, ReturnsVoid = true, ReturnType = default, ReturnTypeSyntax = "", JSSpace = "", JSName = "", ReturnsTaskLike = true, JSReturnTypeSyntax = "", ShouldSerializeReturnType = true };
    }

    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("Foo.dll");
        Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("Foo.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("Bootsharp.Common.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Runtime.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Private.CoreLib.dll"));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        File.WriteAllText(Path.Combine(Project.Root, "foo.dll"), "corrupted");
        Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }
}
