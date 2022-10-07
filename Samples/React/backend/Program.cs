using Microsoft.JSInterop;

namespace Backend;

public static class Program
{
    public static void Main () { }

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
