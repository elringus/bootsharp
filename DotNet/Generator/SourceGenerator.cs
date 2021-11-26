using System.Collections.Generic;
using System.Linq;
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
                context.AddSource("Functions", EmitFunctions(receiver.FunctionClasses));
        }

        private string EmitFunctions (IEnumerable<FunctionClass> classes)
        {
            var sources = classes.Select(c => c.EmitSource());
            return string.Join("\n", sources);
        }
    }
}
