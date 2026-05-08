namespace Bootsharp.Publish;

/// <summary>
/// Second pass: emits JS bindings and type declarations, bundles ES module.
/// </summary>
public sealed class BootsharpPack : Microsoft.Build.Utilities.Task
{
    public required string BuildDirectory { get; set; }
    public required string DebugDirectory { get; set; }
    public required string InspectedDirectory { get; set; }
    public required string EntryAssemblyName { get; set; }
    public required bool Globalization { get; set; }
    public required bool LLVM { get; set; }
    public required bool Debug { get; set; }

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
        var inspector = new SolutionInspector(prefs);
        var inspection = inspector.Inspect(InspectedDirectory, GetFiles());
        new InspectionReporter(Log).Report(inspection);
        return inspection;

        IEnumerable<string> GetFiles ()
        {
            if (LLVM) return Directory.GetFiles(InspectedDirectory, "*.dll").Order();
            // Assemblies in publish dir are trimmed and don't contain some data (eg, method arg names).
            // While the inspected dir contains extra assemblies we don't need in build. Hence the filtering.
            var included = Directory.GetFiles(BuildDirectory, "*.wasm")
                .Select(Path.GetFileNameWithoutExtension).ToHashSet();
            return Directory.GetFiles(InspectedDirectory, "*.dll").Order()
                .Where(p => included.Contains(Path.GetFileNameWithoutExtension(p)));
        }
    }

    private void GenerateBindings (Preferences prefs, SolutionInspection spec)
    {
        var generator = new BindingGenerator(prefs, Debug);
        var content = generator.Generate(spec);
        WriteGenerated("bindings.g.mjs", content);
    }

    private void GenerateDeclarations (Preferences prefs, SolutionInspection spec)
    {
        var generator = new DeclarationGenerator(prefs);
        var content = generator.Generate(spec);
        WriteGenerated("bindings.g.d.mts", content);
    }

    private void GenerateResources (SolutionInspection spec)
    {
        var generator = new ResourceGenerator(EntryAssemblyName, Debug, Globalization);
        var content = generator.Generate(BuildDirectory, DebugDirectory);
        WriteGenerated("resources.g.mjs", content);
    }

    private void PatchModules ()
    {
        var patcher = new ModulePatcher(BuildDirectory);
        patcher.Patch();
    }

    private void WriteGenerated (string filename, string content)
    {
        var dir = Path.Combine(BuildDirectory, "generated");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, filename), content);
    }
}
