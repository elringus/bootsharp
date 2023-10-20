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
    public static string[] TestArrayArgFunction (string[] values) => ArrayArgFunction(values);

    [JSFunction]
    public static partial string[] ArrayArgFunction (string[] values);

    [JSInvokable]
    public static byte[] EchoBytes () => GetBytes();

    [JSInvokable]
    public static Task<byte[]> EchoBytesAsync () => GetBytesAsync();

    [JSFunction]
    public static partial byte[] GetBytes ();

    [JSFunction]
    public static partial Task<byte[]> GetBytesAsync ();
}
