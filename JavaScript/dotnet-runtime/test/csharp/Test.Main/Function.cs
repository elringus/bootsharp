using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Main;

public static partial class Function
{
    [JSInvokable]
    public static string[] TestEchoFunction (string[] values) => EchoFunction(values);

    [JSFunction]
    public static partial string[] EchoFunction (string[] values);

    [JSInvokable]
    public static Task<string[]> TestAsyncEchoFunction (string[] values) => AsyncEchoFunction(values);

    [JSFunction]
    public static partial Task<string[]> AsyncEchoFunction (string[] values);
}
