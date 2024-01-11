using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspector (JSSpaceBuilder spaceBuilder)
{
    private readonly List<AssemblyMeta> assemblies = [];
    private readonly List<MethodMeta> methods = [];
    private readonly List<Type> exports = [];
    private readonly List<Type> imports = [];
    private readonly List<string> warnings = [];
    private readonly TypeConverter converter = new(spaceBuilder);

    public AssemblyInspection InspectInDirectory (string directory)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var assemblyPath in Directory.GetFiles(directory, "*.dll"))
            try { InspectAssemblyFile(assemblyPath, ctx); }
            catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
        return CreateInspection(ctx);
    }

    private void InspectAssemblyFile (string assemblyPath, MetadataLoadContext ctx)
    {
        assemblies.Add(CreateAssembly(assemblyPath));
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
        Assemblies = assemblies.ToImmutableArray(),
        Methods = methods.ToImmutableArray(),
        Crawled = converter.CrawledTypes.ToImmutableArray(),
        Exports = exports.ToImmutableArray(),
        Imports = imports.ToImmutableArray(),
        Warnings = warnings.ToImmutableArray()
    };

    private AssemblyMeta CreateAssembly (string assemblyPath) => new() {
        Name = Path.GetFileNameWithoutExtension(assemblyPath),
        Bytes = File.ReadAllBytes(assemblyPath)
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
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        foreach (var attr in method.CustomAttributes)
            if (attr.AttributeType.FullName == typeof(JSInvokableAttribute).FullName)
                methods.Add(CreateMethod(method, MethodType.Invokable));
            else if (attr.AttributeType.FullName == typeof(JSFunctionAttribute).FullName)
                methods.Add(CreateMethod(method, MethodType.Function));
            else if (attr.AttributeType.FullName == typeof(JSEventAttribute).FullName)
                methods.Add(CreateMethod(method, MethodType.Event));
    }

    private void InspectAssemblyAttribute (CustomAttributeData attribute)
    {
        if (attribute.AttributeType.FullName == typeof(JSExportAttribute).FullName)
            exports.AddRange(((IEnumerable<CustomAttributeTypedArgument>)attribute
                .ConstructorArguments[0].Value!).Select(v => (Type)v.Value!));
        else if (attribute.AttributeType.FullName == typeof(JSImportAttribute).FullName)
            imports.AddRange(((IEnumerable<CustomAttributeTypedArgument>)attribute
                .ConstructorArguments[0].Value!).Select(v => (Type)v.Value!));
    }

    private MethodMeta CreateMethod (MethodInfo info, MethodType type) => new() {
        Type = type,
        Assembly = info.DeclaringType!.Assembly.GetName().Name!,
        Space = info.DeclaringType.FullName!,
        Name = info.Name,
        Arguments = info.GetParameters().Select(CreateArgument).ToArray(),
        ReturnValue = new() {
            Type = info.ReturnType,
            TypeSyntax = BuildSyntax(info.ReturnType, info.ReturnParameter),
            JSTypeSyntax = converter.ToTypeScript(info.ReturnType, GetNullability(info.ReturnParameter)),
            Nullable = IsNullable(info),
            Async = IsTaskLike(info.ReturnType),
            Void = IsVoid(info.ReturnType),
            Serialized = ShouldSerialize(info.ReturnType)
        },
        JSSpace = spaceBuilder.Build(info.DeclaringType),
        JSName = ToFirstLower(info.Name),
    };

    private ArgumentMeta CreateArgument (ParameterInfo info) => new() {
        Name = info.Name!,
        JSName = info.Name == "function" ? "fn" : info.Name!,
        Value = new() {
            Type = info.ParameterType,
            TypeSyntax = BuildSyntax(info.ParameterType, info),
            JSTypeSyntax = converter.ToTypeScript(info.ParameterType, GetNullability(info)),
            Nullable = IsNullable(info),
            Async = false,
            Void = false,
            Serialized = ShouldSerialize(info.ParameterType)
        }
    };
}
