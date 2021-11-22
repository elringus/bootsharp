using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetJS.Generator
{
    internal class FunctionBuilder
    {
        public bool CanBuildFrom (SyntaxNode node)
        {
            if (node is not MethodDeclarationSyntax methodNode) return false;
            if (!methodNode.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains(nameof(JSFunctionAttribute)))) return false;
            return true;
        }
        
        public void Build (SyntaxNode node, StringBuilder builder)
        {
            
            string source = $@"
using System;

namespace {node!.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {mainMethod.ContainingType.Name}
    {{
        static partial void HelloFrom(string name)
        {{
            Console.WriteLine($""Generator says: Hi from '{{name}}'"");
        }}
    }}
}}
";
            return source;
        }
    }
}
