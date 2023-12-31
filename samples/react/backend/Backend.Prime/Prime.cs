using System.Diagnostics;

namespace Backend.Prime;

// Implementation of the computer service that compute prime numbers.
// Injected in the application entry point assembly (Backend.WASM).

public class Prime(IPrimeUI ui) : IComputer
{
    private static readonly SemaphoreSlim semaphore = new(0);
    private readonly Stopwatch watch = new();
    private CancellationTokenSource? cts;

    public void StartComputing ()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        cts.Token.Register(() => ui.NotifyComputing(false));
        var options = ui.GetOptions();
        if (!options.Multithreading) ComputeLoop(options.Complexity, cts.Token);
        else new Thread(() => ComputeLoop(options.Complexity, cts.Token)).Start();
        ObserveLoop(cts.Token);
        ui.NotifyComputing(true);
    }

    public void StopComputing () => cts?.Cancel();

    public bool IsComputing () => !cts?.IsCancellationRequested ?? false;

    private async void ObserveLoop (CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            watch.Restart();
            try { await semaphore.WaitAsync(token); }
            catch (OperationCanceledException) { }
            finally
            {
                watch.Stop();
                ui.NotifyComplete(watch.ElapsedMilliseconds);
            }
            await Task.Delay(1);
        }
    }

    private static async void ComputeLoop (int complexity, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            ComputePrime(complexity);
            semaphore.Release();
            await Task.Delay(10);
        }
    }

    private static void ComputePrime (int n)
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
