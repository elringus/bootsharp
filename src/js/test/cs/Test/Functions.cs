using System.Threading.Tasks;
using Bootsharp;

namespace Test;

public static partial class Functions
{
    [JSInvokable]
    public static string EchoString () => GetString();

    [JSFunction]
    public static partial string GetString ();

    [JSInvokable]
    public static async Task<string> EchoStringAsync ()
    {
        await Task.Delay(1);
        return await GetStringAsync();
    }

    [JSFunction]
    public static partial Task<string> GetStringAsync ();

    [JSInvokable]
    public static byte[] EchoBytes () => GetBytes();

    [JSFunction]
    public static partial byte[] GetBytes ();
}
