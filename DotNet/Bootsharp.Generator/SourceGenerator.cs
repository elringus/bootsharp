using Microsoft.CodeAnalysis;

namespace Bootsharp.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SourceGenerator : IIncrementalGenerator
{
    public void Initialize (IncrementalGeneratorInitializationContext context) => context
        .RegisterSourceOutput(context.CompilationProvider, Compile);

    private static void Compile (SourceProductionContext context, Compilation compilation)
    {
        CompileGlobal(context, compilation);
        CompilePartial(context, compilation);
    }

    private static void CompileGlobal (SourceProductionContext context, Compilation compilation)
    {
        foreach (var type in ExportType.Resolve(compilation))
            context.AddSource($"{type.Name}Export.g", type.EmitSource());
        foreach (var type in ImportType.Resolve(compilation))
            context.AddSource($"{type.Name}Import.g", type.EmitSource());
    }

    private static void CompilePartial (SourceProductionContext context, Compilation compilation)
    {
        var receiver = VisitNodes(compilation);
        foreach (var @class in receiver.InvokableClasses)
            context.AddSource($"{@class.Name}Invokable.g", @class.EmitSource());
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
