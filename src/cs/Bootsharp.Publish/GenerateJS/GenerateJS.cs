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
    public required bool Debug { get; set; }

    public override bool Execute ()
    {
        PreferencesResolver.Resolve(EntryAssemblyName, InspectedDirectory);
        using var spec = InspectSolution();
        var mds = new JSModules(spec.Types);
        GenerateImports(mds, spec);
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
        var files = Directory.GetFiles(InspectedDirectory, "*.dll").Order();
        var inspection = inspector.Inspect(InspectedDirectory, files);
        new InspectionReporter(Log).Report(inspection);
        return inspection;
    }

    private void GenerateImports (JSModules mds, SolutionInspection spec)
    {
        var generator = new JSImportsGenerator();
        var bindings = generator.Collect(mds, spec.Types.OfType<SurfaceMeta>().ToArray());
        WriteGenerated("imports.g.mjs", generator.GenerateBinding(mds, bindings));
    }

    private void GenerateModules (JSModules mds)
    {
        var generator = new JSModuleGenerator(Debug);
        foreach (var module in mds.List)
            WriteGenerated($"modules/{module.Path}.g.mjs", generator.Generate(module));
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
            WriteGenerated($"modules/{module.Path}.g.d.mts", generator.Generate(module));
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
