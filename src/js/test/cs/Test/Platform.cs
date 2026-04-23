using System;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bootsharp;

namespace Test;

public static partial class Platform
{
    [Export]
    public static string GetGuid () => Guid.NewGuid().ToString();

    [Export]
    public static string FormatDate (string culture, int month, int day, string format)
    {
        var info = new CultureInfo(culture, false);
        return new DateTime(2024, month, day).ToString(format, info);
    }

    [Export]
    public static string? CatchException ()
    {
        try { ThrowJS(); }
        catch (Exception e) { return e.Message; }
        return null;
    }

    [Export]
    public static void ThrowCS (string message) => throw new Exception(message);

    [Import]
    public static partial void ThrowJS ();

    [Export]
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
