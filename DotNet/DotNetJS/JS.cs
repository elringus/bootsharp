using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace DotNetJS
{
    /// <summary>
    /// Provides interop with JavaScript.
    /// </summary>
    public static class JS
    {
        private static IJSInProcessRuntime js = new JSRuntime();

        /// <summary>
        /// Allows providing a custom JavaScript runtime to be used for the interop.
        /// </summary>
        public static void UseCustomRuntime (IJSInProcessRuntime runtime)
        {
            js = runtime;
        }

        /// <summary>
        /// Invokes a global JavaScript function with the provided name and arguments.
        /// </summary>
        /// <param name="name">Name of the function to invoke.</param>
        /// <param name="args">JSON-serializable arguments for the function.</param>
        public static void Invoke (string name, params object[] args) => js.InvokeVoid(name, args);

        /// <summary>
        /// Invokes a global JavaScript function with the provided name and arguments.
        /// </summary>
        /// <param name="name">Name of the function to invoke.</param>
        /// <param name="args">JSON-serializable arguments for the function.</param>
        /// <typeparam name="T">Expected return type of the function.</typeparam>
        /// <returns>The result of the function invocation.</returns>
        public static T Invoke<T> (string name, params object[] args) => js.Invoke<T>(name, args);

        /// <summary>
        /// Invokes a global asynchronous JavaScript function with the provided name and arguments.
        /// </summary>
        /// <param name="name">Name of the function to invoke.</param>
        /// <param name="args">JSON-serializable arguments for the function.</param>
        /// <returns>A task that resolves when the asynchronous function returns.</returns>
        public static ValueTask InvokeAsync (string name, params object[] args) => js.InvokeVoidAsync(name, args);

        /// <summary>
        /// Invokes a global asynchronous JavaScript function with the provided name and arguments.
        /// </summary>
        /// <param name="name">Name of the function to invoke.</param>
        /// <param name="args">JSON-serializable arguments for the function.</param>
        /// <typeparam name="T">Expected return type of the function.</typeparam>
        /// <returns>A task with the result that resolves when the asynchronous function returns.</returns>
        public static ValueTask<T> InvokeAsync<T> (string name, params object[] args) => js.InvokeAsync<T>(name, args);
    }
}
