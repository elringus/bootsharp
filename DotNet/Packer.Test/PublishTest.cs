using System.IO;
using Xunit;

namespace Packer.Test;

public class PublishTest : BuildTest
{
    [Fact]
    public void LibraryPublished ()
    {
        Task.Execute();
        Assert.NotNull(Data.GeneratedLibrary);
    }

    [Fact]
    public void DeclarationPublished ()
    {
        Task.Execute();
        Assert.NotNull(Data.GeneratedDeclaration);
    }

    [Fact]
    public void SourceMapPublished ()
    {
        Task.Execute();
        Assert.NotNull(Data.GeneratedMap);
    }

    [Fact]
    public void BinariesEmbeddedByDefault ()
    {
        Task.Execute();
        Assert.Contains("bootWithData(bootData)", Data.GeneratedLibrary);
    }

    [Fact]
    public void BinariesNotEmbeddedWhenRequested ()
    {
        Task.EmbedBinaries = false;
        Task.Execute();
        Assert.DoesNotContain("bootWithData(bootData)", Data.GeneratedLibrary);
    }

    [Fact]
    public void AssembliesAndWasmArePublishedWhenEmbeddingDisabled ()
    {
        AddAssembly("Foo.dll");
        Task.EmbedBinaries = false;
        Task.Execute();
        Assert.True(File.Exists(Path.Combine(Data.PublishDir, Data.WasmFile)));
        Assert.True(File.Exists(Path.Combine(Data.PublishDir, "managed/Foo.dll")));
        Assert.True(File.Exists(Path.Combine(Data.PublishDir, "managed/DotNetJS.dll")));
    }

    [Fact]
    public void PublishDirectoryCleanedByDefault ()
    {
        var filePath = Path.Combine(Data.PublishDir, "test");
        File.WriteAllText(filePath, "");
        Task.Execute();
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void PublishDirectoryNotCleanedWhenRequested ()
    {
        var filePath = Path.Combine(Data.PublishDir, "test");
        File.WriteAllText(filePath, "");
        Task.Clean = false;
        Task.Execute();
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void AllAssembliesAreInspected ()
    {
        AddAssembly("Foo.dll");
        Task.Execute();
        Assert.Contains(Engine.Messages, w => w.Contains("Foo.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("DotNetJS.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Runtime.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("Microsoft.JSInterop.dll"));
        Assert.Contains(Engine.Messages, w => w.Contains("System.Private.CoreLib.dll"));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        File.WriteAllText(Path.Combine(Data.BlazorOutDir, "foo.dll"), "corrupted");
        Task.Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }
}
