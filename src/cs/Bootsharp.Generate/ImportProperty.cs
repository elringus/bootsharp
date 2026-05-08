using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class ImportProperty (PropertyDeclarationSyntax stx)
{
    public string EmitSource (Compilation cmp)
    {
        var p = (IPropertySymbol)cmp.GetSemanticModel(stx.SyntaxTree).GetDeclaredSymbol(stx)!;
        var type = BuildSyntax(p.Type);
        var canGet = p.GetMethod != null;
        var canSet = p.SetMethod != null;
        var get = canGet ? $"get => Bootsharp_GetProperty{p.Name}(); " : "";
        var set = canSet ? $"set => Bootsharp_SetProperty{p.Name}(value); " : "";
        var getter = canGet ? $"\n    public static delegate* managed<{type}> Bootsharp_GetProperty{p.Name};" : "";
        var setter = canSet ? $"\n    public static delegate* managed<{type}, void> Bootsharp_SetProperty{p.Name};" : "";
        return $"{stx.Modifiers} {type} {p.Name} {{ {get}{set}}}{getter}{setter}";
    }
}
