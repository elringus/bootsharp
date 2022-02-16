using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator
{
    internal class FunctionMethod
    {
        private readonly MethodDeclarationSyntax syntax;

        public FunctionMethod (MethodDeclarationSyntax syntax)
        {
            this.syntax = syntax;
        }

        public string EmitSource (Compilation compilation)
        {
            var assembly = GetNamespace(compilation);
            return $"{EmitSignature()} => {EmitBody(assembly)};";
        }

        private string GetNamespace (Compilation compilation)
        {
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            var symbol = model.GetEnclosingSymbol(syntax.SpanStart)!;
            if (symbol.ContainingNamespace.IsGlobalNamespace) return "Bindings";
            return string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);
        }

        private string EmitSignature ()
        {
            return $"{syntax.Modifiers} {syntax.ReturnType} {syntax.Identifier} {syntax.ParameterList}";
        }

        private string EmitBody (string assembly)
        {
            var invokeMethod = GetInvokeMethod();
            var invokeParameters = GetInvokeParameters(assembly);
            return $"JS.{invokeMethod}({invokeParameters})";
        }

        private string GetInvokeMethod ()
        {
            var returnType = syntax.ReturnType.ToString();
            return
                returnType is "void" ? "Invoke" :
                returnType is "ValueTask" || returnType is "Task" ? "InvokeAsync" :
                returnType.Contains("Task") ? $"InvokeAsync<{returnType.Substring(10, returnType.Length - 11)}>" :
                $"Invoke<{returnType}>";
        }

        private string GetInvokeParameters (string assembly)
        {
            var args = $"\"dotnet.{assembly}.{syntax.Identifier.ToString()}\"";
            if (syntax.ParameterList.Parameters.Count == 0) return args;
            var ids = syntax.ParameterList.Parameters.Select(p => p.Identifier);
            args += $", {string.Join(", ", ids)}";
            return args;
        }
    }
}
