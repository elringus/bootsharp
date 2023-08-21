using System.Threading.Tasks;

namespace Bootsharp;

/// <summary>
/// Provides access to JavaScript functions via interop-specific endpoints.
/// </summary>
public static partial class Function
{
    /// <summary>
    /// Invokes JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the function.</param>
    /// <returns>JSON-serialized result of the function invocation or null when return type is void.</returns>
    [System.Runtime.InteropServices.JavaScript.JSImport("invoke", "Bootsharp")]
    public static partial string? Invoke (string endpoint, string[] args);

    /// <summary>
    /// Invokes asynchronous JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the function.</param>
    /// <returns>Task with JSON-serialized result of the function invocation; result is null when promise yields void.</returns>
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeAsync", "Bootsharp")]
    public static partial Task<string?> InvokeAsync (string endpoint, string[] args);
}
