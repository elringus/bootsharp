using System;
using Xunit;
using static Bootsharp.Event;

namespace Bootsharp.Test;

public class EventTest
{
    [Fact]
    public void WhenBroadcastOutsideWasmExceptionIsThrown ()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Broadcast(""));
    }
}
