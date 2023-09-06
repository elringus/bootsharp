namespace Backend.Prime;

// One possible implementation of the prime computer service.
// Injected in the application entry point assembly (Backend.WASM).

public class PrimeComputer(IPrimeFrontend frontend) : IPrimeBackend
{
    private CancellationTokenSource? cts;

    public void StartComputing ()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        _ = RunAsync(cts.Token);
    }

    public void StopComputing () => cts?.Cancel();

    public bool IsComputing () => !cts?.IsCancellationRequested ?? false;

    private async Task RunAsync (CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var time = DateTime.Now;
            ComputePrime(frontend.GetComplexity());
            frontend.NotifyComplete((DateTime.Now - time).Milliseconds);
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
