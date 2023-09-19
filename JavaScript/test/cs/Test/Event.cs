using Bootsharp;

namespace Test.Main;

public static partial class Event
{
    [JSInvokable]
    public static void BroadcastEvent (string value) => OnEventBroadcast(value);

    [JSEvent]
    public static partial string OnEventBroadcast (string value);
}
