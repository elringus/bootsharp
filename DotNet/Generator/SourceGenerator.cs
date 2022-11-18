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
            AddGlobal(context);
            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
                AddPartial(context, receiver);
        }

        private static void AddGlobal (GeneratorExecutionContext context)
        {
            foreach (var type in ExportType.Resolve(context.Compilation.Assembly))
                context.AddSource($"{type.Name}Export.g", ExportType.EmitSource(type, context.Compilation));
            foreach (var type in ImportType.Resolve(context.Compilation.Assembly))
                context.AddSource($"{type.Name}Import.g", ImportType.EmitSource(type, context.Compilation));
        }

        private static void AddPartial (GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            foreach (var @class in receiver.FunctionClasses)
                context.AddSource($"{@class.Name}Functions.g", @class.EmitSource(context.Compilation));
            foreach (var @class in receiver.EventClasses)
                context.AddSource($"{@class.Name}Events.g", @class.EmitSource(context.Compilation));
        }
    }
}
