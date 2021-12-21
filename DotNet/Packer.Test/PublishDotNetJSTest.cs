using System;
using System.IO;
using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

namespace Packer.Test;

public sealed class PublishDotNetJSTest : IDisposable
{
    private readonly MockData data = new();
    private PublishDotNetJS task => data.Task;
    private BuildEngine engine => (BuildEngine)data.Task.BuildEngine;

    public void Dispose () => data.Dispose();

    [Fact]
    public void LibraryFilePublished ()
    {
        task.Execute();
        Assert.True(File.Exists(data.ResultLibraryFile));
    }

    [Fact]
    public void LibraryStartsWithJSRuntime ()
    {
        task.Execute();
        var libraryContent = File.ReadAllText(data.ResultLibraryFile);
        Assert.StartsWith(MockData.JSFileContent, libraryContent);
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
    public void TypesFileContainsInteropAndBootContentWithoutImport ()
    {
        task.EmitTypes = true;
        task.Execute();
        var typesContent = File.ReadAllText(data.ResultTypesFile);
        Assert.Contains(MockData.InteropTypeContent, typesContent);
        Assert.Contains(MockData.BootTypeContent.Split('\n')[1], typesContent);
    }

    [Fact]
    public void TypesFileDoesntContainOtherContent ()
    {
        task.EmitTypes = true;
        File.WriteAllText(Path.Combine(data.JSDir, "other.d.ts"), "other");
        task.Execute();
        Assert.DoesNotContain(File.ReadAllText(data.ResultTypesFile), "other");
    }

    [Fact]
    public void WhenTypeResolveFailsExceptionIsThrown ()
    {
        task.EmitTypes = true;
        File.Delete(Path.Combine(data.JSDir, "interop.d.ts"));
        Assert.Throws<PackerException>(() => task.Execute());
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
    public void BaseDirectoryNotCleanedWhenRequested ()
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
        data.AssertExpectedCodeGenerated();
    }

    [Fact]
    public void WhenAssemblyInspectionFailsWarningIsLogged ()
    {
        File.WriteAllText(Path.Combine(data.BlazorOutDir, "foo.dll"), "corrupted");
        task.Execute();
        Assert.Contains(engine.Warnings, w => w.Contains("Failed to inspect 'foo.dll' assembly"));
    }

    [Fact]
    public void CodeGeneratedForAllJSAttributedMethods ()
    {
        data.AddBlazorOutAssembly(new Diverse());
        task.EmitTypes = true;
        task.Execute();
        data.AssertExpectedCodeGenerated();
    }

    [Fact]
    public void CodeGeneratedForMultipleAssemblies ()
    {
        data.AddBlazorOutAssembly(new Diverse());
        data.AddBlazorOutAssembly(new Diverse());
        task.EmitTypes = true;
        task.Execute();
        data.AssertExpectedCodeGenerated();
    }
}
