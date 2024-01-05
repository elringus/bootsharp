using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspector (NamespaceBuilder spaceBuilder)
{
    private readonly List<AssemblyMeta> assemblies = [];
    private readonly List<MethodMeta> methods = [];
    private readonly List<string> warnings = [];
    private readonly TypeConverter converter = new(spaceBuilder);

    public AssemblyInspection InspectInDirectory (string directory)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var assemblyPath in Directory.GetFiles(directory, "*.dll"))
            try { InspectAssembly(assemblyPath, ctx); }
            catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
        return CreateInspection(ctx);
    }

    private void InspectAssembly (string assemblyPath, MetadataLoadContext ctx)
    {
        assemblies.Add(CreateAssembly(assemblyPath));
        if (!ShouldIgnoreAssembly(assemblyPath))
            InspectMethods(ctx.LoadFromAssemblyPath(assemblyPath));
    }

    private void AddSkippedAssemblyWarning (string assemblyPath, Exception exception)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{assemblyName}' assembly; " +
                      $"affected methods won't be available in JavaScript. Error: {exception.Message}";
        warnings.Add(message);
    }

    private AssemblyInspection CreateInspection (MetadataLoadContext ctx) => new(ctx) {
        Assemblies = assemblies.ToImmutableArray(),
        Methods = methods.ToImmutableArray(),
        Types = converter.CrawledTypes.ToImmutableArray(),
        Warnings = warnings.ToImmutableArray()
    };

    private AssemblyMeta CreateAssembly (string assemblyPath) => new() {
        Name = Path.GetFileName(assemblyPath),
        Bytes = File.ReadAllBytes(assemblyPath)
    };

    private void InspectMethods (Assembly assembly)
    {
        foreach (var method in GetStaticMethods(assembly))
        foreach (var attribute in method.CustomAttributes)
            InspectMethodWithAttribute(method, attribute.AttributeType.Name);
    }

    private void InspectMethodWithAttribute (MethodInfo method, string attributeName)
    {
        if (attributeName == nameof(JSInvokableAttribute))
            methods.Add(CreateMethod(method, MethodType.Invokable));
        else if (attributeName == nameof(JSFunctionAttribute))
            methods.Add(CreateMethod(method, MethodType.Function));
        else if (attributeName == nameof(JSEventAttribute))
            methods.Add(CreateMethod(method, MethodType.Event));
    }

    private MethodMeta CreateMethod (MethodInfo info, MethodType type) => new() {
        Type = type,
        Assembly = info.DeclaringType!.Assembly.GetName().Name!,
        DeclaringName = info.DeclaringType.FullName!,
        Name = info.Name,
        Arguments = info.GetParameters().Select(CreateArgument).ToArray(),
        ReturnType = new() {
            Type = info.ReturnType,
            Syntax = BuildSyntax(info.ReturnType, info.ReturnParameter),
            JSSyntax = converter.ToTypeScript(info.ReturnType, GetNullability(info.ReturnParameter)),
            Void = IsVoid(info.ReturnType),
            Nullable = IsNullable(info),
            TaskLike = IsTaskLike(info.ReturnType),
            ShouldSerialize = ShouldSerialize(info.ReturnType)
        },
        JSSpace = spaceBuilder.Build(info.DeclaringType),
        JSName = ToFirstLower(info.Name),
    };

    private ArgumentMeta CreateArgument (ParameterInfo info) => new() {
        Name = info.Name!,
        JSName = info.Name == "function" ? "fn" : info.Name!,
        Type = new() {
            Type = info.ParameterType,
            Syntax = BuildSyntax(info.ParameterType, info),
            JSSyntax = converter.ToTypeScript(info.ParameterType, GetNullability(info)),
            Nullable = IsNullable(info),
            ShouldSerialize = ShouldSerialize(info.ParameterType),
            TaskLike = false,
            Void = false
        }
    };

    private static IEnumerable<MethodInfo> GetStaticMethods (Assembly assembly)
    {
        var exported = assembly.GetExportedTypes();
        return exported.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static));
    }
}
