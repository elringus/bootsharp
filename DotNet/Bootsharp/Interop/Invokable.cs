using static Bootsharp.InfoCache;
using static Bootsharp.Serializer;

namespace Bootsharp;

/// <summary>
/// Provides access to C# methods invokable from JavaScript via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Both arguments and return types of the methods are expected to be JSON-serializable.
/// Invokable endpoint is expected in the following format: <code>Assembly/Namespace/Class/Method</code>
/// </remarks>
public static partial class Invokable
{
    /// <summary>
    /// Invokes C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    /// <returns>JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string Invoke (string endpoint, string[]? args = null)
    {
        var (method, @params, _) = GetInvokableInfo(endpoint);
        if (method.Invoke(null, DeserializeArgs(@params, args)) is not { } result)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return any value.");
        return Serialize(result);
    }

    /// <summary>
    /// Invokes void C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static void InvokeVoid (string endpoint, string[]? args = null)
    {
        var (method, @params, _) = GetInvokableInfo(endpoint);
        method.Invoke(null, DeserializeArgs(@params, args));
    }

    /// <summary>
    /// Invokes asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    /// <returns>Task with JSON-serialized result of the method invocation.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static async Task<string> InvokeAsync (string endpoint, string[]? args = null)
    {
        var (method, @params, taskResult) = GetInvokableInfo(endpoint);
        if (method.Invoke(null, DeserializeArgs(@params, args)) is not Task task)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return task.");
        await task.ConfigureAwait(false);
        if (taskResult?.GetValue(task) is not { } result)
            throw new Error($"Failed to invoke '{endpoint}': missing task result.");
        return Serialize(result);
    }

    /// <summary>
    /// Invokes void asynchronous C# method with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the method to invoke.</param>
    /// <param name="args">JSON-serialized arguments for the method or null when invoking w/o arguments.</param>
    /// <returns>Task representing completion status of the method.</returns>
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static async Task InvokeVoidAsync (string endpoint, string[]? args = null)
    {
        var (method, @params, _) = GetInvokableInfo(endpoint);
        if (method.Invoke(null, DeserializeArgs(@params, args)) is not Task task)
            throw new Error($"Failed to invoke '{endpoint}': method didn't return task.");
        await task.ConfigureAwait(false);
    }
}
