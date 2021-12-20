using System;
using System.IO;
using Xunit;

namespace Packer.Test;

public sealed class PublishDotNetJSTest : IDisposable
{
    private readonly MockData data = new();

    public void Dispose () => data.Dispose();

    [Fact]
    public void BaseDirectoryCleanedByDefault ()
    {
        var filePath = Path.Combine(data.BaseDir, "test");
        File.WriteAllText(filePath, "");
        data.CreateTask("").Execute();
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void BaseDirectoryNotCleanedWhenDisabled ()
    {
        var filePath = Path.Combine(data.BaseDir, "test");
        File.WriteAllText(filePath, "");
        var task = data.CreateTask("");
        task.Clean = false;
        task.Execute();
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void WhenBlazor ()
    {
        data.AddBlazorOutAssembly("test.dll", @"
using DotNetJS;
using Microsoft.JSInterop;

public static class Program 
{ 
    [JSInvokable]
    public static void Foo () {}

    [JSFunction]
    public static string Bar () => """";
}
");
        data.CreateTask("test.dll").Execute();
    }
}
