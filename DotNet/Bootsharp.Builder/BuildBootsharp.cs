using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Builder;

public sealed class BuildBootsharp : Task
{
    [Required] public required string BuildDirectory { get; set; }
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required bool EmbedBinaries { get; set; }

    public override bool Execute ()
    {
        var builder = CreateNamespaceBuilder();
        using var inspector = InspectAssemblies(builder);
        GenerateBindings(inspector, builder);
        GenerateDeclarations(inspector, builder);
        GenerateResources(inspector);
        return true;
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(InspectedDirectory, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspector InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        inspector.InspectInDirectory(InspectedDirectory);
        inspector.Report(Log);
        return inspector;
    }

    private void GenerateBindings (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new BindingGenerator(spaceBuilder);
        var content = generator.Generate(inspector);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.js"), content);
    }

    private void GenerateDeclarations (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new DeclarationGenerator(spaceBuilder);
        var content = generator.Generate(inspector);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.d.ts"), content);
    }

    private void GenerateResources (AssemblyInspector inspector)
    {
        var generator = new ResourceGenerator(EntryAssemblyName, BuildDirectory, EmbedBinaries);
        var content = generator.Generate(inspector);
        File.WriteAllText(Path.Combine(BuildDirectory, "resources.g.js"), content);
    }
}
