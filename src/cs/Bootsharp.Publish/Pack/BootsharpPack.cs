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
    public required bool LLVM { get; set; }

    public override bool Execute ()
    {
        var prefs = ResolvePreferences();
        using var inspection = InspectSolution(prefs);
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

    private SolutionInspection InspectSolution (Preferences prefs)
    {
        var inspector = new SolutionInspector(prefs, EntryAssemblyName);
        var inspection = inspector.Inspect(InspectedDirectory, GetFiles());
        new InspectionReporter(Log).Report(inspection);
        return inspection;

        IEnumerable<string> GetFiles ()
        {
            if (LLVM) return Directory.GetFiles(InspectedDirectory, "*.dll");
            // Assemblies in publish dir are trimmed and don't contain some data (eg, method arg names).
            // While the inspected dir contains extra assemblies we don't need in build. Hence the filtering.
            var included = Directory.GetFiles(BuildDirectory, "*.wasm").Select(Path.GetFileNameWithoutExtension).ToHashSet();
            return Directory.GetFiles(InspectedDirectory, "*.dll").Where(p => included.Contains(Path.GetFileNameWithoutExtension(p)));
        }
    }

    private void GenerateBindings (Preferences prefs, SolutionInspection inspection)
    {
        var generator = new BindingGenerator(prefs);
        var content = generator.Generate(inspection);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.js"), content);
    }

    private void GenerateDeclarations (Preferences prefs, SolutionInspection inspection)
    {
        var generator = new DeclarationGenerator(prefs);
        var content = generator.Generate(inspection);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.d.ts"), content);
    }

    private void GenerateResources (SolutionInspection inspection)
    {
        var generator = new ResourceGenerator(EntryAssemblyName, EmbedBinaries);
        var content = generator.Generate(BuildDirectory);
        File.WriteAllText(Path.Combine(BuildDirectory, "resources.g.js"), content);
    }

    private void PatchModules ()
    {
        var patcher = new ModulePatcher(BuildDirectory, Threading, EmbedBinaries, TrimmingEnabled);
        patcher.Patch();
    }
}
