namespace Bootsharp.Publish.Test;

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

    [Fact]
    public void WhenTrimmingDisabledPreservesSourceMapReferences ()
    {
        Task.TrimmingEnabled = false;
        Project.WriteFile("dotnet.js", "//# sourceMappingURL=dotnet.js.map\n");
        Project.WriteFile("dotnet.runtime.js", "//# sourceMappingURL=dotnet.runtime.js.map\n");
        Execute();
        Assert.Contains("//# sourceMappingURL=dotnet.js.map\n", File.ReadAllText(Path.Combine(Project.Root, "dotnet.js")));
        Assert.Contains("//# sourceMappingURL=dotnet.runtime.js.map\n", File.ReadAllText(Path.Combine(Project.Root, "dotnet.runtime.js")));
    }

    [Fact]
    public void WhenTrimmingEnabledStripsSourceMapReference ()
    {
        Task.TrimmingEnabled = true;
        Project.WriteFile("dotnet.js", "//# sourceMappingURL=dotnet.js.map\n");
        Project.WriteFile("dotnet.runtime.js", "//# sourceMappingURL=dotnet.runtime.js.map\n");
        Execute();
        Assert.DoesNotContain("//# sourceMappingURL=dotnet.js.map\n", File.ReadAllText(Path.Combine(Project.Root, "dotnet.js")));
        Assert.DoesNotContain("//# sourceMappingURL=dotnet.runtime.js.map\n", File.ReadAllText(Path.Combine(Project.Root, "dotnet.runtime.js")));
    }
}
