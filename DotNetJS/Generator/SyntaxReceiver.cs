using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        private readonly List<JSFunctionClass> classes = new();

        public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classSyntax)
                ConsumeClass(classSyntax);
        }

        public string GetEmitted () => string.Join('\n', classes.Select(e => e.EmitSource()));

        private void ConsumeClass (ClassDeclarationSyntax classSyntax)
        {
            var functions = GetJSFunctions(classSyntax);
            if (functions.Count == 0) return;
            classes.Add(new JSFunctionClass(classSyntax, functions));
        }

        private List<JSFunctionMethod> GetJSFunctions (ClassDeclarationSyntax classSyntax)
        {
            return classSyntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(HasJSFunctionAttribute)
                .Select(m => new JSFunctionMethod(m)).ToList();
        }

        private bool HasJSFunctionAttribute (MethodDeclarationSyntax methodSyntax)
        {
            return methodSyntax.AttributeLists
                .SelectMany(l => l.Attributes)
                .Any(a => a.ToString().Contains(Attributes.JSFunction));
        }
    }
}
