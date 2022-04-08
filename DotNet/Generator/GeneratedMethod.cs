using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator
{
    internal class GeneratedMethod
    {
        private readonly MethodDeclarationSyntax syntax;
        private readonly NamespaceConverter spaceConverter;
        private readonly bool @event;

        public GeneratedMethod (MethodDeclarationSyntax syntax, bool @event)
        {
            this.syntax = syntax;
            this.@event = @event;
            spaceConverter = new NamespaceConverter();
        }

        public string EmitSource (Compilation compilation)
        {
            var @namespace = GetNamespace(compilation);
            return $"{EmitSignature()} => {EmitBody(@namespace)};";
        }

        private string GetNamespace (Compilation compilation)
        {
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            var symbol = model.GetEnclosingSymbol(syntax.SpanStart)!;
            var space = symbol.ContainingNamespace.IsGlobalNamespace ? "Bindings"
                : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);
            return spaceConverter.Convert(space, symbol.ContainingAssembly);
        }

        private string EmitSignature ()
        {
            return $"{syntax.Modifiers} {syntax.ReturnType} {syntax.Identifier} {syntax.ParameterList}";
        }

        private string EmitBody (string @namespace)
        {
            var invokeMethod = GetInvokeMethod();
            var invokeParameters = GetInvokeParameters(@namespace);
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
            var args = $"\"dotnet.{assembly}.{syntax.Identifier}{(@event ? ".broadcast" : "")}\"";
            if (syntax.ParameterList.Parameters.Count == 0) return args;
            var ids = syntax.ParameterList.Parameters.Select(p => p.Identifier);
            args += $", {string.Join(", ", ids)}";
            return args;
        }
    }
}
