using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class SolutionInspector
{
    private readonly List<InterfaceMeta> staticInterfaces = [];
    private readonly List<InterfaceMeta> instancedInterfaces = [];
    private readonly List<MethodMeta> staticMethods = [];
    private readonly List<string> warnings = [];
    private readonly TypeConverter converter;
    private readonly MethodInspector methodInspector;
    private readonly InterfaceInspector interfaceInspector;

    public SolutionInspector (Preferences prefs, string entryAssemblyName)
    {
        converter = new(prefs);
        methodInspector = new(prefs, converter);
        interfaceInspector = new(prefs, converter, entryAssemblyName);
    }

    /// <summary>
    /// Inspects specified solution assembly paths in the output directory.
    /// </summary>
    /// <param name="directory">The directory containing compiled assemblies.</param>
    /// <param name="paths">Full paths of the assemblies to inspect.</param>
    public SolutionInspection Inspect (string directory, IEnumerable<string> paths)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var assemblyPath in paths)
            try { InspectAssemblyFile(assemblyPath, ctx); }
            catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
        return CreateInspection(ctx);
    }

    private void InspectAssemblyFile (string assemblyPath, MetadataLoadContext ctx)
    {
        if (!ShouldIgnoreAssembly(assemblyPath))
            InspectAssembly(ctx.LoadFromAssemblyPath(assemblyPath));
    }

    private void AddSkippedAssemblyWarning (string assemblyPath, Exception exception)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{assemblyName}' assembly; " +
                      $"affected methods won't be available in JavaScript. Error: {exception.Message}";
        warnings.Add(message);
    }

    private SolutionInspection CreateInspection (MetadataLoadContext ctx) => new(ctx) {
        StaticInterfaces = [..staticInterfaces.DistinctBy(i => i.FullName)],
        InstancedInterfaces = [..instancedInterfaces.DistinctBy(i => i.FullName)],
        StaticMethods = [..staticMethods],
        Crawled = [..converter.CrawledTypes],
        Warnings = [..warnings]
    };

    private void InspectAssembly (Assembly assembly)
    {
        foreach (var exported in assembly.GetExportedTypes())
            InspectExportedType(exported);
        foreach (var attribute in assembly.CustomAttributes)
            InspectAssemblyAttribute(attribute);
    }

    private void InspectExportedType (Type type)
    {
        if (type.Namespace?.StartsWith("Bootsharp.Generated") ?? false) return;
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            InspectExportedStaticMethod(method);
    }

    private void InspectAssemblyAttribute (CustomAttributeData attribute)
    {
        var kind = default(InterfaceKind);
        var name = attribute.AttributeType.FullName;
        if (name == typeof(JSExportAttribute).FullName) kind = InterfaceKind.Export;
        else if (name == typeof(JSImportAttribute).FullName) kind = InterfaceKind.Import;
        else return;
        foreach (var arg in (IEnumerable<CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value!)
            InspectStaticInteropInterface((Type)arg.Value!, kind);
    }

    private void InspectExportedStaticMethod (MethodInfo info)
    {
        var kind = default(MethodKind?);
        foreach (var attr in info.CustomAttributes.Select(a => a.AttributeType.FullName))
            if (attr == typeof(JSInvokableAttribute).FullName) kind = MethodKind.Invokable;
            else if (attr == typeof(JSFunctionAttribute).FullName) kind = MethodKind.Function;
            else if (attr == typeof(JSEventAttribute).FullName) kind = MethodKind.Event;
        if (kind.HasValue) InspectStaticInteropMethod(info, kind.Value);
    }

    private void InspectStaticInteropMethod (MethodInfo info, MethodKind kind)
    {
        var methodMeta = methodInspector.Inspect(info, kind);
        staticMethods.Add(methodMeta);
        InspectMethodParameters(methodMeta, kind);
    }

    private void InspectStaticInteropInterface (Type type, InterfaceKind kind)
    {
        var interfaceMeta = interfaceInspector.Inspect(type, kind);
        staticInterfaces.Add(interfaceMeta);
        foreach (var method in interfaceMeta.Methods)
            InspectMethodParameters(method, kind);
    }

    private void InspectMethodParameters (MethodMeta meta, MethodKind kind)
    {
        var iKind = kind == MethodKind.Invokable ? InterfaceKind.Export : InterfaceKind.Import;
        InspectMethodParameters(meta, iKind);
    }

    private void InspectMethodParameters (MethodMeta meta, InterfaceKind kind)
    {
        // When interop instance is an argument of exported method, it's imported (JS) API and vice-versa.
        var argKind = kind == InterfaceKind.Export ? InterfaceKind.Import : InterfaceKind.Export;
        foreach (var arg in meta.Arguments)
            InspectMethodParameter(arg.Value.Type, argKind);
        if (!meta.ReturnValue.Void)
            InspectMethodParameter(meta.ReturnValue.Type, kind);
    }

    private void InspectMethodParameter (Type paramType, InterfaceKind kind)
    {
        if (!IsInstancedInteropInterface(paramType)) return;
        instancedInterfaces.Add(interfaceInspector.Inspect(paramType, kind));
    }
}
