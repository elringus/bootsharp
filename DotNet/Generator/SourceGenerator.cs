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
            // https://stackoverflow.com/a/68733955/1202251

            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
                AddSources(context, receiver);
        }

        private void AddSources (GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            for (int i = 0; i < receiver.FunctionClasses.Count; i++)
                context.AddSource($"Functions{i}.g", receiver.FunctionClasses[i].EmitSource(context.Compilation));
            for (int i = 0; i < receiver.EventClasses.Count; i++)
                context.AddSource($"Events{i}.g", receiver.EventClasses[i].EmitSource(context.Compilation));
        }
    }
}
