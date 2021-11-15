using System;
using DotNetJS;
using Microsoft.JSInterop;

// Namespace is used as the name for both the generated .js file
// and main export object of the UMD library.
namespace HelloBrowser
{
    public static class Program
    {
        // Main is invoked by the JavaScript runtime on boot.
        public static void Main ()
        {
            // Invoking 'getName()' function from JavaScript.
            var browserName = JS.Invoke<string>("getName");
            // Writing to browser console.
            Console.WriteLine($"Hello {browserName}, DotNet here!");
        }

        [JSInvokable] // The method is invoked from JavaScript.
        public static string GetName () => Environment.MachineName;
    }
}
