using System.Linq;
using Bootsharp;
using Test.Types;

namespace Test;

public partial class Other
{
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
