using System.Threading.Tasks;

namespace Bootsharp;

/// <summary>
/// Provides access to JavaScript functions via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Both arguments and return types of the functions are expected to be JSON-serializable.
/// </remarks>
public static partial class Function
{
    /// <summary>
    /// Invokes JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the function.</param>
    /// <returns>JSON-serialized result of the function invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSImport("invoke", "Bootsharp")]
    public static partial string Invoke (string endpoint, string[] args);

    /// <summary>
    /// Invokes void JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the function.</param>
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeVoid", "Bootsharp")]
    public static partial string InvokeVoid (string endpoint, string[] args);

    /// <summary>
    /// Invokes asynchronous JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the function.</param>
    /// <returns>Task with JSON-serialized result of the function invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeAsync", "Bootsharp")]
    public static partial Task<string> InvokeAsync (string endpoint, string[] args);

    /// <summary>
    /// Invokes void asynchronous JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the function.</param>
    /// <returns>Task that completes when JavaScript promise is resolved.</returns>
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeVoidAsync", "Bootsharp")]
    public static partial Task InvokeVoidAsync (string endpoint, string[] args);
}
