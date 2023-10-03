using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Builder;

internal sealed class AssemblyInspector(NamespaceBuilder spaceBuilder) : IDisposable
{
    public List<Assembly> Assemblies { get; } = new();
    public List<Method> Methods { get; } = new();
    public List<Type> Types { get; } = new();

    private const string invokableAttributeName = "JSInvokableAttribute";
    private const string functionAttributeName = "JSFunctionAttribute";
    private const string eventAttributeName = "JSEventAttribute";

    private readonly List<string> warnings = new();
    private readonly List<MetadataLoadContext> contexts = new();
    private readonly TypeConverter typeConverter = new(spaceBuilder);

    public void InspectInDirectory (string directory)
    {
        var context = CreateLoadContext(directory);
        foreach (var assemblyPath in Directory.GetFiles(directory, "*.dll"))
            try { InspectAssembly(assemblyPath, context); }
            catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
        Types.AddRange(typeConverter.CrawledTypes);
        contexts.Add(context);
    }

    public void Report (TaskLoggingHelper logger)
    {
        logger.LogMessage(MessageImportance.Normal, "Bootsharp assembly inspection result:");
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {Assemblies.Count} assemblies:",
            JoinLines(Assemblies.Select(a => a.Name))));
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {Methods.Count} JS methods:",
            JoinLines(Methods.Select(m => m.ToString()))));

        foreach (var warning in warnings)
            logger.LogWarning(warning);
    }

    public void Dispose ()
    {
        foreach (var context in contexts)
            context.Dispose();
        contexts.Clear();
    }

    private void InspectAssembly (string assemblyPath, MetadataLoadContext context)
    {
        Assemblies.Add(CreateAssembly(assemblyPath));
        if (!ShouldIgnoreAssembly(assemblyPath))
            InspectMethods(context.LoadFromAssemblyPath(assemblyPath));
    }

    private void AddSkippedAssemblyWarning (string assemblyPath, Exception exception)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{assemblyName}' assembly; " +
                      $"affected methods won't be available in JavaScript. Error: {exception.Message}";
        warnings.Add(message);
    }

    private Assembly CreateAssembly (string assemblyPath)
    {
        var name = Path.GetFileName(assemblyPath);
        var bytes = File.ReadAllBytes(assemblyPath);
        return new(name, bytes);
    }

    private void InspectMethods (System.Reflection.Assembly assembly)
    {
        foreach (var method in GetStaticMethods(assembly))
        foreach (var attribute in method.CustomAttributes)
            InspectMethodWithAttribute(method, attribute.AttributeType.Name);
    }

    private void InspectMethodWithAttribute (MethodInfo method, string attributeName)
    {
        if (attributeName == invokableAttributeName)
            Methods.Add(CreateMethod(method, MethodType.Invokable));
        else if (attributeName == functionAttributeName)
            Methods.Add(CreateMethod(method, MethodType.Function));
        else if (attributeName == eventAttributeName)
            Methods.Add(CreateMethod(method, MethodType.Event));
    }

    private Method CreateMethod (MethodInfo info, MethodType type) => new() {
        Type = type,
        Assembly = info.DeclaringType!.Assembly.GetName().Name!,
        Namespace = info.DeclaringType.Namespace,
        DeclaringName = info.DeclaringType.FullName!,
        Name = info.Name,
        Arguments = info.GetParameters().Select(CreateArgument).ToArray(),
        ReturnType = info.ReturnType,
        ReturnTypeSyntax = BuildSyntax(info.ReturnType, info.ReturnParameter),
        ReturnsVoid = IsVoid(info.ReturnType),
        ReturnsNullable = IsNullable(info),
        ReturnsTaskLike = IsTaskLike(info.ReturnType),
        ShouldSerializeReturnType = ShouldSerialize(info.ReturnType),
        JSSpace = spaceBuilder.Build(info.DeclaringType),
        JSArguments = info.GetParameters().Select(CreateJSArgument).ToArray(),
        JSReturnTypeSyntax = typeConverter.ToTypeScript(info.ReturnType, GetNullability(info.ReturnParameter))
    };

    private Argument CreateArgument (ParameterInfo info) => new() {
        Name = info.Name!,
        Type = info.ParameterType,
        TypeSyntax = BuildSyntax(info.ParameterType, info),
        Nullable = IsNullable(info),
        ShouldSerialize = ShouldSerialize(info.ParameterType)
    };

    private Argument CreateJSArgument (ParameterInfo info) => new() {
        Name = info.Name == "function" ? "fn" : info.Name!,
        Type = info.ParameterType,
        TypeSyntax = typeConverter.ToTypeScript(info.ParameterType, GetNullability(info)),
        Nullable = IsNullable(info),
        ShouldSerialize = ShouldSerialize(info.ParameterType)
    };

    private static IEnumerable<MethodInfo> GetStaticMethods (System.Reflection.Assembly assembly)
    {
        var exported = assembly.GetExportedTypes();
        return exported.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static));
    }
}
