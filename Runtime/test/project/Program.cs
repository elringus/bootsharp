using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace Test;

public static class Program
{
    private class JSRuntime : WebAssemblyJSRuntime { }

    private static readonly JSRuntime js = new();

    // Entry point is required by the runtime.
    private static void Main () { }

    [JSInvokable]
    public static string JoinStrings (string a, string b) => a + b;

    [JSInvokable]
    public static double SumDoubles (double a, double b) => a + b;

    [JSInvokable]
    public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);

    [JSInvokable]
    public static void InvokeJS (string funcName) => js.InvokeVoid(funcName);

    [JSInvokable]
    public static string[] ForEachJS (string[] items, string funcName)
    {
        for (int i = 0; i < items.Length; i++)
            items[i] = js.Invoke<string>(funcName, items[i]);
        return items;
    }

    [JSInvokable]
    public static async Task<string> JoinStringsAsync (string a, string b)
    {
        await Task.Yield();
        return a + b;
    }

    [JSInvokable("EchoAlias")]
    public static string Echo (string message) => message;

    [JSInvokable]
    public static string ReceiveBytes (byte[] bytes) => Encoding.UTF8.GetString(bytes);

    [JSInvokable]
    public static string SendBytes ()
    {
        var bytes = new byte[] {
            0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
            0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
            0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
            0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e
        };
        return js.Invoke<string>("receiveBytes", bytes);
    }

    [JSInvokable]
    public static void StartStream ()
    {
        Task.Run(StreamAsync);

        static async void StreamAsync ()
        {
            var inRef = await js.InvokeAsync<IJSStreamReference>("sendStream");
            await using var inStream = await inRef.OpenReadStreamAsync(maxAllowedSize: 10_000_000);
            var buffer = new byte[inStream.Length];
            await inStream.ReadAsync(buffer);
            await using var outStream = new MemoryStream(buffer);
            var outRef = new DotNetStreamReference(outStream, false);
            await js.InvokeVoidAsync("receiveStream", outRef);
        }
    }

    [JSInvokable]
    public static string CatchException ()
    {
        try { js.InvokeVoid("throw"); }
        catch (JSException e) { return e.Message; }
        return null;
    }

    [JSInvokable]
    public static string Throw (string message) => throw new Exception(message);
}

public class Instance
{
    private string var;

    [JSInvokable]
    public static DotNetObjectReference<Instance> CreateInstance ()
    {
        return DotNetObjectReference.Create(new Instance());
    }

    [JSInvokable]
    public void SetVar (string value) => var = value;

    [JSInvokable]
    public string GetVar () => var;

    [JSInvokable]
    public string SetFromOther (DotNetObjectReference<Instance> objRef) => var = objRef.Value.var;
}
