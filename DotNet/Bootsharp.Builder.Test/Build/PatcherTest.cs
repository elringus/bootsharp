namespace Bootsharp.Builder.Test;

public class PatcherTest : BuildTest
{
    [Fact]
    public void WhenEmbedEnabledModulesAreCopied ()
    {
        Task.EmbedBinaries = true;
        Execute();
        Assert.Equal(MockDotNetContent, GeneratedDotNetModule);
        Assert.Equal(MockRuntimeContent, GeneratedRuntimeModule);
        Assert.Equal(MockNativeContent, GeneratedNativeModule);
    }

    [Fact]
    public void WhenEmbedDisabledModuleExportFalseFlag ()
    {
        Task.EmbedBinaries = false;
        Execute();
        Assert.Equal("export const embedded = false;\nexport const mt = false;", GeneratedDotNetModule);
        Assert.Equal("export const embedded = false;\nexport const mt = false;", GeneratedRuntimeModule);
        Assert.Equal("export const embedded = false;\nexport const mt = false;", GeneratedNativeModule);
    }

    [Fact]
    public void WhenTreadingEnabledFlagIsSet ()
    {
        Task.EmbedBinaries = false;
        Task.Threading = true;
        Execute();
        Assert.Equal("export const embedded = false;\nexport const mt = true;", GeneratedDotNetModule);
        Assert.Equal("export const embedded = false;\nexport const mt = true;", GeneratedRuntimeModule);
        Assert.Equal("export const embedded = false;\nexport const mt = true;", GeneratedNativeModule);
    }
}
