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
            var receiver = context.SyntaxContextReceiver as SyntaxReceiver;
            if (receiver is null) return;

            for (int i = 0; i < receiver.FunctionClasses.Count; i++)
                context.AddSource($"Functions{i}", receiver.FunctionClasses[i].EmitSource(context.Compilation));
            for (int i = 0; i < receiver.EventClasses.Count; i++)
                context.AddSource($"Events{i}", receiver.EventClasses[i].EmitSource(context.Compilation));
        }
    }
}
