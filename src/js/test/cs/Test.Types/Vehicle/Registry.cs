using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Types;

public class Registry
{
    public static IRegistryProvider Provider { get; set; }
    public List<Wheeled> Wheeled { get; set; }
    public List<Tracked> Tracked { get; set; }

    [JSInvokable]
    public static Registry EchoRegistry (Registry registry) => registry;

    [JSInvokable]
    public static float CountTotalSpeed ()
    {
        var registry = Provider.GetRegistry();
        return registry.Tracked.Sum(t => t.MaxSpeed) +
               registry.Wheeled.Sum(t => t.MaxSpeed);
    }

    [JSInvokable]
    public static async Task<IReadOnlyList<Registry>> ConcatRegistriesAsync (IReadOnlyList<Registry> registries)
    {
        await Task.Delay(1);
        return registries.Concat(Provider.GetRegistries()).ToArray();
    }

    [JSInvokable]
    public static async Task<IReadOnlyDictionary<string, Registry>> MapRegistriesAsync (IReadOnlyDictionary<string, Registry> map)
    {
        await Task.Delay(1);
        return map.Concat(Provider.GetRegistryMap()).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    [JSInvokable]
    public static Vehicle GetWithEmptyId () => new() { Id = "" };
}
