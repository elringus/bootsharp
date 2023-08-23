using System.Linq;

namespace Bootsharp;

/// <summary>
/// Resolves endpoint string to the data required for accessing associated member.
/// </summary>
public static class EndpointResolver
{
    /// <summary>
    /// Resolves assembly, class and method names from specified endpoint string.
    /// </summary>
    /// <remarks>Namespace of the method is expected to equal assembly name.</remarks>
    public static (string Assembly, string Class, string Method) ResolveMethod (string endpoint)
    {
        var parts = endpoint.Split('/');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
            throw new Error($"Failed to resolve C# method endpoint '{endpoint}'. " +
                            $"The endpoint is expected to contain assembly name, " +
                            $"class name and method name joined with forward slashes.");
        return (parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// Resolves namespace and function names from specified endpoint string.
    /// </summary>
    public static (string Namespace, string Function) ResolveFunction (string endpoint)
    {
        var parts = endpoint.Split('/');
        if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
            throw new Error($"Failed to resolve JavaScript function endpoint '{endpoint}'. " +
                            $"The endpoint is expected to contain namespace, " +
                            $"and function name joined with forward slashes.");
        return (parts[0], parts[1]);
    }
}
