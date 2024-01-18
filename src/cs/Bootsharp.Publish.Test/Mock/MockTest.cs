using Xunit.Sdk;

namespace Bootsharp.Publish.Test;

public class MockTest
{
    [Fact]
    public void WhenCompileFailsIncludesSourceAndError ()
    {
        using var project = new MockProject();
        var asm = new MockAssembly("asm.dll", [new(null, "foo", false)]);
        Assert.Contains("Invalid test source code", Assert.Throws<FailException>(() => project.AddAssembly(asm)).Message);
        Assert.Contains("foo", Assert.Throws<FailException>(() => project.AddAssembly(asm)).Message);
    }
}
