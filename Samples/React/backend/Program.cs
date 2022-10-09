using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Backend;

public static partial class Program
{
    public static void Main () { }

    [JSFunction] public static partial void Log (string message);
    [JSFunction] public static partial Task<int> GetPrime ();
    [JSEvent] public static partial void OnWarn (string message);

    [JSInvokable]
    public static async Task ComputePrime ()
    {
        var n = await GetPrime();
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
        Log(a.ToString());
        OnWarn(n.ToString());
    }
}
