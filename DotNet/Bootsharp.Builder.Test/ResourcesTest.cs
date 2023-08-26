using Xunit;

namespace Bootsharp.Builder.Test;

public class ResourcesTest : ContentTest
{
    protected override string TestedContent => GeneratedResources;

    [Fact]
    public void ResourceNamesAreWritten ()
    {
        AddAssembly("Foo.dll");
        Task.Execute();
        Contains("exports.getBootUris = () => ({");
        Contains("wasm: \"dotnet.wasm\"");
        Contains("entryAssembly: \"Foo.dll\"");
        Contains("assemblies: [");
        Contains("Foo.dll");
        Contains("Bootsharp.dll");
    }

    [Fact]
    public void BinariesEmbeddedWhenEnabled ()
    {
        Task.EmbedBinaries = true;
        Task.Execute();
        Assert.NotEmpty(Matches("TODO: embed bins regex"));
    }

    [Fact]
    public void BinariesNotEmbeddedWhenDisabled ()
    {
        Task.EmbedBinaries = false;
        Task.Execute();
        Assert.Empty(Matches("TODO: embed bins regex"));
    }
}
