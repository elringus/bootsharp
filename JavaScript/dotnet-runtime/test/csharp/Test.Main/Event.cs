using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Main;

public static partial class Event
{
    [JSInvokable]
    public static void BroadcastEvent (string value) => OnEventBroadcast(value);

    [JSInvokable]
    public static void BroadcastStrings (string[] values) => OnStringsBroadcast(values);

    [JSEvent]
    public static partial string OnEventBroadcast (string value);

    [JSEvent]
    public static partial string OnStringsBroadcast (string[] values);
}
