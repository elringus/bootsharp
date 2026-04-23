global using static Bootsharp.Generate.Global;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Bootsharp.Generate;

internal static class Global
{
    public static string BuildSyntax (ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Void) return "void";
        if (type is IArrayTypeSymbol arrayType) return $"{BuildSyntax(arrayType.ElementType)}[]";
        var nil = type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
        if (IsGeneric(type, out var args)) return BuildGeneric(type, args) + nil;
        return $"global::{ResolveTypeName(type)}{nil}";

        static string BuildGeneric (ITypeSymbol type, ImmutableArray<ITypeSymbol> args)
        {
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return BuildSyntax(args[0]);
            return $"global::{ResolveTypeName(type)}<{string.Join(", ", args.Select(BuildSyntax))}>";
        }

        static string ResolveTypeName (ITypeSymbol type)
        {
            if (type.ContainingNamespace.IsGlobalNamespace) return type.Name;
            return string.Join(".", type.ContainingNamespace.ConstituentNamespaces) + "." + type.Name;
        }

        static bool IsGeneric (ITypeSymbol type, out ImmutableArray<ITypeSymbol> args)
        {
            args = type is INamedTypeSymbol { IsGenericType: true } named ? named.TypeArguments : default;
            return args != default;
        }
    }
}
