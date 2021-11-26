using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class FunctionClass
    {
        private readonly ClassDeclarationSyntax syntax;
        private readonly IReadOnlyList<FunctionMethod> methods;

        public FunctionClass (ClassDeclarationSyntax syntax, IReadOnlyList<FunctionMethod> methods)
        {
            this.syntax = syntax;
            this.methods = methods;
        }

        public string EmitSource () => EmitImport() + WrapNamespace(EmitHeader() + EmitMethods() + EmitFooter());

        private string EmitImport ()
        {
            var imports = syntax.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<UsingDirectiveSyntax>();
            return string.Join("\n", imports) + "\n";
        }

        private string EmitHeader () => $"{syntax.Modifiers} class {syntax.Identifier}\n{{\n";

        private string EmitMethods ()
        {
            var sources = methods.Select(m => m.EmitSource());
            return string.Join("\n", sources);
        }

        private string EmitFooter () => "\n}";

        private string WrapNamespace (string source)
        {
            if (syntax.Parent is NamespaceDeclarationSyntax space)
                return $"namespace {space.Name}\n{{\n{source}\n}}";
            return source;
        }
    }
}
