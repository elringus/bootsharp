namespace Bootsharp.Publish;

/// <summary>
/// Second pass: emits JS bindings and type declarations, bundles ES module.
/// </summary>
public sealed class BootsharpPack : Microsoft.Build.Utilities.Task
{
    public required string BuildDirectory { get; set; }
    public required string InspectedDirectory { get; set; }
    public required string EntryAssemblyName { get; set; }
    public required bool TrimmingEnabled { get; set; }
    public required bool EmbedBinaries { get; set; }
    public required bool Threading { get; set; }

    public override bool Execute ()
    {
        var prefs = ResolvePreferences();
        using var inspection = InspectAssemblies(prefs);
        GenerateBindings(prefs, inspection);
        GenerateDeclarations(prefs, inspection);
        GenerateResources(inspection);
        PatchModules();
        return true;
    }

    private Preferences ResolvePreferences ()
    {
        var resolver = new PreferencesResolver(EntryAssemblyName);
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
