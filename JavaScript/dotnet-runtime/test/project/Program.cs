﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test
{
    public static class Program
    {
        private static void Main () { }

        [Microsoft.JSInterop.JSInvokable]
        public static string JoinStrings (string a, string b) => a + b;

        [Microsoft.JSInterop.JSInvokable]
        public static double SumDoubles (double a, double b) => a + b;

        [Microsoft.JSInterop.JSInvokable]
        public static DateTime AddDays (DateTime date, int days) => date.AddDays(days);

        [Microsoft.JSInterop.JSInvokable]
        public static void InvokeJS (string funcName) => JS.Invoke(funcName);

        [Microsoft.JSInterop.JSInvokable]
        public static string[] ForEachJS (string[] items, string funcName)
        {
            for (int i = 0; i < items.Length; i++)
                items[i] = JS.Invoke<string>(funcName, items[i]);
            return items;
        }

        [Microsoft.JSInterop.JSInvokable]
        public static async Task<string> JoinStringsAsync (string a, string b)
        {
            await Task.Yield();
            return a + b;
        }

        [Microsoft.JSInterop.JSInvokable("EchoAlias")]
        public static string Echo (string message) => message;

        [Microsoft.JSInterop.JSInvokable]
        public static string ReceiveBytes (byte[] bytes) => Encoding.UTF8.GetString(bytes);

        [Microsoft.JSInterop.JSInvokable]
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

        [Microsoft.JSInterop.JSInvokable]
        public static async Task StreamFromJSAsync (IJSStreamReference streamRef)
        {
            await using var stream = await streamRef.OpenReadStreamAsync();
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var buffer = memoryStream.ToArray();
            for (var i = 0; i < buffer.Length; i++)
                if (buffer[i] != i % 256)
                    throw new Exception($"Failure at index {i}.");
            if (buffer.Length != 100_000)
                throw new Exception($"Got a stream of length {buffer.Length}, expected a length of 100,000.");
        }

        [Microsoft.JSInterop.JSInvokable]
        public static DotNetStreamReference StreamFromDotNet ()
        {
            var data = new byte[100000];
            for (var i = 0; i < data.Length; i++)
                data[i] = (byte)(i % 256);
            return new DotNetStreamReference(new MemoryStream(data));
        }

        [Microsoft.JSInterop.JSInvokable]
        public static string CatchException ()
        {
            try { JS.Invoke("throw"); }
            catch (JSException e) { return e.Message; }
            return null;
        }

        [Microsoft.JSInterop.JSInvokable]
        public static string Throw (string message) => throw new Exception(message);

        [Microsoft.JSInterop.JSInvokable]
        public static IJSObjectReference GetAndReturnJSObject ()
        {
            return JS.Invoke<IJSObjectReference>("getObject");
        }

        [Microsoft.JSInterop.JSInvokable]
        public static async Task InvokeOnJSObjectAsync (IJSObjectReference obj, string function, params object[] args)
        {
            await obj.InvokeVoidAsync(function, args);
        }

        [Microsoft.JSInterop.JSInvokable]
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

        [Microsoft.JSInterop.JSInvokable]
        public static void InvokeVoid () { }

        [Microsoft.JSInterop.JSInvokable]
        public static string GetGuid () => Guid.NewGuid().ToString();
    }
}