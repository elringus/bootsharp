using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Types;

public partial class Registry
{
    public List<Wheeled> Wheeled { get; set; }
    public List<Tracked> Tracked { get; set; }

    [JSInvokable]
    public static Registry EchoRegistry (Registry registry) => registry;

    [JSInvokable]
    public static float CountTotalSpeed ()
    {
        var registry = GetRegistry();
        return registry.Tracked.Sum(t => t.MaxSpeed) +
               registry.Wheeled.Sum(t => t.MaxSpeed);
    }

    [JSInvokable]
    public static async Task<IReadOnlyList<Registry>> ConcatRegistriesAsync (IReadOnlyList<Registry> registries)
    {
        await Task.Delay(10);
        return registries.Concat(GetRegistries()).ToArray();
    }

    [JSInvokable]
    public static async Task<IReadOnlyDictionary<string, Registry>> MapRegistriesAsync (IReadOnlyDictionary<string, Registry> map)
    {
        await Task.Delay(10);
        return map.Concat(GetRegistryMap()).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    [JSFunction]
    public static partial Registry GetRegistry ();

    [JSFunction]
    public static partial IReadOnlyList<Registry> GetRegistries ();

    [JSFunction]
    public static partial IReadOnlyDictionary<string, Registry> GetRegistryMap ();
}
