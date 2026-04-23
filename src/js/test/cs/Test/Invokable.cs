using System;
using System.Text;
using System.Threading.Tasks;
using Bootsharp;

namespace Test;

/// <summary>
/// Invokable test API.
/// </summary>
public static class Invokable
{
    /// <summary>
    /// Joins two strings.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>Joined string.</returns>
    [Export] public static string JoinStrings (string a, string b) => a + b;
    [Export] public static void InvokeVoid () { }
    [Export] public static double SumDoubles (double a, double b) => a + b;
    [Export] public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);
    [Export] public static Task<string> JoinStringsAsync (string a, string b) => Task.Delay(1).ContinueWith(_ => a + b);
    [Export] public static string BytesToString (byte[] bytes) => Encoding.UTF8.GetString(bytes);
    [Export] public static IdxEnum GetIdxEnumOne () => IdxEnum.One;
}
