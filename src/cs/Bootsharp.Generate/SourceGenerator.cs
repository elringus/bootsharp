using Microsoft.CodeAnalysis;

namespace Bootsharp.Generate;

[Generator(LanguageNames.CSharp)]
public sealed class SourceGenerator : IIncrementalGenerator
{
    public void Initialize (IncrementalGeneratorInitializationContext ctx) => ctx
        .RegisterSourceOutput(ctx.CompilationProvider, Compile);

    private static void Compile (SourceProductionContext ctx, Compilation cmp)
    {
        var receiver = VisitNodes(cmp);
        foreach (var @class in receiver.ImportClasses)
            ctx.AddSource($"{@class.Name}Imports.g", @class.EmitSource());
    }

    private static SyntaxReceiver VisitNodes (Compilation cmp)
    {
        var receiver = new SyntaxReceiver();
        foreach (var tree in cmp.SyntaxTrees)
            if (!tree.FilePath.EndsWith(".g.cs"))
                foreach (var node in tree.GetRoot().DescendantNodesAndSelf())
                    receiver.VisitNode(node, cmp);
        return receiver;
    }
}
