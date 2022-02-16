using Microsoft.CodeAnalysis;

namespace Generator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize (GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute (GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
                for (int i = 0; i < receiver.FunctionClasses.Count; i++)
                    context.AddSource($"Functions{i}",
                        receiver.FunctionClasses[i].EmitSource(context.Compilation));
        }
    }
}
