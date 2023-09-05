using static Bootsharp.Serializer;

namespace Bootsharp;

/// <summary>
/// Provides access to JavaScript functions via interop-specific endpoints.
/// </summary>
/// <remarks>
/// Both arguments and return types of the functions are expected to be JSON-serializable.
/// Function endpoint is expected in the following format: <code>namespace/function</code>
/// — where 'namespace' is the object chain leading to the function, joined with dots.
/// </remarks>
public static partial class Function
{
    /// <summary>
    /// Invokes JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">Arguments for the function.</param>
    /// <returns>Result of the function invocation.</returns>
    public static TResult Invoke<TResult> (string endpoint, params object[] args) =>
        (TResult)Deserialize(InvokeSerialized(endpoint, SerializeArgs(args)), typeof(TResult));

    /// <summary>
    /// Invokes void JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">Arguments for the function.</param>
    public static void InvokeVoid (string endpoint, params object[] args) =>
        InvokeVoidSerialized(endpoint, SerializeArgs(args));

    /// <summary>
    /// Invokes asynchronous JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">Arguments for the function.</param>
    /// <returns>Task with result of the function invocation.</returns>
    public static async Task<TResult> InvokeAsync<TResult> (string endpoint, params object[] args) =>
        (TResult)Deserialize(await InvokeSerializedAsync(endpoint, SerializeArgs(args)), typeof(TResult));

    /// <summary>
    /// Invokes void asynchronous JavaScript function with specified endpoint and arguments.
    /// </summary>
    /// <param name="endpoint">Address of the function to invoke.</param>
    /// <param name="args">Arguments for the function.</param>
    /// <returns>Task that completes when JavaScript promise is resolved.</returns>
    public static Task InvokeVoidAsync (string endpoint, params object[] args) =>
        InvokeVoidSerializedAsync(endpoint, SerializeArgs(args));

    [System.Runtime.InteropServices.JavaScript.JSImport("invoke", "Bootsharp")]
    private static partial string InvokeSerialized (string endpoint, string[]? args);
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeVoid", "Bootsharp")]
    private static partial void InvokeVoidSerialized (string endpoint, string[]? args);
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeAsync", "Bootsharp")]
    private static partial Task<string> InvokeSerializedAsync (string endpoint, string[]? args);
    [System.Runtime.InteropServices.JavaScript.JSImport("invokeVoidAsync", "Bootsharp")]
    private static partial Task InvokeVoidSerializedAsync (string endpoint, string[]? args);
}
