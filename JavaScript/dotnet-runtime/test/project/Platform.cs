using System;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test
{
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
}
