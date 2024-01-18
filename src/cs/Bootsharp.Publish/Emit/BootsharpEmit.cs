using Microsoft.Build.Framework;

namespace Bootsharp.Publish;

/// <summary>
/// First pass: emits C# sources to be picked by .NET's source generators.
/// </summary>
public sealed class BootsharpEmit : Microsoft.Build.Utilities.Task
{
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required string InterfacesFilePath { get; set; }
    [Required] public required string DependenciesFilePath { get; set; }
    [Required] public required string SerializerFilePath { get; set; }
    [Required] public required string InteropFilePath { get; set; }

    public override bool Execute ()
    {
        var prefs = ResolvePreferences();
        using var inspection = InspectAssemblies(prefs);
        GenerateInterfaces(inspection);
        GenerateDependencies(inspection);
        GenerateSerializer(inspection);
        GenerateInterop(inspection);
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

    private void GenerateInterfaces (AssemblyInspection inspection)
    {
        var generator = new InterfaceGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(InterfacesFilePath, content);
    }

    private void GenerateDependencies (AssemblyInspection inspection)
    {
        var generator = new DependencyGenerator(EntryAssemblyName);
        var content = generator.Generate(inspection);
        WriteGenerated(DependenciesFilePath, content);
    }

    private void GenerateSerializer (AssemblyInspection inspection)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(SerializerFilePath, content);
    }

    private void GenerateInterop (AssemblyInspection inspection)
    {
        var generator = new InteropGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(InteropFilePath, content);
    }

    private void WriteGenerated (string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
