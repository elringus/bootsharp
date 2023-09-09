﻿namespace Bootsharp.Builder.Test;

public class InspectionTest : BuildBootsharpTest
{
    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("Foo.dll");
        Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("Foo.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("Bootsharp.dll"));
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
