using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Generator
{
    internal static class Common
    {
        public const string FunctionAttribute = "JSFunctionAttribute";
        public const string EventAttribute = "JSEventAttribute";
        public const string ExportAttribute = "JSExportAttribute";
        public const string ImportAttribute = "JSImportAttribute";
        public const string NamespaceAttribute = "JSNamespaceAttribute";

        public static string MuteNullableWarnings (string source)
        {
            return "#nullable enable\n#pragma warning disable\n\n" +
                   source +
                   "\n\n#pragma warning restore\n#nullable restore\n";
        }

        public static string BuildBindingType (ITypeSymbol type)
        {
            return $"JS{type.Name.Substring(1)}";
        }

        public static string BuildBindingNamespace (ITypeSymbol type)
        {
            return type.Name.Substring(1);
        }

        public static string BuildFullName (ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Void) return "void";
            if (type is IArrayTypeSymbol arrayType) return $"{BuildFullName(arrayType.ElementType)}[]";
            var nullable = type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
            var name = IsGeneric(type, out var args) ? $"{type.Name}<{string.Join(", ", args.Select(BuildFullName))}>" : type.Name;
            return $"global::{ResolveNamespace(type)}.{name}{nullable}";
        }

        public static string BuildInvoke (IMethodSymbol method, string methodName, Compilation compilation)
        {
            var @event = IsEvent(method);
            var async = method.ReturnType.Name == "ValueTask" || method.ReturnType.Name == "Task";
            var assembly = ConvertNamespace(BuildBindingNamespace(method.ContainingType), compilation.Assembly);
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
                var args = $"\"dotnet.{assembly}.{methodName}{(@event ? ".broadcast" : "")}\"";
                if (parameters.Length == 0) return args;
                args += $", new object[] {{ {string.Join(", ", parameters)} }}";
                return args;
            }
        }

        public static bool IsEvent (IMethodSymbol method)
        {
            return method.Name.StartsWith("Notify");
        }

        public static bool IsGeneric (ITypeSymbol type, out ImmutableArray<ITypeSymbol> args)
        {
            args = type is INamedTypeSymbol { IsGenericType: true } named ? named.TypeArguments : default;
            return args != default;
        }

        public static string ResolveNamespace (ISymbol symbol)
        {
            if (symbol.ContainingNamespace.IsGlobalNamespace) return "Bindings";
            return string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);
        }

        public static string ConvertNamespace (string space, IAssemblySymbol assembly)
        {
            foreach (var attribute in assembly.GetAttributes().Where(IsNamespaceAttribute))
                space = Convert(space, attribute);
            return space;

            static string Convert (string space, AttributeData attribute)
            {
                var pattern = (string)attribute.ConstructorArguments[0].Value;
                var replacement = (string)attribute.ConstructorArguments[1].Value;
                return Regex.Replace(space, pattern, replacement);
            }
        }

        public static string ConvertMethodName (string name, AttributeData attribute)
        {
            if (string.IsNullOrEmpty(attribute.ConstructorArguments.ElementAtOrDefault(1).Value as string) ||
                string.IsNullOrEmpty(attribute.ConstructorArguments.ElementAtOrDefault(2).Value as string))
                return name;
            return Regex.Replace(name,
                (string)attribute.ConstructorArguments[1].Value,
                (string)attribute.ConstructorArguments[2].Value);
        }

        public static string ConvertMethodInvocation (string body, AttributeData attribute)
        {
            if (string.IsNullOrEmpty(attribute.ConstructorArguments.ElementAtOrDefault(3).Value as string) ||
                string.IsNullOrEmpty(attribute.ConstructorArguments.ElementAtOrDefault(4).Value as string))
                return body;
            return Regex.Replace(body,
                (string)attribute.ConstructorArguments[3].Value,
                (string)attribute.ConstructorArguments[4].Value);
        }

        public static bool IsExportAttribute (AttributeData attribute) => IsJSAttribute(attribute, ExportAttribute);
        public static bool IsImportAttribute (AttributeData attribute) => IsJSAttribute(attribute, ImportAttribute);
        public static bool IsNamespaceAttribute (AttributeData attribute) => IsJSAttribute(attribute, NamespaceAttribute);

        public static bool IsJSAttribute (AttributeData attribute, string name) =>
            attribute.AttributeClass!.ContainingNamespace.Name == "DotNetJS" &&
            attribute.AttributeClass.Name == name;
    }
}
