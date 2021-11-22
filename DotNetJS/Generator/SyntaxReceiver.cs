using System.Text;
using Microsoft.CodeAnalysis;

namespace DotNetJS.Generator
{
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        private readonly StringBuilder sourceBuilder = new();
        private readonly FunctionBuilder functionBuilder = new();

        public void OnVisitSyntaxNode (SyntaxNode syntaxNode)
        {
            if (functionBuilder.CanBuildFrom(syntaxNode))
                functionBuilder.Build(syntaxNode, sourceBuilder);
        }

        public string BuildSource () => sourceBuilder.ToString();
    }
}
