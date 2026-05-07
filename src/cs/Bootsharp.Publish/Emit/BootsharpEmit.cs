namespace Bootsharp.Publish;

/// <summary>
/// First pass: emits C# sources to be picked by .NET's source generators.
/// </summary>
public sealed class BootsharpEmit : Microsoft.Build.Utilities.Task
{
    public required string InspectedDirectory { get; set; }
    public required string EntryAssemblyName { get; set; }
    public required string SerializerFilePath { get; set; }
    public required string InstancesFilePath { get; set; }
    public required string ModulesFilePath { get; set; }
    public required string InteropFilePath { get; set; }

    public override bool Execute ()
    {
        var prefs = ResolvePreferences();
        using var inspection = InspectSolution(prefs);
        GenerateSerializer(inspection);
        GenerateInstances(inspection);
        GenerateModules(inspection);
        GenerateInterop(inspection);
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
        var inspected = Directory.GetFiles(InspectedDirectory, "*.dll").Order();
        var inspection = inspector.Inspect(InspectedDirectory, inspected);
        new InspectionReporter(Log).Report(inspection);
        return inspection;
    }

    private void GenerateSerializer (SolutionInspection spec)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(spec);
        WriteGenerated(SerializerFilePath, content);
    }

    private void GenerateInstances (SolutionInspection spec)
    {
        var generator = new InstanceGenerator();
        var content = generator.Generate(spec);
        WriteGenerated(InstancesFilePath, content);
    }

    private void GenerateModules (SolutionInspection spec)
    {
        var generator = new ModuleGenerator();
        var content = generator.Generate(spec);
        WriteGenerated(ModulesFilePath, content);
    }

    private void GenerateInterop (SolutionInspection spec)
    {
        var generator = new InteropGenerator();
        var content = generator.Generate(spec);
        WriteGenerated(InteropFilePath, content);
    }

    private void WriteGenerated (string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
