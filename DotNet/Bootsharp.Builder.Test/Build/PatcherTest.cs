namespace Bootsharp.Builder.Test;

public class PatcherTest : BuildTest
{
    [Fact]
    public void WhenEmbedEnabledModulesAreCopied ()
    {
        Task.EmbedBinaries = true;
        Execute();
        Assert.Equal(MockRuntimeContent, GeneratedRuntimeModule);
        Assert.Equal(MockNativeContent, GeneratedNativeModule);
    }

    [Fact]
    public void WhenEmbedDisabledModulesAreUndefined ()
    {
        Task.EmbedBinaries = false;
        Execute();
        Assert.Equal("export default undefined;", GeneratedRuntimeModule);
        Assert.Equal("export default undefined;", GeneratedNativeModule);
    }
}
