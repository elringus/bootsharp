namespace Bootsharp;

/// <summary>
/// Builds endpoint string from the data required for accessing associated member.
/// </summary>
public class EndpointBuilder
{
    /// <summary>
    /// Builds method endpoint string from specified assembly, class and method names.
    /// </summary>
    /// <param name="assembly">Simple (short) name of the assembly containing class.</param>
    /// <param name="class">Class name containing method.</param>
    /// <param name="method">Method name to invoke.</param>
    public string BuildMethod (string assembly, string @class, string method)
    {
        return $"{assembly}.{@class}.{method}";
    }

    /// <summary>
    /// Builds function endpoint string from namespace and function names.
    /// </summary>
    /// <param name="namespace">Namespace containing function (nested spaces joined with dots).</param>
    /// <param name="method">Function name to invoke.</param>
    public string BuildFunction (string @namespace, string method)
    {
        return $"{@namespace}.{method}";
    }
}
