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
        Interfaces = [..interfaces],
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
        foreach (var attr in method.CustomAttributes)
            if (attr.AttributeType.FullName == typeof(JSInvokableAttribute).FullName)
                methods.Add(methodInspector.Inspect(method, MethodKind.Invokable));
            else if (attr.AttributeType.FullName == typeof(JSFunctionAttribute).FullName)
                methods.Add(methodInspector.Inspect(method, MethodKind.Function));
            else if (attr.AttributeType.FullName == typeof(JSEventAttribute).FullName)
                methods.Add(methodInspector.Inspect(method, MethodKind.Event));
    }

    private void InspectAssemblyAttribute (CustomAttributeData attribute)
    {
        var name = attribute.AttributeType.FullName;
        var kind = name == typeof(JSExportAttribute).FullName ? InterfaceKind.Export
            : name == typeof(JSImportAttribute).FullName ? InterfaceKind.Import
            : (InterfaceKind?)null;
        if (!kind.HasValue) return;
        foreach (var arg in (IEnumerable<CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value!)
            InspectInterfaceType((Type)arg.Value!, kind.Value);
    }

    private void InspectInterfaceType (Type type, InterfaceKind kind)
    {
        var meta = interfaceInspector.Inspect(type, kind);
        interfaces.Add(meta);
        foreach (var method in meta.Methods)
            methods.Add(method.Generated);
    }
}
