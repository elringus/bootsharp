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
        using var cfg = ResolveConfiguration();
        using var inspection = InspectAssemblies(cfg.Preferences);
        GenerateBindings(cfg.Preferences, inspection);
        GenerateDeclarations(cfg.Preferences, inspection);
        GenerateResources(inspection);
        PatchModules();
        return true;
    }

    private Configuration ResolveConfiguration ()
    {
        var resolver = new ConfigurationResolver(EntryAssemblyName);
        return resolver.Resolve(InspectedDirectory);
    }

    private AssemblyInspection InspectAssemblies (Preferences prefs)
    {
        var inspector = new AssemblyInspector(prefs, EntryAssemblyName);
        var inspection = inspector.InspectInDirectory(InspectedDirectory);
        new InspectionReporter(Log).Report(inspection);
        return inspection;
    }

    private void GenerateBindings (Preferences prefs, AssemblyInspection inspection)
    {
        var generator = new BindingGenerator(prefs);
        var content = generator.Generate(inspection);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.js"), content);
    }

    private void GenerateDeclarations (Preferences prefs, AssemblyInspection inspection)
    {
        var generator = new DeclarationGenerator(prefs);
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
