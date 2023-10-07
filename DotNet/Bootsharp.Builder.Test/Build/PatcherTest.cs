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
    public void WhenEmbedDisabledModuleExportFalseFlag ()
    {
        Task.EmbedBinaries = false;
        Execute();
        Assert.Equal("export const embedded = false;\n", GeneratedRuntimeModule);
        Assert.Equal("export const embedded = false;\n", GeneratedNativeModule);
    }

    [Fact]
    public void CanBuildWithThreadingEnabled ()
    {
        Task.Threading = true;
        Execute();
    }
}
