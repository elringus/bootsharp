using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class SyntaxReceiver
{
    public List<ImportClass> ImportClasses { get; } = [];

    public void VisitNode (SyntaxNode node, Compilation cmp)
    {
        if (node is ClassDeclarationSyntax classSyntax)
            VisitClass(classSyntax, cmp);
    }

    private void VisitClass (ClassDeclarationSyntax stx, Compilation cmp)
    {
        var methods = GetMethodsWithAttribute(stx, "Import");
        var events = GetEventsWithAttribute(stx, "Import");
        if (methods.Count > 0 || events.Count > 0)
            ImportClasses.Add(new(cmp, stx, methods, events));
    }

    private List<ImportMethod> GetMethodsWithAttribute (ClassDeclarationSyntax stx, string attribute)
    {
        return stx.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(s => HasAttribute(s, attribute))
            .Select(m => new ImportMethod(m)).ToList();
    }

    private List<ImportEvent> GetEventsWithAttribute (ClassDeclarationSyntax stx, string attribute)
    {
        return stx.Members
            .OfType<EventFieldDeclarationSyntax>()
            .Where(s => s.Modifiers.Any(m => m.Text == "static") && HasAttribute(s, attribute))
            .Select(e => new ImportEvent(e)).ToList();
    }

    private bool HasAttribute (MemberDeclarationSyntax stx, string attributeName)
    {
        return stx.AttributeLists
            .SelectMany(l => l.Attributes)
            .Any(a => a.ToString().Contains(attributeName));
    }
}
