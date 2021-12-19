using System.Text.Json.Serialization;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Main;

public static class Program
{
    private static bool mainInvoked;

    static Program ()
    {
        JS.Runtime.ConfigureJson(options =>
            options.Converters.Add(new JsonStringEnumConverter())
        );
    }

    private static void Main ()
    {
        mainInvoked = true;
    }

    [JSInvokable]
    public static bool IsMainInvoked () => mainInvoked;
}
