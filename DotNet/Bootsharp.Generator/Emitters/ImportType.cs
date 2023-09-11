using Microsoft.CodeAnalysis;

namespace Bootsharp.Generator;

internal sealed class ImportType(Compilation compilation, ITypeSymbol type, AttributeData attribute)
{
    public string Name { get; } = type.Name;

    private string specType;

    public static IEnumerable<ImportType> Resolve (Compilation compilation) =>
        compilation.Assembly.GetAttributes().FirstOrDefault(IsImportAttribute) is { } attribute
            ? attribute.ConstructorArguments[0].Values
                .Select(v => v.Value).OfType<ITypeSymbol>()
                .Where(t => t.TypeKind == TypeKind.Interface)
                .Select(t => new ImportType(compilation, t, attribute))
            : Array.Empty<ImportType>();

    public string EmitSource ()
    {
        specType = BuildFullName(type);
        var implType = BuildBindingType(type);
        var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();
        var space = BuildBindingNamespace(type);
        return EmitCommon
        ($$"""
           namespace {{space}};

           public class {{implType}} : {{specType}}
           {
               [ModuleInitializer]
               [DynamicDependency(DynamicallyAccessedMemberTypes.All, "{{space}}.{{implType}}", "{{compilation.Assembly.Name}}")]
               internal static void RegisterDynamicDependencies () { }

               {{string.Join("\n    ", methods.Select(EmitBinding))}}

               {{string.Join("\n    ", methods.Select(EmitSpec))}}
           }
           """);
    }

    private string EmitBinding (IMethodSymbol method)
    {
        var @event = IsEvent(method.Name, attribute);
        var space = ConvertNamespace(BuildBindingNamespace(method.ContainingType), compilation.Assembly);
        var name = ConvertMethodName(method.Name, attribute);
        new BindingEmitter(method, @event, space, name).Emit(out var sig, out var body);
        var attr = @event ? $"[{EventAttribute}]" : $"[{FunctionAttribute}]";
        return $"{attr} public static {sig} => {ConvertMethodInvocation(body, attribute)};";
    }

    private string EmitSpec (IMethodSymbol method)
    {
        var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
        var sig = $"{BuildFullName(method.ReturnType)} {specType}.{method.Name} ({string.Join(", ", args)})";
        var methodName = ConvertMethodName(method.Name, attribute);
        var @params = method.Parameters.Select(p => p.Name);
        return $"{sig} => {methodName}({string.Join(", ", @params)});";
    }
}
