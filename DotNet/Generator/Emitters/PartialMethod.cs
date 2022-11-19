using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Generator.Common;

namespace Generator
{
    // TODO: Refactor to re-use Common.BuildInvoke (resolving symbols from compilation not working in JavaScript/test/csproj).

    internal class PartialMethod
    {
        private readonly MethodDeclarationSyntax syntax;
        private readonly bool @event;
        private readonly string className;

        public PartialMethod (MethodDeclarationSyntax syntax, string className, bool @event)
        {
            this.syntax = syntax;
            this.className = className;
            this.@event = @event;
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
            return ConvertNamespace(space, className, symbol.ContainingAssembly);
        }

        private string EmitSignature ()
        {
            return $"{syntax.Modifiers} {syntax.ReturnType} {syntax.Identifier} {syntax.ParameterList}";
        }

        private string EmitBody (string @namespace)
        {
            var invokeMethod = GetInvokeMethod();
            var invokeParameters = GetInvokeParameters(@namespace);
            var convertTask = syntax.ReturnType.ToString().StartsWith("Task") ? ".AsTask()" : "";
            return $"JS.{invokeMethod}({invokeParameters}){convertTask}";
        }

        private string GetInvokeMethod ()
        {
            var returnType = syntax.ReturnType.ToString();
            return
                returnType is "void" ? "Invoke" :
                returnType is "ValueTask" || returnType is "Task" ? "InvokeAsync" :
                returnType.StartsWith("Task<") ? $"InvokeAsync<{returnType.Substring(5, returnType.Length - 6)}>" :
                returnType.StartsWith("ValueTask<") ? $"InvokeAsync<{returnType.Substring(10, returnType.Length - 11)}>" :
                $"Invoke<{returnType}>";
        }

        private string GetInvokeParameters (string assembly)
        {
            var args = $"\"dotnet.{assembly}.{syntax.Identifier}{(@event ? ".broadcast" : "")}\"";
            if (syntax.ParameterList.Parameters.Count == 0) return args;
            var ids = syntax.ParameterList.Parameters.Select(p => p.Identifier);
            args += $", new object[] {{ {string.Join(", ", ids)} }}";
            return args;
        }
    }
}
