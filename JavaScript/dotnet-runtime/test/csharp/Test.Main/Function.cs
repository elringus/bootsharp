using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Main;

public static partial class Function
{
    [JSInvokable]
    public static string TestEchoFunction (string value) => EchoFunction(value);

    [JSFunction]
    public static partial string EchoFunction (string value);

    [JSInvokable]
    public static Task<string> TestAsyncEchoFunction (string value) => AsyncEchoFunction(value);

    [JSFunction]
    public static partial Task<string> AsyncEchoFunction (string value);
}
