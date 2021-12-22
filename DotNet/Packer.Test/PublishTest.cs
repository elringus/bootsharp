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
    public void MapNotPublishedByDefault ()
    {
        Task.Execute();
        Assert.Null(Data.GeneratedMap);
    }

    [Fact]
    public void MapPublishedWhenRequested ()
    {
        Task.EmitSourceMap = true;
        Task.Execute();
        Assert.NotNull(Data.GeneratedMap);
    }

    [Fact]
    public void TypesNotPublishedByDefault ()
    {
        Task.Execute();
        Assert.Null(Data.GeneratedTypes);
    }

    [Fact]
    public void TypesPublishedWhenRequested ()
    {
        Task.EmitTypes = true;
        Task.Execute();
        Assert.NotNull(Data.GeneratedTypes);
    }

    [Fact]
    public void BaseDirectoryCleanedByDefault ()
    {
        var filePath = Path.Combine(Data.BaseDir, "test");
        File.WriteAllText(filePath, "");
        Task.Execute();
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void BaseDirectoryNotCleanedWhenRequested ()
    {
        var filePath = Path.Combine(Data.BaseDir, "test");
        File.WriteAllText(filePath, "");
        Task.Clean = false;
        Task.Execute();
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        File.WriteAllText(Path.Combine(Data.BlazorOutDir, "foo.dll"), "corrupted");
        Task.Execute();
        Assert.Contains(Engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }
}
