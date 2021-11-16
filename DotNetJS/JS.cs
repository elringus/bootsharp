using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace DotNetJS
{
    /// <summary>
    /// Provides interop with JavaScript.
    /// </summary>
    public static class JS
    {
        private class JSRuntime : WebAssemblyJSRuntime { }

        private static readonly JSRuntime js = new();

        /// <summary>
        /// Invokes a global JavaScript function with the provided name and arguments.
        /// </summary>
        /// <param name="name">Name of the function to invoke.</param>
        /// <param name="args">Arguments to provide for the function.</param>
        public static void Invoke (string name, params object[] args) => js.InvokeVoid(name, args);

        /// <inheritdoc cref="Invoke"/>
        /// <typeparam name="T">Expected return type of the function.</typeparam>
        /// <returns>The result of the function invocation.</returns>
        public static T Invoke<T> (string name, params object[] args) => js.Invoke<T>(name, args);

        /// <inheritdoc cref="Invoke"/>
        public static ValueTask InvokeAsync (string name, params object[] args) => js.InvokeVoidAsync(name, args);

        /// <inheritdoc cref="Invoke{T}"/>
        public static ValueTask<T> InvokeAsync<T> (string name, params object[] args) => js.InvokeAsync<T>(name, args);
    }
}
