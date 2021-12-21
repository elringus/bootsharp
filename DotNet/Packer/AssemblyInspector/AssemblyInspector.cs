using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using static Packer.Utilities;

namespace Packer;

internal class AssemblyInspector
{
    public IReadOnlyCollection<Assembly> Assemblies => assemblies;
    public IReadOnlyCollection<Method> InvokableMethods => invokableMethods;
    public IReadOnlyCollection<Method> FunctionMethods => functionMethods;

    private readonly List<Assembly> assemblies = new();
    private readonly List<Method> invokableMethods = new();
    private readonly List<Method> functionMethods = new();
    private readonly List<string> warnings = new();

    public void InspectInDirectory (string directory)
    {
        var assemblyPaths = Directory.GetFiles(directory, "*.dll");
        using var context = CreateLoadContext(assemblyPaths);
        foreach (var assemblyPath in assemblyPaths)
            if (ShouldInspectAssembly(assemblyPath))
                try { InspectAssembly(assemblyPath, context); }
                catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
    }

    public void Report (TaskLoggingHelper logger)
    {
        logger.LogMessage(MessageImportance.Normal, "DotNetJS assembly inspection result:");
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {Assemblies.Count} assemblies:",
            JoinLines(Assemblies.Select(a => a.Name))));
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {InvokableMethods.Count} JS invokable methods:",
            JoinLines(InvokableMethods.Select(m => m.ToString()))));
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {FunctionMethods.Count} JS function methods:",
            JoinLines(FunctionMethods.Select(m => m.ToString()))));

        foreach (var warning in warnings)
            logger.LogWarning(warning);
    }

    private MetadataLoadContext CreateLoadContext (IEnumerable<string> assemblyPaths)
    {
        var resolver = new PathAssemblyResolver(assemblyPaths);
        return new MetadataLoadContext(resolver);
    }

    private void InspectAssembly (string assemblyPath, MetadataLoadContext context)
    {
        assemblies.Add(new Assembly {
            Name = Path.GetFileName(assemblyPath),
            Base64 = ReadBase64(assemblyPath)
        });
        InspectMethods(context.LoadFromAssemblyPath(assemblyPath));
    }

    private void AddSkippedAssemblyWarning (string assemblyPath, Exception exception)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{assemblyName}' assembly; " +
                      $"affected methods won't be available in JavaScript. Error: {exception.Message}";
        warnings.Add(message);
    }

    private bool ShouldInspectAssembly (string assemblyPath)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        if (assemblyName.StartsWith("System.")) return false;
        if (assemblyName.StartsWith("Microsoft.")) return false;
        return true;
    }

    private void InspectMethods (System.Reflection.Assembly assembly)
    {
        foreach (var method in GetStaticMethods(assembly))
        foreach (var attribute in method.CustomAttributes)
            if (attribute.AttributeType.Name == Attributes.Invokable)
                invokableMethods.Add(new Method(method));
            else if (attribute.AttributeType.Name == Attributes.Function)
                functionMethods.Add(new Method(method));
    }

    private static string ReadBase64 (string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return Convert.ToBase64String(bytes);
    }

    private static IEnumerable<MethodInfo> GetStaticMethods (System.Reflection.Assembly assembly)
    {
        var exported = assembly.GetExportedTypes();
        return exported.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static));
    }
}
