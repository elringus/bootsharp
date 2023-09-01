using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Bootsharp.Generator.Common;

namespace Bootsharp.Generator;

internal sealed class ImportType(ITypeSymbol type, AttributeData attribute)
{
    public string Name { get; } = type.Name;

    public static IEnumerable<ImportType> Resolve (IAssemblySymbol assembly) =>
        assembly.GetAttributes().FirstOrDefault(IsImportAttribute) is { } attribute
            ? attribute.ConstructorArguments[0].Values
                .Select(v => v.Value).OfType<ITypeSymbol>()
                .Where(t => t.TypeKind == TypeKind.Interface)
                .Select(t => new ImportType(t, attribute))
            : Array.Empty<ImportType>();

    public string EmitSource (Compilation compilation)
    {
        var specType = BuildFullName(type);
        var implType = BuildBindingType(type);
        var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();
        var space = BuildBindingNamespace(type);

        return EmitCommon
        ($$"""
           namespace {{space}};

           public class {{implType}} : {{specType}}
           {
               [ModuleInitializer]
               [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "{{space}}.{{implType}}", "{{compilation.Assembly.Name}}")]
               internal static void RegisterDynamicDependencies () { }

               {{string.Join("\n    ", methods.Select(EmitBinding))}}

               {{string.Join("\n    ", methods.Select(EmitSpec))}}
           }
           """);

        string EmitBinding (IMethodSymbol method)
        {
            var methodName = ConvertMethodName(method.Name, attribute);
            return $"{EmitAttribute()} public static {EmitSignature()} => {EmitBody()};";

            string EmitAttribute () => IsEvent(method.Name, attribute) ? $"[{EventAttribute}]" : $"[{FunctionAttribute}]";

            string EmitSignature ()
            {
                var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
                return $"{BuildFullName(method.ReturnType)} {methodName} ({string.Join(", ", args)})";
            }

            string EmitBody ()
            {
                var body = BuildInvoke(method, methodName, compilation);
                return ConvertMethodInvocation(body, attribute);
            }
        }

        string EmitSpec (IMethodSymbol method)
        {
            return $"{EmitSignature()} => {EmitBody()};";

            string EmitSignature ()
            {
                var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
                return $"{BuildFullName(method.ReturnType)} {specType}.{method.Name} ({string.Join(", ", args)})";
            }

            string EmitBody ()
            {
                var methodName = ConvertMethodName(method.Name, attribute);
                var args = method.Parameters.Select(p => p.Name);
                return $"{methodName}({string.Join(", ", args)})";
            }
        }
    }

    private string BuildInvoke (IMethodSymbol method, string function, Compilation compilation)
    {
        var @event = IsEvent(method.Name, attribute);
        var async = method.ReturnType.Name == "Task";
        var module = ConvertNamespace(BuildBindingNamespace(method.ContainingType), compilation.Assembly);
        var invokeFunction = GetInvokeFunction();
        var invokeArgs = GetInvokeParameters();
        var handle = @event ? "Event" : "Function";
        return $"{handle}.{invokeFunction}({invokeArgs})";

        string GetInvokeFunction ()
        {
            if (@event) return "Broadcast";
            if (method.ReturnsVoid) return "InvokeVoid";
            if (async && IsGeneric(method.ReturnType, out var args))
                return $"InvokeAsync<{string.Join(", ", args.Select(BuildFullName))}>";
            if (async) return "InvokeVoidAsync";
            return $"Invoke<{BuildFullName(method.ReturnType)}>";
        }

        string GetInvokeParameters ()
        {
            var parameters = method.Parameters.Select(p => p.Name).ToArray();
            var args = $"\"{module}.{ToFirstLower(function)}\"";
            if (parameters.Length == 0) return args;
            args += $", {string.Join(", ", parameters)}";
            return args;
        }
    }
}
