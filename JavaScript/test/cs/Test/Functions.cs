using System.Threading.Tasks;
using Bootsharp;

namespace Test;

public static partial class Functions
{
    [JSInvokable]
    public static string TestEchoFunction (string value) => EchoFunction(value);

    [JSFunction]
    public static partial string EchoFunction (string value);

    [JSInvokable]
    public static void TestAsyncEchoFunction (string value)
    {
        AsyncEchoFunction(value).ContinueWith(v => OnAsyncJSFunctionComplete(v.Result)).ConfigureAwait(false);
        return;
    }

    [JSFunction]
    public static partial Task<string> AsyncEchoFunction (string value);

    [JSInvokable]
    public static string[] TestArrayArgFunction (string[] values) => ArrayArgFunction(values);

    [JSFunction]
    public static partial string[] ArrayArgFunction (string[] values);

    [JSEvent]
    public static partial void OnAsyncJSFunctionComplete (string result);
}
