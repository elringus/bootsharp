namespace Bootsharp;

/// <summary>
/// Builds endpoint string from the data required for accessing associated member.
/// </summary>
public sealed class EndpointBuilder
{
    /// <summary>
    /// Builds method endpoint string from specified assembly, class and method names.
    /// </summary>
    /// <remarks>Namespace of the method is expected to equal assembly name.</remarks>
    /// <param name="assembly">Simple (short) name of the assembly containing class (nested parts joined with dots).</param>
    /// <param name="class">Class name containing method.</param>
    /// <param name="method">Method name to invoke.</param>
    public string BuildMethod (string assembly, string @class, string method)
    {
        return $"{assembly}/{@class}/{method}";
    }

    /// <summary>
    /// Builds function endpoint string from namespace and function names.
    /// </summary>
    /// <param name="namespace">Namespace containing function (nested parts joined with dots).</param>
    /// <param name="method">Function name to invoke.</param>
    public string BuildFunction (string @namespace, string method)
    {
        return $"{@namespace}/{method}";
    }
}
