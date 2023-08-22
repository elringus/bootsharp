using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bootsharp;

internal class MethodCache
{
    private readonly Dictionary<string, (MethodInfo, IReadOnlyList<ParameterInfo>)> endpointToInfo = new();
    private readonly EndpointResolver resolver = new();

    public (MethodInfo, IReadOnlyList<ParameterInfo>) Get (string endpoint)
    {
        return endpointToInfo.TryGetValue(endpoint, out var method) ? method :
            endpointToInfo[endpoint] = Reflect(endpoint);
    }

    private (MethodInfo, IReadOnlyList<ParameterInfo>) Reflect (string endpoint)
    {
        var (assemblyName, className, methodName) = resolver.ResolveMethod(endpoint);
        var assembly = Assembly.Load(assemblyName) ?? throw new Exception($"Failed to load '{assemblyName}' assembly.");
        var @class = assembly.GetType(className) ?? throw new Exception($"Failed to reflect '{assemblyName}.{className}' class.");
        var method = @class.GetMethod(methodName) ?? throw new Exception($"Failed to reflect '{assemblyName}.{className}.{methodName}' method.");
        return (method, method.GetParameters());
    }
}
