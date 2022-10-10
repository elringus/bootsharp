using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Backend;

public static partial class Program
{
    private static CancellationTokenSource? cts;

    public static void Main () { }

    [JSInvokable]
    public static Task StartStress ()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        _ = Stress(cts.Token);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public static Task StopStress ()
    {
        cts?.Cancel();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public static Task<bool> IsStressing ()
    {
        return Task.FromResult(!cts?.IsCancellationRequested ?? false);
    }

    [JSFunction]
    public static partial Task<int> GetStressPower ();

    [JSEvent]
    public static partial void OnStressIteration (int time);

    private static async Task Stress (CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var time = DateTime.Now;
            ComputePrime(await GetStressPower());
            OnStressIteration((DateTime.Now - time).Milliseconds);
            await Task.Delay(1);
        }
    }

    private static void ComputePrime (int n)
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
    }
}
