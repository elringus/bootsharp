using System;
using System.Threading.Tasks;
using Xunit;

namespace DotNetJS.Test;

public class JSTest
{
    [Fact]
    public void CanUseCustomRuntime ()
    {
        var customRuntime = new MockRuntime();
        JS.Runtime = customRuntime;
        Assert.Same(customRuntime, JS.Runtime);
    }

    [Fact]
    public async Task InvokeMethodsAreRoutedToRuntime ()
    {
        JS.Runtime = new MockRuntime();
        Assert.Throws<NotImplementedException>(() => JS.Invoke(""));
        Assert.Throws<NotImplementedException>(() => JS.Invoke<int>(""));
        await Assert.ThrowsAsync<NotImplementedException>(() => JS.InvokeAsync<int>("").AsTask());
        await Assert.ThrowsAsync<NotImplementedException>(() => JS.InvokeAsync("").AsTask());
    }
}
