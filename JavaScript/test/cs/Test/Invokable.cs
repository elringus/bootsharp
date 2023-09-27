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
    public static string JoinStrings (string a, string b) => a + b;

    [JSInvokable]
    public static double SumDoubles (double a, double b) => a + b;

    [JSInvokable]
    public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);

    [JSInvokable]
    public static async Task<string> JoinStringsAsync (string a, string b)
    {
        await Task.Delay(1).ConfigureAwait(false);
        return a + b;
    }

    [JSInvokable]
    public static string BytesToString (byte[] bytes) => Encoding.UTF8.GetString(bytes);
}
