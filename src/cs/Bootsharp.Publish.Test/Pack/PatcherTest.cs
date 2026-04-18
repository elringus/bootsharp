namespace Bootsharp.Publish.Test;

public class PatcherTest : PackTest
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

    [Fact]
    public void WhenDebugDisabledMapsAreRemoved ()
    {
        Task.Debug = false;
        Project.WriteFile("dotnet.js", "dotnet\n//# sourceMappingURL=dotnet.js.map\n");
        Project.WriteFile("dotnet.runtime.js", "runtime\n//# sourceMappingURL=dotnet.runtime.js.map\n");
        Project.WriteFile("dotnet.native.js", "native\n//# sourceMappingURL=dotnet.native.js.map\n");
        Execute();
        Assert.DoesNotContain("sourceMappingURL", Project.ReadFile("dotnet.js"));
        Assert.DoesNotContain("sourceMappingURL", Project.ReadFile("dotnet.runtime.js"));
        Assert.DoesNotContain("sourceMappingURL", Project.ReadFile("dotnet.runtime.js"));
    }

    [Fact]
    public void WhenDebugEnabledMapsArePreserved ()
    {
        Task.Debug = true;
        Project.WriteFile("dotnet.js", "dotnet\n//# sourceMappingURL=dotnet.js.map\n");
        Project.WriteFile("dotnet.runtime.js", "runtime\n//# sourceMappingURL=dotnet.runtime.js.map\n");
        Project.WriteFile("dotnet.native.js", "native\n//# sourceMappingURL=dotnet.native.js.map\n");
        Execute();
        Assert.Contains("sourceMappingURL", Project.ReadFile("dotnet.js"));
        Assert.Contains("sourceMappingURL", Project.ReadFile("dotnet.runtime.js"));
        Assert.Contains("sourceMappingURL", Project.ReadFile("dotnet.runtime.js"));
    }
}
