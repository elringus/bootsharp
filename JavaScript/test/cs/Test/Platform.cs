using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bootsharp;

namespace Test.Main;

public static partial class Platform
{
    [JSInvokable]
    public static string GetGuid () => Guid.NewGuid().ToString();

    [JSInvokable]
    public static string CatchException ()
    {
        try { ThrowJS(); }
        catch (Exception e) { return e.Message; }
        return null;
    }

    [JSInvokable]
    public static string ThrowCS (string message) => throw new Exception(message);

    [JSFunction]
    private static partial void ThrowJS ();

    [JSInvokable]
    public static async Task<string> EchoViaWebSocket (string uri, string message, int timeout)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri(uri), cts.Token);
        var buffer = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
        await client.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        return Encoding.UTF8.GetString(buffer);
    }
}
