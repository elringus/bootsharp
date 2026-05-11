using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Library;

public partial class Registries
{
    [Export] public static event Action<Vehicle?>? OnVehicleBroadcast;

    public static IRegistryProvider Provider { get; set; } = null!;

    [Export]
    public static IRegistry EchoRegistry (IRegistry registry)
    {
        registry.Wheeled = registry.Wheeled;
        registry.Tracked = registry.Tracked;
        return registry;
    }

    [Export] public static IRegistry MakeRegistry () => new Registry();
    [Export] public static Vehicle?[]? EchoVehicles (Vehicle?[]? value) => value;
    [Export] public static Record?[]? EchoRecords (Record?[]? value) => value;

    [Export]
    public static float CountTotalSpeed ()
    {
        var registry = Provider.GetRegistry();
        return registry.Tracked.Sum(t => t?.MaxSpeed ?? 0) +
               registry.Wheeled.Sum(t => t?.MaxSpeed ?? 0);
    }

    [Export]
    public static async Task<IReadOnlyList<IRegistry?>> ConcatRegistriesAsync (IReadOnlyList<IRegistry?> registries)
    {
        await Task.Delay(1);
        return registries.Concat(Provider.GetRegistries()).ToArray();
    }

    [Export]
    public static async Task<IReadOnlyDictionary<string, IRegistry>> MapRegistriesAsync
        (IReadOnlyDictionary<string, IRegistry> map)
    {
        await Task.Delay(1);
        return map.Concat(Provider.GetRegistryMap()).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    [Export] public static void BroadcastVehicle (Vehicle? vehicle) => OnVehicleBroadcast?.Invoke(vehicle);
    [Export] public static Vehicle GetVehicleWithEmptyId () => new() { Id = "" };
    [Export] public static Vehicle GetVehicle (string id, float maxSpeed) => CreateVehicle(id, maxSpeed);
    [Import] public static partial Vehicle CreateVehicle (string id, float maxSpeed);
}
