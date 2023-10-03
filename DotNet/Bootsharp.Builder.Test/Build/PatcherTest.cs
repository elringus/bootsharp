namespace Bootsharp.Builder.Test;

public class PatcherTest : BuildTest
{
    [Fact]
    public void WhenThreadingDisabledModulesAreCopied ()
    {
        Task.Threading = false;
        Execute();
        Assert.Equal(MockRuntimeContent, GeneratedRuntimeModule);
        Assert.Equal(MockNativeContent, GeneratedNativeModule);
    }

    [Fact]
    public void WhenThreadingEnabledModulesAreUndefined ()
    {
        Task.Threading = true;
        Execute();
        Assert.Equal("export default undefined;", GeneratedRuntimeModule);
        Assert.Equal("export default undefined;", GeneratedNativeModule);
    }
}
