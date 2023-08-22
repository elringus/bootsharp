using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Bootsharp;

/// <summary>
/// Provides access to C# methods via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Namespace of the methods is expected to equal assembly name.
/// Both arguments and return types of the methods are expected to be JSON-serializable.
/// </remarks>
[SupportedOSPlatform("browser")]
public static partial class Method
{
    private static readonly MethodCache cache = new();
    private static readonly Serializer serializer = new();

    /// <summary>
    /// Invokes C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string Invoke (string endpoint, string[] args)
    {
        var (method, @params, _) = cache.Get(endpoint);
        if (method.Invoke(null, serializer.DeserializeArgs(args, @params)) is not { } result)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return any value.");
        return serializer.Serialize(result);
    }

    /// <summary>
    /// Invokes void C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static void InvokeVoid (string endpoint, string[] args)
    {
        var (method, @params, _) = cache.Get(endpoint);
        method.Invoke(null, serializer.DeserializeArgs(args, @params));
    }

    /// <summary>
    /// Invokes asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Task with JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static async Task<string> InvokeAsync (string endpoint, string[] args)
    {
        var (method, @params, taskResult) = cache.Get(endpoint);
        if (method.Invoke(null, serializer.DeserializeArgs(args, @params)) is not Task task)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return task.");
        await task.ConfigureAwait(false);
        if (taskResult?.GetValue(task) is not { } result)
            throw new Error($"Failed to invoke '{endpoint}': missing task result.");
        return serializer.Serialize(result);
    }

    /// <summary>
    /// Invokes void asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method.</param>
    /// <returns>Task representing completion status of the method.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static async Task InvokeVoidAsync (string endpoint, string[] args)
    {
        var (method, @params, _) = cache.Get(endpoint);
        if (method.Invoke(null, serializer.DeserializeArgs(args, @params)) is not Task task)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return task.");
        await task.ConfigureAwait(false);
    }
}
