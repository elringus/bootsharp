using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Generator.Common;

namespace Generator
{
    internal class ImportType
    {
        public string Name { get; }

        private readonly ITypeSymbol type;
        private readonly AttributeData attribute;

        public ImportType (ITypeSymbol type, AttributeData attribute)
        {
            this.type = type;
            this.attribute = attribute;
            Name = type.Name;
        }

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

            return MuteNullableWarnings($@"using DotNetJS;

namespace {BuildBindingNamespace(type)};

public class {implType} : {specType}
{{
    {string.Join("\n    ", methods.Select(EmitBinding))}

    {string.Join("\n    ", methods.Select(EmitSpec))}
}}");
            string EmitBinding (IMethodSymbol method)
            {
                var methodName = ConvertMethodName(method.Name, attribute);
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
    }
}
