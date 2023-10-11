namespace Bootsharp.Builder.Test;

public class InspectionTest : BuildTest
{
    [Fact]
    public void Records ()
    {
        // TODO: Remove once coverlet properly handles record coverage.
        _ = new Assembly("", default) with { Name = "foo" };
        _ = new MockAssembly("", default) with { Name = "foo" };
        _ = new AssemblyResource("", "") with { Name = "foo" };
        _ = new MockSource("", "", false) with { Namespace = "foo" };
        _ = new Argument { Name = "", Type = default, Nullable = false, TypeSyntax = "", ShouldSerialize = false } with { Name = "foo" };
        _ = new Method { Name = "", Arguments = default, Assembly = "", Type = default, DeclaringName = "", ReturnsNullable = true, ReturnsVoid = true, ReturnType = default, ReturnTypeSyntax = "", JSArguments = default, JSSpace = "", ReturnsTaskLike = true, JSReturnTypeSyntax = "", ShouldSerializeReturnType = true } with { Name = "foo" };
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
