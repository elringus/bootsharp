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
            var receiver = (SyntaxReceiver)context.SyntaxReceiver!;
            var source = receiver.BuildSource();
            context.AddSource("DotNetJS.Generated", source);
        }
    }
}
