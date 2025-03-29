using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class PartialMethod (MethodDeclarationSyntax syntax)
{
    private IMethodSymbol method = null!;

    public string EmitSource (Compilation compilation)
    {
        method = compilation.GetSemanticModel(syntax.SyntaxTree).GetDeclaredSymbol(syntax)!;
        return $"""
                {syntax.Modifiers} {EmitSignature()} =>
                    #if BOOTSHARP_EMITTED
                    {EmitBody()};
                    #else
                    throw new System.NotImplementedException("https://github.com/elringus/bootsharp/issues/173");
                    #endif
                """;
    }

    private string EmitSignature ()
    {
        var args = method.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}");
        return $"{BuildSyntax(method.ReturnType)} {method.Name} ({string.Join(", ", args)})";
    }

    private string EmitBody ()
    {
        return $"global::Bootsharp.Generated.Interop.{BuildName()}({BuildArgs()})";
    }

    private string BuildName ()
    {
        var name = method.ContainingNamespace.IsGlobalNamespace
            ? $"{method.ContainingType.Name}_{method.Name}"
            : string.Join("_", [..method.ContainingNamespace.ConstituentNamespaces, method.ContainingType.Name, method.Name]);
        return $"Proxy_{name.Replace(".", "_")}";
    }

    private string BuildArgs ()
    {
        if (method.Parameters.Length == 0) return "";
        return string.Join(", ", method.Parameters.Select(p => p.Name));
    }

    private static string BuildSyntax (ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Void) return "void";
        if (type is IArrayTypeSymbol arrayType) return $"{BuildSyntax(arrayType.ElementType)}[]";
        var nullable = type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
        if (IsGeneric(type, out var args)) return BuildGeneric(type, args) + nullable;
        return $"global::{ResolveTypeName(type)}{nullable}";

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
