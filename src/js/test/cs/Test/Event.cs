using System;
using System.Threading.Tasks;
using Bootsharp;
using Test.Types;

namespace Test;

public delegate void VehicleEvent (byte num, Vehicle? vehicle, TrackType type);

public static partial class Event
{
    [Export] public static event VehicleEvent? OnVehicleEvent;
    [Export] public static event Action<string>? OnImportedEventEchoed;
    [Import] public static event Action<string>? OnImportedEvent;

    [Export]
    public static void BroadcastVehicleEvent (byte num, Vehicle? vehicle, TrackType type)
    {
        OnVehicleEvent?.Invoke(num, vehicle, type);
    }

    [Export]
    public static Task EchoImportedEventAsync ()
    {
        var tcs = new TaskCompletionSource();
        OnImportedEvent += Handle;
        return tcs.Task;

        void Handle (string value)
        {
            OnImportedEvent -= Handle;
            OnImportedEventEchoed?.Invoke(value);
            tcs.SetResult();
        }
    }
}
