using Microsoft.CodeAnalysis;

namespace Bootsharp.Generate;

[Generator(LanguageNames.CSharp)]
public sealed class SourceGenerator : IIncrementalGenerator
{
    public void Initialize (IncrementalGeneratorInitializationContext context) => context
        .RegisterSourceOutput(context.CompilationProvider, Compile);

    private static void Compile (SourceProductionContext context, Compilation compilation)
    {
        var receiver = VisitNodes(compilation);
        foreach (var @class in receiver.FunctionClasses)
            context.AddSource($"{@class.Name}Functions.g", @class.EmitSource());
        foreach (var @class in receiver.EventClasses)
            context.AddSource($"{@class.Name}Events.g", @class.EmitSource());
    }

    private static SyntaxReceiver VisitNodes (Compilation compilation)
    {
        var receiver = new SyntaxReceiver();
        foreach (var tree in compilation.SyntaxTrees)
            if (!tree.FilePath.EndsWith(".g.cs"))
                foreach (var node in tree.GetRoot().DescendantNodesAndSelf())
                    receiver.VisitNode(node, compilation);
        return receiver;
    }
}
