using Microsoft.Build.Framework;

namespace Bootsharp.Publish;

/// <summary>
/// Second pass: emits JS bindings and type declarations, bundles ES module.
/// </summary>
public sealed class BootsharpPack : Microsoft.Build.Utilities.Task
{
    [Required] public required string BuildDirectory { get; set; }
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required bool TrimmingEnabled { get; set; }
    [Required] public required bool EmbedBinaries { get; set; }
    [Required] public required bool Threading { get; set; }

    public override bool Execute ()
    {
        var spaceBuilder = CreateNamespaceBuilder();
        using var inspection = InspectAssemblies(spaceBuilder);
        GenerateBindings(inspection, spaceBuilder);
        GenerateDeclarations(inspection, spaceBuilder);
        GenerateResources(inspection);
        PatchModules();
        return true;
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(InspectedDirectory, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspection InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        var inspection = inspector.InspectInDirectory(InspectedDirectory);
        new InspectionReporter(Log).Report(inspection);
        return inspection;
    }

    private void GenerateBindings (AssemblyInspection inspection, NamespaceBuilder spaceBuilder)
    {
        var generator = new BindingGenerator(spaceBuilder);
        var content = generator.Generate(inspection);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.js"), content);
    }

    private void GenerateDeclarations (AssemblyInspection inspection, NamespaceBuilder spaceBuilder)
    {
        var generator = new DeclarationGenerator(spaceBuilder);
        var content = generator.Generate(inspection);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.d.ts"), content);
    }

    private void GenerateResources (AssemblyInspection inspection)
    {
        var generator = new ResourceGenerator(EntryAssemblyName, BuildDirectory, EmbedBinaries);
        var content = generator.Generate(inspection);
        File.WriteAllText(Path.Combine(BuildDirectory, "resources.g.js"), content);
    }

    private void PatchModules ()
    {
        var patcher = new ModulePatcher(BuildDirectory, Threading, EmbedBinaries, TrimmingEnabled);
        patcher.Patch();
    }
}
