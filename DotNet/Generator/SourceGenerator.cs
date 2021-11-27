using System.Collections.Generic;
using System.IO;
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
            EmitJavaScript(context);
        }

        private string EmitFunctions (IEnumerable<FunctionClass> functionClasses, Compilation compilation)
        {
            var builder = new StringBuilder();
            foreach (var functionClass in functionClasses)
                builder.AppendLine(functionClass.EmitSource(compilation));
            return builder.ToString();
        }

        private void EmitJavaScript (GeneratorExecutionContext context)
        {
            File.WriteAllText("bin/codegen/init.txt", "// Init JS...");
            File.WriteAllText("bin/codegen/boot.txt", "// Boot JS...");
        }
    }
}
