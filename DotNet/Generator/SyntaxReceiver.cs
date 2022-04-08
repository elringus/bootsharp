using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<GeneratedClass> FunctionClasses { get; } = new List<GeneratedClass>();
        public List<GeneratedClass> EventClasses { get; } = new List<GeneratedClass>();

        public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classSyntax)
                VisitClass(classSyntax);
        }

        private void VisitClass (ClassDeclarationSyntax syntax)
        {
            var functions = GetFunctions(syntax);
            if (functions.Count > 0) FunctionClasses.Add(new GeneratedClass(syntax, functions));
            var events = GetEvents(syntax);
            if (events.Count > 0) EventClasses.Add(new GeneratedClass(syntax, events));
        }

        private List<GeneratedMethod> GetFunctions (ClassDeclarationSyntax syntax)
        {
            return syntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(s => HasAttribute(s, Attributes.Function))
                .Select(m => new GeneratedMethod(m, false)).ToList();
        }

        private List<GeneratedMethod> GetEvents (ClassDeclarationSyntax syntax)
        {
            return syntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(s => HasAttribute(s, Attributes.Event))
                .Select(m => new GeneratedMethod(m, true)).ToList();
        }

        private bool HasAttribute (MethodDeclarationSyntax syntax, string attributeName)
        {
            return syntax.AttributeLists
                .SelectMany(l => l.Attributes)
                .Any(a => a.ToString().Contains(attributeName));
        }
    }
}
