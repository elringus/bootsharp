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
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

            context.AddSource("Log", Log.Flush());
            context.AddSource("Emitted", receiver.GetEmitted());
        }
    }
}
