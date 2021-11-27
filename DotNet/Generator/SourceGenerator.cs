using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DotNetJS.Generator
{
    [Generator]
    internal class SourceGenerator : ISourceGenerator
    {
        public void Initialize (GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute (GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
                context.AddSource("Functions", EmitFunctions(receiver.FunctionClasses, context.Compilation));
        }

        private string EmitFunctions (IEnumerable<FunctionClass> functionClasses, Compilation compilation)
        {
            var builder = new StringBuilder();
            foreach (var functionClass in functionClasses)
                builder.AppendLine(functionClass.EmitSource(compilation));
            return builder.ToString();
        }
    }
}
