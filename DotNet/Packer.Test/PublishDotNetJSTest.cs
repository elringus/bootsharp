using System;
using System.IO;
using Xunit;

namespace Packer.Test;

public sealed class PublishDotNetJSTest : IDisposable
{
    private readonly MockData data = new();
    private PublishDotNetJS task => data.Task;

    public void Dispose () => data.Dispose();

    [Fact]
    public void LibraryFilePublished ()
    {
        task.Execute();
        Assert.True(File.Exists(data.ResultLibraryFile));
    }

    [Fact]
    public void MapFileNotPublishedByDefault ()
    {
        task.Execute();
        Assert.False(File.Exists(data.ResultMapFile));
    }

    [Fact]
    public void MapFilePublishedWhenRequested ()
    {
        task.EmitSourceMap = true;
        task.Execute();
        Assert.True(File.Exists(data.ResultMapFile));
    }

    [Fact]
    public void TypesFileNotPublishedByDefault ()
    {
        task.Execute();
        Assert.False(File.Exists(data.ResultTypesFile));
    }

    [Fact]
    public void TypesFilePublishedWhenRequested ()
    {
        task.EmitTypes = true;
        task.Execute();
        Assert.True(File.Exists(data.ResultTypesFile));
    }

    [Fact]
    public void BaseDirectoryCleanedByDefault ()
    {
        var filePath = Path.Combine(data.BaseDir, "test");
        File.WriteAllText(filePath, "");
        task.Execute();
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void BaseDirectoryNotCleanedWhenDisabled ()
    {
        var filePath = Path.Combine(data.BaseDir, "test");
        File.WriteAllText(filePath, "");
        task.Clean = false;
        task.Execute();
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void WhenNoJSAttributesMethodsNotGenerated ()
    {
        data.AddBlazorOutAssembly(new Empty());
        task.EmitTypes = true;
        task.Execute();
        data.AssertExpectedJSGenerated();
    }

    [Fact]
    public void WhenBlazor ()
    {
        data.AddBlazorOutAssembly(new Diverse());
        task.Execute();
    }
}
