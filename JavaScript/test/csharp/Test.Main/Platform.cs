using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test.Main;

public static class Platform
{
    [JSInvokable]
    public static string GetGuid () => Guid.NewGuid().ToString();

    [JSInvokable]
    public static string CatchException ()
    {
        try { JS.Invoke("throw"); }
        catch (JSException e) { return e.Message; }
        return null;
    }

    [JSInvokable]
    public static string Throw (string message) => throw new Exception(message);

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

    [JSInvokable]
    public static long ComputePrime (int n)
    {
        int count = 0;
        long a = 2;
        while (count < n)
        {
            long b = 2;
            int prime = 1;
            while (b * b <= a)
            {
                if (a % b == 0)
                {
                    prime = 0;
                    break;
                }
                b++;
            }
            if (prime > 0) count++;
            a++;
        }
        return --a;
    }
}
