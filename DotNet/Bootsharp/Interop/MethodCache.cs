using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Bootsharp;

internal static class MethodCache
{
    private static readonly Dictionary<string, (MethodInfo, IReadOnlyList<ParameterInfo>, PropertyInfo?)> endpointToInfo = new();

    public static (MethodInfo Method, IReadOnlyList<ParameterInfo> Args, PropertyInfo? TaskResult) Get (string endpoint)
    {
        return endpointToInfo.TryGetValue(endpoint, out var method) ? method :
            endpointToInfo[endpoint] = Reflect(endpoint);
    }

    private static (MethodInfo, IReadOnlyList<ParameterInfo>, PropertyInfo?) Reflect (string endpoint)
    {
        var (assemblyName, className, methodName) = EndpointResolver.ResolveMethod(endpoint);
        var assembly = Assembly.Load(assemblyName);
        var @class = assembly.GetType($"{assemblyName}.{className}") ?? throw new Error($"Failed to reflect '{endpoint}': '{className}' class not found.");
        var method = @class.GetMethod(methodName) ?? throw new Error($"Failed to reflect '{endpoint}': '{methodName}' method not found.");
        var taskResult = typeof(Task).IsAssignableFrom(method.ReturnType) && method.ReturnType.IsGenericType ? method.ReturnType.GetProperty("Result") : null;
        return (method, method.GetParameters(), taskResult);
    }
}
