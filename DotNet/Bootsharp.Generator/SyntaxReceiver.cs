using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Bootsharp.Generator.Common;

namespace Bootsharp.Generator;

internal sealed class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<PartialClass> InvokableClasses { get; } = new();
    public List<PartialClass> FunctionClasses { get; } = new();
    public List<PartialClass> EventClasses { get; } = new();

    public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classSyntax)
            VisitClass(classSyntax);
    }

    private void VisitClass (ClassDeclarationSyntax syntax)
    {
        var invokable = GetMethodsWithAttribute(syntax, InvokableAttribute);
        if (invokable.Count > 0) InvokableClasses.Add(new PartialClass(syntax, invokable));
        var functions = GetMethodsWithAttribute(syntax, FunctionAttribute);
        if (functions.Count > 0) FunctionClasses.Add(new PartialClass(syntax, functions));
        var events = GetMethodsWithAttribute(syntax, EventAttribute);
        if (events.Count > 0) EventClasses.Add(new PartialClass(syntax, events));
    }

    private List<PartialMethod> GetMethodsWithAttribute (ClassDeclarationSyntax syntax, string attribute)
    {
        return syntax.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(s => HasAttribute(s, attribute))
            .Select(m => new PartialMethod(m, GetMethodType(attribute))).ToList();
    }

    private bool HasAttribute (MethodDeclarationSyntax syntax, string attributeName)
    {
        return syntax.AttributeLists
            .SelectMany(l => l.Attributes)
            .Any(a => a.ToString().Contains(attributeName));
    }
}
