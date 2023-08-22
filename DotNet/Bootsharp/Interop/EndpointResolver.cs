using System;

namespace Bootsharp;

/// <summary>
/// Resolves endpoint string to the data required for accessing associated member.
/// </summary>
public class EndpointResolver
{
    /// <summary>
    /// Resolves assembly, class and method names from specified endpoint string.
    /// </summary>
    public (string Assembly, string Class, string Method) ResolveMethod (string endpoint)
    {
        var parts = endpoint.Split(".", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            throw new FormatException($"Failed to resolve C# method endpoint '{endpoint}'. " +
                                      $"The endpoint is expected to contain assembly name, " +
                                      $"class name and method name joined with dots.");
        return (parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// Resolves namespace and function names from specified endpoint string.
    /// </summary>
    public (string Namespace, string Function) ResolveFunction (string endpoint)
    {
        var lastDotIndex = endpoint.LastIndexOf('.');
        if (lastDotIndex < 1 || endpoint.EndsWith('.'))
            throw new FormatException($"Failed to resolve JavaScript function endpoint '{endpoint}'. " +
                                      $"The endpoint is expected to contain namespace, " +
                                      $"and function name joined with dots.");
        return (endpoint[..(endpoint.Length - lastDotIndex - 1)], endpoint[(lastDotIndex + 1)..]);
    }
}
