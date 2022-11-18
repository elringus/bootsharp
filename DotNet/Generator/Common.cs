using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Generator
{
    internal static class Common
    {
        public static string MuteNullableWarnings (string source)
        {
            return "#nullable enable\n#pragma warning disable\n\n" +
                   source +
                   "\n\n#pragma warning restore\n#nullable restore\n";
        }

        public static string BuildBindingName (ITypeSymbol type)
        {
            return $"JS{type.Name.Substring(1)}";
        }

        public static string BuildFullName (ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Void) return "void";
            var nullable = type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
            var name = IsGeneric(type, out var args) ? $"{type.Name}<{string.Join(", ", args.Select(BuildFullName))}>" : type.Name;
            return $"global::{ResolveNamespace(type)}.{name}{nullable}";
        }

        public static string BuildInvoke (IMethodSymbol method, Compilation compilation)
        {
            var @event = method.GetAttributes().Any(IsEventAttribute) ||
                         method.ReturnsVoid && !method.GetAttributes().Any(IsFunctionAttribute);
            var async = method.ReturnType.Name == "ValueTask" || method.ReturnType.Name == "Task";
            var assembly = ConvertNamespace(ResolveNamespace(method), compilation.Assembly);
            var invokeMethod = GetInvokeMethod();
            var invokeParameters = GetInvokeParameters();
            var convertTask = method.ReturnType.Name == "Task" ? ".AsTask()" : "";
            return $"JS.{invokeMethod}({invokeParameters}){convertTask}";

            string GetInvokeMethod ()
            {
                if (method.ReturnsVoid) return "Invoke";
                if (async && IsGeneric(method.ReturnType, out var args))
                    return $"InvokeAsync<{string.Join(", ", args.Select(BuildFullName))}>";
                if (async) return "InvokeAsync";
                return $"Invoke<{BuildFullName(method.ReturnType)}>";
            }

            string GetInvokeParameters ()
            {
                var parameters = method.Parameters.Select(p => p.Name).ToArray();
                var args = $"\"dotnet.{assembly}.{method.Name}{(@event ? ".broadcast" : "")}\"";
                if (parameters.Length == 0) return args;
                args += $", new object[] {{ {string.Join(", ", parameters)} }}";
                return args;
            }
        }

        public static bool IsGeneric (ITypeSymbol type, out ImmutableArray<ITypeSymbol> args)
        {
            args = type is INamedTypeSymbol { IsGenericType: true } named ? named.TypeArguments : default;
            return args != default;
        }

        public static string ResolveNamespace (ISymbol symbol)
        {
            return symbol.ContainingNamespace.IsGlobalNamespace ? "Bindings"
                : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);
        }

        public static string ConvertNamespace (string space, IAssemblySymbol assembly)
        {
            foreach (var attribute in assembly.GetAttributes().Where(IsNamespaceAttribute))
                space = Convert(space, attribute);
            return space;

            static string Convert (string space, AttributeData attribute) =>
                Regex.Replace(space,
                    (string)attribute.ConstructorArguments[0].Value,
                    (string)attribute.ConstructorArguments[1].Value);
        }

        public static bool IsFunctionAttribute (AttributeData attribute)
        {
            return IsDotNetJSAttribute(attribute, "JSFunctionAttribute");
        }

        public static bool IsEventAttribute (AttributeData attribute)
        {
            return IsDotNetJSAttribute(attribute, "JSEventAttribute");
        }

        public static bool IsExportAttribute (AttributeData attribute)
        {
            return IsDotNetJSAttribute(attribute, "JSExportAttribute");
        }

        public static bool IsImportAttribute (AttributeData attribute)
        {
            return IsDotNetJSAttribute(attribute, "JSImportAttribute");
        }

        public static bool IsNamespaceAttribute (AttributeData attribute)
        {
            return IsDotNetJSAttribute(attribute, "JSNamespaceAttribute");
        }

        private static bool IsDotNetJSAttribute (AttributeData attribute, string name)
        {
            return attribute.AttributeClass != null &&
                   attribute.AttributeClass.Name == name;
        }
    }
}
