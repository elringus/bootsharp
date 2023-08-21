using System.Threading.Tasks;

namespace Bootsharp;

/// <summary>
/// Provides access to C# methods via interop-specific endpoints.
/// </summary>
public static class Method
{
    /// <summary>
    /// Invokes C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Result of the method invocation or null when return type is void.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string? Invoke (string endpoint, string[] args)
    {
        return "";
    }

    /// <summary>
    /// Invokes asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Task with JSON-serialized result of the method invocation; result is null when method yields void.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static Task<string?> InvokeAsync (string endpoint, string[] args)
    {
        return Task.FromResult("");
    }
}
