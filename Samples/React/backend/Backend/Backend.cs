using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backend;

public class Backend : IBackend
{
    private readonly IFrontend frontend;

    private CancellationTokenSource? cts;

    public Backend (IFrontend frontend)
    {
        this.frontend = frontend;
    }

    public void StartStress ()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        _ = Stress(cts.Token);
    }

    public void StopStress ()
    {
        cts?.Cancel();
    }

    public bool IsStressing ()
    {
        return !cts?.IsCancellationRequested ?? false;
    }

    private async Task Stress (CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var time = DateTime.Now;
            ComputePrime(frontend.GetStressPower());
            frontend.OnStressComplete((DateTime.Now - time).Milliseconds);
            await Task.Delay(1);
        }
    }

    private void ComputePrime (int n)
    {
        var count = 0;
        var a = (long)2;
        while (count < n)
        {
            var b = (long)2;
            var prime = 1;
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
