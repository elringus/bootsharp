using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Bootsharp;

/// <summary>
/// Provides access to C# methods via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Both arguments and return types of the methods are expected to be JSON-serializable.
/// </remarks>
[SupportedOSPlatform("browser")]
public static partial class Method
{
    /// <summary>
    /// Invokes C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string Invoke (string endpoint, string[] args)
    {
        return "";
    }

    /// <summary>
    /// Invokes void C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string InvokeVoid (string endpoint, string[] args)
    {
        return "";
    }

    /// <summary>
    /// Invokes asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Task with JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static Task<string> InvokeAsync (string endpoint, string[] args)
    {
        return Task.FromResult("");
    }

    /// <summary>
    /// Invokes void asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Task representing completion status of the method.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static Task InvokeVoidAsync (string endpoint, string[] args)
    {
        return Task.CompletedTask;
    }
}
