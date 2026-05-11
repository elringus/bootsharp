namespace Bootsharp.Publish;

/// <summary>
/// Second pass: emits JS sources and type declarations.
/// </summary>
public sealed class GenerateJS : Microsoft.Build.Utilities.Task
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
        PreferencesResolver.Resolve(EntryAssemblyName, InspectedDirectory);
        using var spec = InspectSolution();
        var mds = new JSModules(spec.Types);
        GenerateImports(mds);
        GenerateModules(mds);
        GenerateSerializer(spec);
        GenerateInstances(spec, mds);
        GenerateDeclarations(spec, mds);
        GenerateResources(spec);
        PatchDotNet();
        return true;
    }

    private SolutionInspection InspectSolution ()
    {
        var inspector = new SolutionInspector();
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

    private void GenerateImports (JSModules mds)
    {
        var generator = new JSImportsGenerator();
        WriteGenerated("imports.g.mjs", generator.Generate(mds));
    }

    private void GenerateModules (JSModules mds)
    {
        var generator = new JSModuleGenerator(Debug);
        foreach (var module in mds.List)
            WriteGenerated($"{module.Path}.g.mjs", generator.Generate(module));
    }

    private void GenerateSerializer (SolutionInspection spec)
    {
        var generator = new JSSerializerGenerator();
        var serialized = spec.Types.OfType<SerializedMeta>().ToArray();
        WriteGenerated("serializer.g.mjs", generator.Generate(serialized));
    }

    private void GenerateInstances (SolutionInspection spec, JSModules mds)
    {
        var generator = new JSInstanceGenerator(Debug, mds);
        var instances = spec.Types.OfType<InstanceMeta>().ToArray();
        WriteGenerated("instances.g.mjs", generator.Generate(instances));
    }

    private void GenerateDeclarations (SolutionInspection spec, JSModules mds)
    {
        var generator = new DeclarationGenerator(spec, mds);
        foreach (var module in mds.List)
            WriteGenerated($"{module.Path}.g.d.mts", generator.Generate(module));
    }

    private void GenerateResources (SolutionInspection spec)
    {
        var generator = new ResourceGenerator(EntryAssemblyName, Debug, Globalization);
        var content = generator.Generate(BuildDirectory, DebugDirectory);
        WriteGenerated("resources.g.mjs", content);
    }

    private void PatchDotNet ()
    {
        var patcher = new DotNetPatcher(BuildDirectory);
        patcher.Patch();
    }

    private void WriteGenerated (string filename, string content)
    {
        var fullPath = Path.Combine(BuildDirectory, "generated", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
