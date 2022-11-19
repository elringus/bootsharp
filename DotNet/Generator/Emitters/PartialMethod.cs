using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Generator.Common;

namespace Generator
{
    internal class PartialMethod
    {
        private readonly MethodDeclarationSyntax syntax;

        public PartialMethod (MethodDeclarationSyntax syntax)
        {
            this.syntax = syntax;
        }

        public string EmitSource (Compilation compilation)
        {
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            var method = (IMethodSymbol)model.GetDeclaredSymbol(syntax);
            return $"{EmitSignature(method)} => {BuildInvoke(method, method!.Name, compilation)};";
        }

        private string EmitSignature (IMethodSymbol method)
        {
            var args = method.Parameters.Select(p => $"{BuildFullName(p.Type)} {p.Name}");
            var returnType = BuildFullName(method.ReturnType);
            return $"{syntax.Modifiers} {returnType} {syntax.Identifier} ({string.Join(", ", args)})";
        }
    }
}
