using System.Collections.Generic;
using System.Linq;
using DotNetJS;
using Microsoft.JSInterop;

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

    [JSFunction]
    public static partial Registry GetRegistry ();
}
