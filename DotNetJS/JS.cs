using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace DotNetJS
{
    public static class JS
    {
        private class JSRuntime : WebAssemblyJSRuntime { }

        private static readonly JSRuntime js = new();

        public static T Invoke<T> (string identifier, params object[] args) => js.Invoke<T>(identifier, args);
        public static void Invoke (string identifier, params object[] args) => js.InvokeVoid(identifier, args);
        public static ValueTask<T> InvokeAsync<T> (string identifier, params object[] args) => js.InvokeAsync<T>(identifier, args);
        public static ValueTask InvokeAsync (string identifier, params object[] args) => js.InvokeVoidAsync(identifier, args);
    }
}
