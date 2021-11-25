using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<FunctionClass> FunctionClasses { get; } = new List<FunctionClass>();

        public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classSyntax)
                VisitClass(classSyntax);
        }

        private void VisitClass (ClassDeclarationSyntax syntax)
        {
            var methods = GetFunctionMethods(syntax);
            if (methods.Count == 0) return;
            FunctionClasses.Add(new FunctionClass(syntax, methods));
        }

        private List<FunctionMethod> GetFunctionMethods (ClassDeclarationSyntax syntax)
        {
            var className = syntax.Identifier.ToString();
            return syntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(HasFunctionAttribute)
                .Select(m => new FunctionMethod(className, m)).ToList();
        }

        private bool HasFunctionAttribute (MethodDeclarationSyntax syntax)
        {
            return syntax.AttributeLists
                .SelectMany(l => l.Attributes)
                .Any(a => a.ToString().Contains(Attributes.Function));
        }
    }
}
