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
    public static void StartStress ()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        _ = Stress(cts.Token);
    }

    [JSInvokable]
    public static void StopStress ()
    {
        cts?.Cancel();
    }

    [JSInvokable]
    public static bool IsStressing ()
    {
        return !cts?.IsCancellationRequested ?? false;
    }

    [JSFunction]
    public static partial int GetStressPower ();

    [JSEvent]
    public static partial void OnStressIteration (int time);

    private static async Task Stress (CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var time = DateTime.Now;
            ComputePrime(GetStressPower());
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
