namespace Bootsharp.Publish;

/// <summary>
/// First pass: emits C# sources to be picked by .NET's source generators.
/// </summary>
public sealed class GenerateCS : Microsoft.Build.Utilities.Task
{
    public required string InspectedDirectory { get; set; }
    public required string EntryAssemblyName { get; set; }
    public required string SerializerFilePath { get; set; }
    public required string InstancesFilePath { get; set; }
    public required string ModulesFilePath { get; set; }
    public required string InteropFilePath { get; set; }

    public override bool Execute ()
    {
        PreferencesResolver.Resolve(EntryAssemblyName, InspectedDirectory);
        using var spec = InspectSolution();
        GenerateSerializer(spec);
        GenerateInstances(spec);
        GenerateModules(spec);
        GenerateInterop(spec);
        return true;
    }

    private SolutionInspection InspectSolution ()
    {
        var inspector = new SolutionInspector();
        var inspected = Directory.GetFiles(InspectedDirectory, "*.dll").Order();
        var inspection = inspector.Inspect(InspectedDirectory, inspected);
        new InspectionReporter(Log).Report(inspection);
        return inspection;
    }

    private void GenerateSerializer (SolutionInspection spec)
    {
        var generator = new SerializerGenerator();
        var serialized = spec.Types.OfType<SerializedMeta>().ToArray();
        var content = generator.Generate(serialized);
        WriteGenerated(SerializerFilePath, content);
    }

    private void GenerateInstances (SolutionInspection spec)
    {
        var generator = new InstanceGenerator();
        var instanced = spec.Types.OfType<InstanceMeta>().ToArray();
        var content = generator.Generate(instanced);
        WriteGenerated(InstancesFilePath, content);
    }

    private void GenerateModules (SolutionInspection spec)
    {
        var generator = new ModuleGenerator();
        var mds = spec.Types.OfType<ModuleMeta>().ToArray();
        var content = generator.Generate(mds);
        WriteGenerated(ModulesFilePath, content);
    }

    private void GenerateInterop (SolutionInspection spec)
    {
        var generator = new InteropGenerator();
        var surfaces = spec.Types.OfType<SurfaceMeta>().ToArray();
        var content = generator.Generate(surfaces);
        WriteGenerated(InteropFilePath, content);
    }

    private void WriteGenerated (string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
