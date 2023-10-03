using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bootsharp;

namespace Test;

public static partial class Platform
{
    [JSInvokable]
    public static string GetGuid () => Guid.NewGuid().ToString();

    [JSInvokable]
    public static string? CatchException ()
    {
        try { ThrowJS(); }
        catch (Exception e) { return e.Message; }
        return null;
    }

    [JSInvokable]
    public static string ThrowCS (string message) => throw new Exception(message);

    [JSFunction]
    public static partial void ThrowJS ();

    [JSInvokable]
    public static async Task<string> EchoWebSocket (string uri, string message, int timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(uri), cts.Token);
        var buffer = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
        await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        return Encoding.UTF8.GetString(buffer);
    }
}
