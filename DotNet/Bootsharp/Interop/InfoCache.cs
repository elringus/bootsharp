using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Bootsharp;

internal static class InfoCache
{
    public record InvokableInfo(MethodInfo Method, IReadOnlyList<ParameterInfo> Args, PropertyInfo? TaskResult);

    private static readonly Dictionary<string, InvokableInfo> endpointToInvokable = new();

    public static InvokableInfo GetInvokableInfo (string endpoint) =>
        endpointToInvokable.TryGetValue(endpoint, out var method)
            ? method : endpointToInvokable[endpoint] = Reflect(endpoint);

    private static InvokableInfo Reflect (string endpoint)
    {
        var (assemblyName, className, methodName) = Endpoint.Resolve(endpoint);
        var assembly = TryLoadAssembly(assemblyName);
        var @class = assembly.GetType(className)
                     ?? throw new Error($"Failed to reflect '{endpoint}': '{className}' class not found.");
        var method = @class.GetMethod(methodName)
                     ?? throw new Error($"Failed to reflect '{endpoint}': '{methodName}' method not found.");
        var taskResult = typeof(Task).IsAssignableFrom(method.ReturnType)
                         && method.ReturnType.IsGenericType ? method.ReturnType.GetProperty("Result") : null;
        return new(method, method.GetParameters(), taskResult);

        Assembly TryLoadAssembly (string name)
        {
            try { return Assembly.Load(name); }
            catch (FileNotFoundException) { throw new Error($"Failed to reflect '{endpoint}': '{name}' assembly not found."); }
        }
    }
}
