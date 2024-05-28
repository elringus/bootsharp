using System.Collections.Generic;
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

    [JSInvokable]
    public static async Task<byte[]> EchoBytesAsync (byte[] arr)
    {
        await Task.Delay(1);
        return arr;
    }

    [JSInvokable]
    public static IReadOnlyList<string> EchoColExprString (IReadOnlyList<string> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static IReadOnlyList<double> EchoColExprDouble (IReadOnlyList<double> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static IReadOnlyList<int> EchoColExprInt (IReadOnlyList<int> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static IReadOnlyList<byte> EchoColExprByte (IReadOnlyList<byte> list)
    {
        return [..list];
    }
}
