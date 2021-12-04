using System.Text.Json.Serialization;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Project;

public static class Program
{
    private static bool mainInvoked;

    static Program ()
    {
        JS.ConfigureJson(options =>
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
