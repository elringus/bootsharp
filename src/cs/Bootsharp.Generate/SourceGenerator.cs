using Microsoft.CodeAnalysis;

namespace Bootsharp.Generate;

[Generator(LanguageNames.CSharp)]
public sealed class SourceGenerator : IIncrementalGenerator
{
    public void Initialize (IncrementalGeneratorInitializationContext ctx)
    {
        var members = ctx.SyntaxProvider.ForAttributeWithMetadataName(
                "Bootsharp.ImportAttribute",
                static (_, _) => true,
                ImportMember.Resolve)
            .Where(static m => m is not null)
            .WithTrackingName("members");
        var classes = members.Collect()
            .SelectMany(static (flat, _) => ImportClass.Group(flat))
            .WithTrackingName("classes");
        ctx.RegisterSourceOutput(classes,
            static (ctx, cls) => ctx.AddSource($"{cls.Name}Imports.g", cls.Emit()));
    }
}
