using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Types;

public partial class Registry
{
    [Export] public static event Action<Vehicle?>? OnVehicleBroadcast;

    public static IRegistryProvider Provider { get; set; } = null!;
    public List<Wheeled?> Wheeled { get; set; } = null!;
    public List<Tracked?> Tracked { get; set; } = null!;

    [Export] public static Registry EchoRegistry (Registry registry) => registry;
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
    public static async Task<IReadOnlyList<Registry?>> ConcatRegistriesAsync (IReadOnlyList<Registry?> registries)
    {
        await Task.Delay(1);
        return registries.Concat(Provider.GetRegistries()).ToArray();
    }

    [Export]
    public static async Task<IReadOnlyDictionary<string, Registry>> MapRegistriesAsync
        (IReadOnlyDictionary<string, Registry> map)
    {
        await Task.Delay(1);
        return map.Concat(Provider.GetRegistryMap()).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    [Export] public static void BroadcastVehicle (Vehicle? vehicle) => OnVehicleBroadcast?.Invoke(vehicle);
    [Export] public static Vehicle GetVehicleWithEmptyId () => new() { Id = "" };
    [Export] public static Vehicle GetVehicle (string id, float maxSpeed) => CreateVehicle(id, maxSpeed);
    [Import] public static partial Vehicle CreateVehicle (string id, float maxSpeed);
}
