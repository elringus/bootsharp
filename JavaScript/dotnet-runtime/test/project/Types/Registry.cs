using System.Collections.Generic;
using System.Linq;
using Microsoft.JSInterop;

namespace Test.Project.Types;

public class Registry
{
    public List<Wheeled> Wheeled { get; set; }
    public List<Tracked> Tracked { get; set; }

    [JSInvokable]
    public static Registry EchoRegistry (Registry registry) => registry;

    [JSInvokable]
    public static float CountTotalSpeed (Registry registry)
    {
        return registry.Tracked.Sum(t => t.MaxSpeed) +
               registry.Wheeled.Sum(t => t.MaxSpeed);
    }
}
