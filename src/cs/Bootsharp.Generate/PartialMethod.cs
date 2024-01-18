using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class PartialMethod (MethodDeclarationSyntax syntax)
{
    private IMethodSymbol method;

    public string EmitSource (Compilation compilation)
    {
        method = compilation.GetSemanticModel(syntax.SyntaxTree).GetDeclaredSymbol(syntax)!;
        return $"{syntax.Modifiers} {EmitSignature()} => {EmitBody()};".Replace("partial async", "async partial");
    }

    private string EmitSignature ()
    {
        var args = method.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}");
        return $"{BuildSyntax(method.ReturnType)} {method.Name} ({string.Join(", ", args)})";
    }

    private string EmitBody ()
    {
        return $"""global::Bootsharp.Proxies.Get<{BuildGetterType()}>("{BuildId()}")({BuildArgs()})""";
    }

    private string BuildId ()
    {
        if (method.ContainingNamespace.IsGlobalNamespace) return $"{method.ContainingType.Name}.{method.Name}";
        return string.Join(".", [..method.ContainingNamespace.ConstituentNamespaces, method.ContainingType.Name, method.Name]);
    }

    private string BuildGetterType ()
    {
        if (method.ReturnsVoid && method.Parameters.Length == 0) return "global::System.Action";
        var basename = method.ReturnsVoid ? "global::System.Action" : "global::System.Func";
        var args = method.Parameters.Select(p => BuildSyntax(p.Type));
        if (!method.ReturnsVoid) args = args.Append(BuildSyntax(method.ReturnType));
        return $"{basename}<{string.Join(", ", args)}>";
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
