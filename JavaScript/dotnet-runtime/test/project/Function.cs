using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Project;

public static partial class Function
{
    [JSInvokable]
    public static string TestEchoFunction (string value)
    {
        return EchoFunction(value);
    }

    [JSFunction]
    public static partial string EchoFunction (string value);
}
