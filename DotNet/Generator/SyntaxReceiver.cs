using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<ExportedType> ExportedTypes { get; } = new List<ExportedType>();
        public List<PartialClass> FunctionClasses { get; } = new List<PartialClass>();
        public List<PartialClass> EventClasses { get; } = new List<PartialClass>();

        public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classSyntax)
                VisitClass(classSyntax);
        }

        private void VisitClass (ClassDeclarationSyntax syntax)
        {
            var functions = GetFunctions(syntax);
            if (functions.Count > 0) FunctionClasses.Add(new PartialClass(syntax, functions));
            var events = GetEvents(syntax);
            if (events.Count > 0) EventClasses.Add(new PartialClass(syntax, events));
        }

        private List<PartialMethod> GetFunctions (ClassDeclarationSyntax syntax)
        {
            return syntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(s => HasAttribute(s, Attributes.Function))
                .Select(m => new PartialMethod(m, false)).ToList();
        }

        private List<PartialMethod> GetEvents (ClassDeclarationSyntax syntax)
        {
            return syntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(s => HasAttribute(s, Attributes.Event))
                .Select(m => new PartialMethod(m, true)).ToList();
        }

        private bool HasAttribute (MethodDeclarationSyntax syntax, string attributeName)
        {
            return syntax.AttributeLists
                .SelectMany(l => l.Attributes)
                .Any(a => a.ToString().Contains(attributeName));
        }
    }
}
