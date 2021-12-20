using System;
using Xunit;

namespace Packer.Test;

public sealed class PublishDotNetJSTest : IDisposable
{
    private readonly MockData data = new();

    public void Dispose () => data.Dispose();

    [Fact]
    public void WhenBlazor ()
    {
        data.AddBlazorOutAssembly("lol.dll", @"
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
        data.CreateTask("lol.dll").Execute();
    }
}
