using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class ImportMethod (MethodDeclarationSyntax stx)
{
    private IMethodSymbol method = null!;

    public string EmitSource (Compilation cmp)
    {
        method = cmp.GetSemanticModel(stx.SyntaxTree).GetDeclaredSymbol(stx)!;
        return $"""
                public static delegate* managed<{EmitPointerSignature()}> Bootsharp_{method.Name};
                    {stx.Modifiers} {EmitMethodSignature()} => Bootsharp_{method.Name}({EmitArgs()});
                """;
    }

    private string EmitPointerSignature ()
    {
        var args = method.Parameters.Select(p => BuildSyntax(p.Type)).ToList();
        args.Add(BuildSyntax(method.ReturnType));
        return string.Join(", ", args);
    }

    private string EmitMethodSignature ()
    {
        var args = method.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}");
        return $"{BuildSyntax(method.ReturnType)} {method.Name} ({string.Join(", ", args)})";
    }

    private string EmitArgs ()
    {
        if (method.Parameters.Length == 0) return "";
        return string.Join(", ", method.Parameters.Select(p => p.Name));
    }
}
