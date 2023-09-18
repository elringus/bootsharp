using Microsoft.CodeAnalysis;

namespace Bootsharp.Generator;

internal sealed class ExportType(Compilation compilation, ITypeSymbol type, AttributeData attribute)
{
    public string Name { get; } = type.Name;

    public static IEnumerable<ExportType> Resolve (Compilation compilation) =>
        compilation.Assembly.GetAttributes().FirstOrDefault(IsExportAttribute) is { } attribute
            ? attribute.ConstructorArguments[0].Values
                .Select(v => v.Value).OfType<ITypeSymbol>()
                .Where(t => t.TypeKind == TypeKind.Interface)
                .Select(t => new ExportType(compilation, t, attribute))
            : Array.Empty<ExportType>();

    public string EmitSource ()
    {
        var specType = BuildFullName(type);
        var implType = BuildBindingType(type);
        var space = BuildBindingNamespace(type);
        return EmitCommon
        ($$"""
           namespace {{space}};

           public class {{implType}}
           {
               private static {{specType}} handler = null!;

               public {{implType}} ({{specType}} handler)
               {
                   {{implType}}.handler = handler;
               }

               [ModuleInitializer]
               [DynamicDependency(DynamicallyAccessedMemberTypes.All, "{{space}}.{{implType}}", "{{compilation.Assembly.Name}}")]
               internal static void RegisterDynamicDependencies ()
               {
                   Bootsharp.Injection.AddExport(typeof({{specType}}), typeof({{implType}}), handler => new {{implType}}(({{specType}})handler));
               }

               {{string.Join("\n    ", type.GetMembers().OfType<IMethodSymbol>().Select(EmitMethod))}}
           }
           """);

        string EmitMethod (IMethodSymbol method)
        {
            return $"[{InvokableAttribute}] public static {EmitSignature()} => {EmitBody()};";

            string EmitSignature ()
            {
                var methodName = ConvertMethodName(method.Name, attribute);
                var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
                return $"{BuildFullName(method.ReturnType)} {methodName} ({string.Join(", ", args)})";
            }

            string EmitBody ()
            {
                var args = method.Parameters.Select(p => p.Name);
                var body = $"handler.{method.Name}({string.Join(", ", args)})";
                return ConvertMethodInvocation(body, attribute);
            }
        }
    }
}
