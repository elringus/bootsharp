using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class ImportEvent (EventFieldDeclarationSyntax stx)
{
    public string EmitSource (Compilation cmp)
    {
        var evt = (IEventSymbol)cmp.GetSemanticModel(stx.SyntaxTree)
            .GetDeclaredSymbol(stx.Declaration.Variables.Single())!;
        var inv = ((INamedTypeSymbol)evt.Type).DelegateInvokeMethod!;
        var sigArgs = string.Join(", ", inv.Parameters.Select(p => $"{BuildSyntax(p.Type)} {p.Name}"));
        var invArgs = string.Join(", ", inv.Parameters.Select(p => p.Name));
        return $"internal static void Bootsharp_Invoke_{evt.Name} ({sigArgs}) => " +
               $"{evt.Name}?.Invoke({invArgs});";
    }
}
