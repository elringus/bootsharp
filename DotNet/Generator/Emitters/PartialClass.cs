using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Generator.Common;

namespace Generator
{
    internal class PartialClass
    {
        public string Name { get; }

        private readonly ClassDeclarationSyntax syntax;
        private readonly IReadOnlyList<PartialMethod> methods;

        public PartialClass (ClassDeclarationSyntax syntax, IReadOnlyList<PartialMethod> methods)
        {
            this.syntax = syntax;
            this.methods = methods;
            Name = syntax.Identifier.ToString();
        }

        public string EmitSource (Compilation compilation)
        {
            return MuteNullableWarnings(
                EmitUsings() +
                WrapNamespace(
                    EmitHeader() +
                    EmitMethods(compilation) +
                    EmitFooter()
                )
            );
        }

        private string EmitUsings ()
        {
            var imports = syntax.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<UsingDirectiveSyntax>();
            var result = string.Join("\n", imports);
            return string.IsNullOrEmpty(result) ? "" : result + "\n\n";
        }

        private string EmitHeader () => $"{syntax.Modifiers} class {syntax.Identifier}\n{{\n";

        private string EmitMethods (Compilation compilation)
        {
            var sources = methods.Select(m => "    " + m.EmitSource(compilation));
            return string.Join("\n", sources);
        }

        private string EmitFooter () => "\n}";

        private string WrapNamespace (string source)
        {
            if (syntax.Parent is NamespaceDeclarationSyntax space)
                return $"namespace {space.Name}\n{{\n{source}\n}}";
            if (syntax.Parent is FileScopedNamespaceDeclarationSyntax fileSpace)
                return $"namespace {fileSpace.Name};\n\n{source}";
            return source;
        }
    }
}
