using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class FunctionMethod
    {
        private readonly MethodDeclarationSyntax syntax;

        public FunctionMethod (MethodDeclarationSyntax syntax)
        {
            this.syntax = syntax;
        }

        public string EmitSource () => $"{EmitSignature()} => {EmitBody()};";

        private string EmitSignature () => $"{syntax.Modifiers} {syntax.ReturnType} {syntax.Identifier} {syntax.ParameterList}";

        private string EmitBody ()
        {
            var invokeMethod = GetInvokeMethod();
            var invokeParameters = GetInvokeParameters();
            return $"JS.{invokeMethod}({invokeParameters})";
        }

        private string GetInvokeMethod ()
        {
            var returnType = syntax.ReturnType.ToString();
            return
                returnType is "void" ? "Invoke" :
                returnType is "ValueTask" ? "InvokeAsync" :
                returnType.Contains("ValueTask") ? $"InvokeAsync<{returnType.Substring(10, returnType.Length - 11)}>" :
                $"Invoke<{returnType}>";
        }

        private string GetInvokeParameters ()
        {
            var args = $"\"{GetFunctionName()}\"";
            if (syntax.ParameterList.Parameters.Count == 0) return args;
            var ids = syntax.ParameterList.Parameters.Select(p => p.Identifier);
            args += $", {string.Join(",", ids)}";
            return args;
        }

        private string GetFunctionName ()
        {
            var name = syntax.Identifier.ToString();
            if (name.Length < 2) return name.ToLowerInvariant();
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
