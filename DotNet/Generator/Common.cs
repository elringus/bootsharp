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

        public const string NamePatternArg = "NamePattern";
        public const string NameReplacementArg = "NameReplacement";
        public const string InvokePatternArg = "InvokePattern";
        public const string InvokeReplacementArg = "InvokeReplacement";
        public const string EventPatternArg = "EventPattern";
        public const string EventPatternReplacementArg = "EventPatternReplacement";

        public static string MuteNullableWarnings (string source)
        {
            return "#nullable enable\n#pragma warning disable\n" +
                   source +
                   "\n#pragma warning restore\n#nullable restore\n";
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
            if (IsGeneric(type, out var args)) return BuildGeneric(type, args) + nullable;
            return $"global::{ResolveNamespace(type)}.{type.Name}{nullable}";

            static string BuildGeneric (ITypeSymbol type, ImmutableArray<ITypeSymbol> args)
            {
                if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return BuildFullName(args[0]);
                return $"global::{ResolveNamespace(type)}.{type.Name}<{string.Join(", ", args.Select(BuildFullName))}>";
            }
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
                var args = $"\"dotnet.{assembly}.{ToFirstLower(methodName)}{(@event ? ".broadcast" : "")}\"";
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
            var pattern = attribute.NamedArguments.FirstOrDefault(a => a.Key == NamePatternArg).Value.Value as string;
            var replacement = attribute.NamedArguments.FirstOrDefault(a => a.Key == NameReplacementArg).Value.Value as string;
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(replacement)) return name;
            return Regex.Replace(name, pattern, replacement);
        }

        public static string ConvertEventName (string name, AttributeData attribute)
        {
            var pattern = attribute.NamedArguments.FirstOrDefault(a => a.Key == EventPatternArg).Value.Value as string;
            var replacement = attribute.NamedArguments.FirstOrDefault(a => a.Key == EventPatternReplacementArg).Value.Value as string;
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(replacement)) return name;
            return Regex.Replace(name, pattern, replacement);
        }

        public static string ConvertMethodInvocation (string body, AttributeData attribute)
        {
            var pattern = attribute.NamedArguments.FirstOrDefault(a => a.Key == InvokePatternArg).Value.Value as string;
            var replacement = attribute.NamedArguments.FirstOrDefault(a => a.Key == InvokeReplacementArg).Value.Value as string;
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(replacement)) return body;
            return Regex.Replace(body, pattern, replacement);
        }

        public static bool IsExportAttribute (AttributeData attribute) => IsJSAttribute(attribute, ExportAttribute);
        public static bool IsImportAttribute (AttributeData attribute) => IsJSAttribute(attribute, ImportAttribute);
        public static bool IsNamespaceAttribute (AttributeData attribute) => IsJSAttribute(attribute, NamespaceAttribute);

        public static bool IsJSAttribute (AttributeData attribute, string name) =>
            attribute.AttributeClass!.ContainingNamespace.Name == "Bootsharp" &&
            attribute.AttributeClass.Name == name;

        public static string ToFirstLower (string value)
        {
            if (value.Length == 1) char.ToLowerInvariant(value[0]);
            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }
}
