using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bootsharp.Generator;

internal sealed class SyntaxReceiver
{
    public List<PartialClass> InvokableClasses { get; } = new();
    public List<PartialClass> FunctionClasses { get; } = new();
    public List<PartialClass> EventClasses { get; } = new();

    public void VisitNode (SyntaxNode node, Compilation compilation)
    {
        if (node is ClassDeclarationSyntax classSyntax)
            VisitClass(classSyntax, compilation);
    }

    private void VisitClass (ClassDeclarationSyntax syntax, Compilation compilation)
    {
        var invokable = GetMethodsWithAttribute(syntax, InvokableAttribute);
        if (invokable.Count > 0) InvokableClasses.Add(new(compilation, syntax, Array.Empty<PartialMethod>(), true));
        var functions = GetMethodsWithAttribute(syntax, FunctionAttribute);
        if (functions.Count > 0) FunctionClasses.Add(new(compilation, syntax, functions, invokable.Count == 0));
        var events = GetMethodsWithAttribute(syntax, EventAttribute);
        if (events.Count > 0) EventClasses.Add(new(compilation, syntax, events, invokable.Count + functions.Count == 0));
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
