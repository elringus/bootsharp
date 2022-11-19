using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Generator.Common;

namespace Generator
{
    internal class ImportType
    {
        public static IEnumerable<ITypeSymbol> Resolve (IAssemblySymbol assembly) =>
            assembly.GetAttributes()
                .FirstOrDefault(IsImportAttribute)?
                .ConstructorArguments[0].Values.Select(v => v.Value).OfType<ITypeSymbol>()
                .Where(t => t.TypeKind == TypeKind.Interface) ?? Array.Empty<ITypeSymbol>();

        public static string EmitSource (ITypeSymbol type, Compilation compilation)
        {
            var specType = BuildFullName(type);
            var implType = BuildBindingType(type);
            var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();

            return MuteNullableWarnings($@"using DotNetJS;

namespace {BuildBindingNamespace(type)};

public class {implType} : {specType}
{{
    {string.Join("\n    ", methods.Select(EmitBinding))}

    {string.Join("\n    ", methods.Select(EmitSpec))}
}}");
            string EmitBinding (IMethodSymbol method)
            {
                var methodName = ConvertMethodName(method.Name, compilation.Assembly, ImportAttribute);
                return $"{EmitAttribute()} public static {EmitSignature()} => {EmitBody()};";

                string EmitAttribute () => IsEvent(method) ? "[JSEvent]" : "[JSFunction]";

                string EmitSignature ()
                {
                    var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
                    return $"{BuildFullName(method.ReturnType)} {methodName} ({string.Join(", ", args)})";
                }

                string EmitBody ()
                {
                    var body = BuildInvoke(method, methodName, compilation);
                    return ConvertMethodInvocation(body, compilation.Assembly, ImportAttribute);
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
                    var methodName = ConvertMethodName(method.Name, compilation.Assembly, ImportAttribute);
                    var args = method.Parameters.Select(p => p.Name);
                    return $"{methodName}({string.Join(", ", args)})";
                }
            }
        }
    }
}
