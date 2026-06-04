using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

/// <summary>
/// An imported member (method, property or event) resolved into the C# code required to implement its binding.
/// </summary>
internal sealed record ImportMember (string Space, string Class, string TypeParams, string Modifiers, string Code)
{
    public static ImportMember? Resolve (GeneratorAttributeSyntaxContext ctx, CancellationToken _)
    {
        if (ctx.TargetNode.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } cls) return null;
        if (Emit(ctx.TargetSymbol, ctx.TargetNode) is not { } code) return null;
        var ns = ctx.TargetSymbol.ContainingType!.ContainingNamespace;
        var space = ns.IsGlobalNamespace ? "" : ns.ToDisplayString();
        var paras = cls.TypeParameterList?.ToString() ?? "";
        return new(space, cls.Identifier.ToString(), paras, cls.Modifiers.ToString(), code);
    }

    private static string? Emit (ISymbol smb, SyntaxNode stx) => smb switch {
        IMethodSymbol m => EmitMethod(m, (MethodDeclarationSyntax)stx),
        IPropertySymbol { IsStatic: true } p => EmitProperty(p, (PropertyDeclarationSyntax)stx),
        IEventSymbol { IsStatic: true } e => EmitEvent(e),
        _ => null
    };

    private static string EmitMethod (IMethodSymbol m, MethodDeclarationSyntax stx)
    {
        var ret = BuildSyntax(m.ReturnType);
        var ptr = string.Join(", ", m.Parameters.Select(p => BuildSyntax(p.Type)).Append(ret));
        var pars = string.Join(", ", m.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}"));
        var args = string.Join(", ", m.Parameters.Select(p => p.Name));
        return $"public static delegate* managed<{ptr}> Bootsharp_{m.Name};\n" +
               $"{stx.Modifiers} {ret} {m.Name} ({pars}) => Bootsharp_{m.Name}({args});";
    }

    private static string EmitProperty (IPropertySymbol p, PropertyDeclarationSyntax stx)
    {
        var type = BuildSyntax(p.Type);
        var get = p.GetMethod is null ? "" : $"get => Bootsharp_Get{p.Name}(); ";
        var set = p.SetMethod is null ? "" : $"set => Bootsharp_Set{p.Name}(value); ";
        var code = $"{stx.Modifiers} {type} {p.Name} {{ {get}{set}}}";
        if (p.GetMethod != null) code += $"\npublic static delegate* managed<{type}> Bootsharp_Get{p.Name};";
        if (p.SetMethod != null) code += $"\npublic static delegate* managed<{type}, void> Bootsharp_Set{p.Name};";
        return code;
    }

    private static string EmitEvent (IEventSymbol e)
    {
        var inv = ((INamedTypeSymbol)e.Type).DelegateInvokeMethod!;
        var pars = string.Join(", ", inv.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}"));
        var args = string.Join(", ", inv.Parameters.Select(p => p.Name));
        return $"internal static void Bootsharp_Invoke_{e.Name} ({pars}) => {e.Name}?.Invoke({args});";
    }

    private static string BuildSyntax (ITypeSymbol t)
    {
        if (t.SpecialType == SpecialType.System_Void) return "void";
        if (t is IArrayTypeSymbol array) return $"{BuildSyntax(array.ElementType)}[]";
        if (t.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return $"{BuildSyntax(((INamedTypeSymbol)t).TypeArguments[0])}?";
        var nil = t.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
        if (t is ITypeParameterSymbol) return $"{t.Name}{nil}";
        var nt = (INamedTypeSymbol)t;
        var args = nt.IsGenericType ? $"<{string.Join(", ", nt.TypeArguments.Select(BuildSyntax))}>" : "";
        var space = t.ContainingNamespace.IsGlobalNamespace ? t.Name : $"{t.ContainingNamespace}.{t.Name}";
        return $"global::{space}{args}{nil}";
    }
}
