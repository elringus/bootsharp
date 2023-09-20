using System;
using System.Text;
using System.Threading.Tasks;
using Bootsharp;

namespace Test;

public static partial class Invokable
{
    [JSInvokable]
    public static void InvokeVoid () { }

    [JSInvokable]
    public static string Echo (string message) => message;

    [JSInvokable]
    public static string JoinStrings (string a, string b) => a + b;

    [JSInvokable]
    public static double SumDoubles (double a, double b) => a + b;

    [JSInvokable]
    public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);

    [JSInvokable]
    public static async Task<string> JoinStringsAsync (string a, string b)
    {
        await Task.Yield();
        return a + b;
    }

    [JSInvokable]
    public static string BytesToString (byte[] bytes) => Encoding.UTF8.GetString(bytes);

    [JSInvokable]
    public static byte[] GetBytes () => new byte[] {
        0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
        0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
        0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
        0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e
    };
}
