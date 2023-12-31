using Bootsharp;
using Test.Types;

namespace Test;

public static partial class Event
{
    [JSInvokable]
    public static void BroadcastEvent (string value) => OnEvent(value);

    [JSInvokable]
    public static void BroadcastEventMultiple (byte num, Vehicle? vehicle, TrackType type) => OnEventMultiple(num, vehicle, type);

    [JSEvent]
    public static partial string OnEvent (string value);

    [JSEvent]
    public static partial string OnEventMultiple (byte num, Vehicle? vehicle, TrackType type);
}
