using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generate;

internal sealed class SyntaxReceiver
{
    public List<PartialClass> FunctionClasses { get; } = [];
    public List<PartialClass> EventClasses { get; } = [];

    public void VisitNode (SyntaxNode node, Compilation compilation)
    {
        if (node is ClassDeclarationSyntax classSyntax)
            VisitClass(classSyntax, compilation);
    }

    private void VisitClass (ClassDeclarationSyntax syntax, Compilation compilation)
    {
        var functions = GetMethodsWithAttribute(syntax, "JSFunction");
        if (functions.Count > 0) FunctionClasses.Add(new(compilation, syntax, functions));
        var events = GetMethodsWithAttribute(syntax, "JSEvent");
        if (events.Count > 0) EventClasses.Add(new(compilation, syntax, events));
    }

    private List<PartialMethod> GetMethodsWithAttribute (ClassDeclarationSyntax syntax, string attribute)
    {
        return syntax.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(s => HasAttribute(s, attribute))
            .Select(m => new PartialMethod(m)).ToList();
    }

    private bool HasAttribute (MethodDeclarationSyntax syntax, string attributeName)
    {
        return syntax.AttributeLists
            .SelectMany(l => l.Attributes)
            .Any(a => a.ToString().Contains(attributeName));
    }
}
