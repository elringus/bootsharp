using System.Collections.Generic;
using System.Threading.Tasks;
using Bootsharp;

namespace Test;

public static partial class Functions
{
    [Import] public static partial string GetString ();
    [Import] public static partial void JSFunction ();
    [Import] public static partial Task<string> GetStringAsync ();
    [Import] public static partial byte[] GetBytes ();

    [Export] public static void InvokeJSFunction () => JSFunction();
    [Export] public static string EchoString () => GetString();
    [Export] public static Task<string> EchoStringAsync () => Task.Delay(1).ContinueWith(_ => GetStringAsync()).Unwrap();
    [Export] public static byte[] EchoBytes () => GetBytes();
    [Export] public static Task<byte[]> EchoBytesAsync (byte[] arr) => Task.Delay(1).ContinueWith(_ => arr);
    [Export] public static IList<string> EchoColExprString (IList<string> list) => [..list];
    [Export] public static IReadOnlyList<double> EchoColExprDouble (IReadOnlyList<double> list) => [..list];
    [Export] public static ICollection<int> EchoColExprInt (ICollection<int> list) => [..list];
    [Export] public static IReadOnlyCollection<byte> EchoColExprByte (IReadOnlyCollection<byte> list) => [..list];
    [Export] public static string[] EchoStringArray (string[] arr) => arr;
    [Export] public static double[] EchoDoubleArray (double[] arr) => arr;
    [Export] public static int[] EchoIntArray (int[] arr) => arr;
    [Export] public static byte[] EchoByteArray (byte[] arr) => arr;
}
