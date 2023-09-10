namespace Bootsharp;

/// <summary>
/// Provides access to JavaScript function delegates via endpoints.
/// </summary>
/// <remarks>
/// Endpoint is composed of JavaScript object and function names joined with dot.
/// In case host object is composed, object chain is joined with dots as well.
/// </remarks>
public static class Function
{
    public record Info(string Baz);

    private static readonly Dictionary<string, Delegate> functions = new();

    /// <summary>
    /// Maps function delegate to specified endpoint.
    /// </summary>
    /// <remarks>
    /// Performed at 'PrepareBootsharp' build step (not in source generator).
    /// </remarks>
    public static void Set (string endpoint, Delegate func)
    {
        functions[endpoint] = func;
    }

    /// <summary>
    /// Returns function delegate of specified endpoint and type.
    /// </summary>
    /// <remarks>
    /// Used in sources generated for partial <see cref="JSFunctionAttribute"/>
    /// and <see cref="JSEventAttribute"/> methods.
    /// </remarks>
    public static T Get<T> (string endpoint) where T : Delegate
    {
        if (!functions.TryGetValue(endpoint, out var func))
            throw new Error($"Endpoint '{endpoint}' is not found.");
        if (func is not T specific)
            throw new Error($"Endpoint '{endpoint}' is not '{typeof(T)}'.");
        return specific;
    }
}
