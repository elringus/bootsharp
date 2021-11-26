using System;
using System.Text;
using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test
{
    public static class Invokable
    {
        [JSInvokable]
        public static void InvokeVoid () { }

        [JSInvokable("EchoAlias")]
        public static string Echo (string message) => message;

        [JSInvokable]
        public static string JoinStrings (string a, string b) => a + b;

        [JSInvokable]
        public static double SumDoubles (double a, double b) => a + b;

        [JSInvokable]
        public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);

        [JSInvokable]
        public static void InvokeJS (string funcName) => JS.Invoke(funcName);

        [JSInvokable]
        public static string[] ForEachJS (string[] items, string funcName)
        {
            for (int i = 0; i < items.Length; i++)
                items[i] = JS.Invoke<string>(funcName, items[i]);
            return items;
        }

        [JSInvokable]
        public static async Task<string> JoinStringsAsync (string a, string b)
        {
            await Task.Yield();
            return a + b;
        }

        [JSInvokable]
        public static string ReceiveBytes (byte[] bytes) => Encoding.UTF8.GetString(bytes);

        [JSInvokable]
        public static string SendBytes ()
        {
            var bytes = new byte[] {
                0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
                0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
                0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
                0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e
            };
            return JS.Invoke<string>("receiveBytes", bytes);
        }
    }
}
