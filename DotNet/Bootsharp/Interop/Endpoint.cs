namespace Bootsharp;

/// <summary>
/// Allows building and resolving endpoints for accessing C# methods from JavaScript.
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Builds C# method endpoint string from specified assembly, namespace, class and method names.
    /// </summary>
    /// <remarks>Namespace of the method is expected to equal assembly name.</remarks>
    /// <param name="assembly">Simple (short) name of the assembly containing namespace, class and method to invoke.</param>
    /// <param name="class">Full class name (including namespace, if any) containing method to invoke.</param>
    /// <param name="method">Method name to invoke.</param>
    public static string Build (string assembly, string @class, string method) => $"{assembly}/{@class}/{method}";

    /// <summary>
    /// Resolves C# assembly name, full class name (including namespace, if any) and method name from specified endpoint string.
    /// </summary>
    public static (string assembly, string @class, string method) Resolve (string endpoint)
    {
        var parts = endpoint.Split('/');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
            throw new Error($"Failed to resolve C# method endpoint '{endpoint}'. " +
                            $"The endpoint is expected to contain assembly name, " +
                            $"full class name and method name joined with forward slashes.");
        return (parts[0], parts[1], parts[2]);
    }
}
