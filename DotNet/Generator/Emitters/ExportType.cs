using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Generator.Common;

namespace Generator
{
    internal class ExportType
    {
        public static IEnumerable<ITypeSymbol> Resolve (IAssemblySymbol assembly) =>
            assembly.GetAttributes()
                .FirstOrDefault(IsExportAttribute)?
                .ConstructorArguments[0].Values.Select(v => v.Value).OfType<ITypeSymbol>()
                .Where(t => t.TypeKind == TypeKind.Interface) ?? Array.Empty<ITypeSymbol>();

        public static string EmitSource (ITypeSymbol type, Compilation compilation)
        {
            var specType = BuildFullName(type);
            var implType = BuildBindingName(type);

            return MuteNullableWarnings($@"using Microsoft.JSInterop;

namespace {ResolveNamespace(type)};

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
                    var methodName = ConvertMethodName(method.Name, compilation.Assembly, ExportAttribute);
                    var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
                    return $"{BuildFullName(method.ReturnType)} {methodName} ({string.Join(", ", args)})";
                }

                string EmitBody ()
                {
                    var args = method.Parameters.Select(p => p.Name);
                    var body = $"handler.{method.Name}({string.Join(", ", args)})";
                    return ConvertMethodInvocation(body, compilation.Assembly, ExportAttribute);
                }
            }
        }
    }
}
