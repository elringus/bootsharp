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
            var implType = BuildBindingName(type);
            var @namespace = ConvertNamespace(ResolveNamespace(type), compilation.Assembly);
            var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();

            return MuteNullableWarnings($@"using DotNetJS;

namespace {@namespace};

public class {implType} : {specType}
{{
    {string.Join("\n    ", methods.Select(EmitBinding))}

    {string.Join("\n    ", methods.Select(EmitSpec))}
}}");
            string EmitBinding (IMethodSymbol method)
            {
                return $"public static {EmitSignature()} => {BuildInvoke(method, compilation)};";

                string EmitSignature ()
                {
                    var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
                    return $"{BuildFullName(method.ReturnType)} {method.Name} ({string.Join(", ", args)})";
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
                    var args = method.Parameters.Select(p => p.Name);
                    return $"{method.Name}({string.Join(", ", args)})";
                }
            }
        }
    }
}
