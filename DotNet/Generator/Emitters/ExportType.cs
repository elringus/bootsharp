using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Generator.Common;

namespace Generator
{
    internal sealed class ExportType
    {
        public string Name { get; }

        private readonly ITypeSymbol type;
        private readonly AttributeData attribute;

        public ExportType (ITypeSymbol type, AttributeData attribute)
        {
            this.type = type;
            this.attribute = attribute;
            Name = type.Name;
        }

        public static IEnumerable<ExportType> Resolve (IAssemblySymbol assembly) =>
            assembly.GetAttributes().FirstOrDefault(IsExportAttribute) is { } attribute
                ? attribute.ConstructorArguments[0].Values
                    .Select(v => v.Value).OfType<ITypeSymbol>()
                    .Where(t => t.TypeKind == TypeKind.Interface)
                    .Select(t => new ExportType(t, attribute))
                : Array.Empty<ExportType>();

        public string EmitSource ()
        {
            var specType = BuildFullName(type);
            var implType = BuildBindingType(type);

            return MuteNullableWarnings($@"using Microsoft.JSInterop;

namespace {BuildBindingNamespace(type)};

public class {implType}
{{
    private static {specType} handler = null!;

    public {implType} ({specType} handler)
    {{
        {implType}.handler = handler;
    }}

    {string.Join("\n    ", type.GetMembers().OfType<IMethodSymbol>().Select(EmitMethod))}
}}");
            string EmitMethod (IMethodSymbol method)
            {
                return $"[JSInvokable] public static {EmitSignature()} => {EmitBody()};";

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
}
