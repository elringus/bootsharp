using System.IO;
using Xunit;

namespace Bootsharp.Builder.Test;

public class OtherTest : BuildTest
{
    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("Foo.dll");
        Task.Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("Foo.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("Bootsharp.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Runtime.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Private.CoreLib.dll"));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        File.WriteAllText(Path.Combine(Task.BuildDirectory, "foo.dll"), "corrupted");
        Task.Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }
}
