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
    public static IList<string> EchoColExprString (IList<string> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static IReadOnlyList<double> EchoColExprDouble (IReadOnlyList<double> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static ICollection<int> EchoColExprInt (ICollection<int> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static IReadOnlyCollection<byte> EchoColExprByte (IReadOnlyCollection<byte> list)
    {
        return [..list];
    }

    [JSInvokable]
    public static string[] EchoStringArray (string[] arr) => arr;
    [JSInvokable]
    public static double[] EchoDoubleArray (double[] arr) => arr;
    [JSInvokable]
    public static int[] EchoIntArray (int[] arr) => arr;
    [JSInvokable]
    public static byte[] EchoByteArray (byte[] arr) => arr;
}
