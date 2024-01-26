using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspector
{
    private readonly List<InterfaceMeta> interfaces = [];
    private readonly List<MethodMeta> methods = [];
    private readonly List<string> warnings = [];
    private readonly TypeConverter converter;
    private readonly MethodInspector methodInspector;
    private readonly InterfaceInspector interfaceInspector;

    public AssemblyInspector (Preferences prefs, string entryAssemblyName)
    {
        converter = new(prefs);
        methodInspector = new(prefs, converter);
        interfaceInspector = new(prefs, converter, entryAssemblyName);
    }

    public AssemblyInspection InspectInDirectory (string directory, IEnumerable<string> paths)
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

    private AssemblyInspection CreateInspection (MetadataLoadContext ctx) => new(ctx) {
        Interfaces = [..interfaces.DistinctBy(i => i.FullName)],
        Methods = [..methods],
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
        methods.Add(methodMeta);
        InspectMethodParameters(methodMeta, kind);
    }

    private void InspectStaticInteropInterface (Type type, InterfaceKind kind)
    {
        var interfaceMetas = interfaceInspector.Inspect(type, kind, false);
        interfaces.AddRange(interfaceMetas);
        methods.AddRange(interfaceMetas.SelectMany(i => i.Methods.Select(m => m.Meta)));
        foreach (var meta in interfaceMetas)
        foreach (var method in meta.Methods)
            InspectMethodParameters(method.Meta, kind);
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
        if (!IsInstancedInterface(paramType)) return;
        interfaces.AddRange(interfaceInspector.Inspect(paramType, kind, true));
    }

    private bool IsInstancedInterface (Type type)
    {
        if (!type.IsInterface) return false;
        if (string.IsNullOrEmpty(type.Namespace)) return true;
        return !type.Namespace.StartsWith("System.", StringComparison.Ordinal);
    }
}
