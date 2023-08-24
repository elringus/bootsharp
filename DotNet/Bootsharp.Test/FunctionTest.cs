using System;
using System.Threading.Tasks;
using Xunit;
using static Bootsharp.Function;

namespace Bootsharp.Test;

public class FunctionTest
{
    [Fact]
    public async Task WhenInvokedOutsideWasmExceptionIsThrown ()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Invoke<object>(""));
        Assert.Throws<PlatformNotSupportedException>(() => InvokeVoid(""));
        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => InvokeAsync<object>(""));
        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => InvokeVoidAsync(""));
    }
}
