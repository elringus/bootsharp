using Microsoft.Build.Framework;

namespace Bootsharp.Publish;

/// <summary>
/// First pass: emits code to be picked up by .NET's source generators.
/// </summary>
public sealed class BootsharpEmit : Microsoft.Build.Utilities.Task
{
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required string ExportsFilePath { get; set; }
    [Required] public required string ImportsFilePath { get; set; }
    [Required] public required string SerializerFilePath { get; set; }

    public override bool Execute ()
    {
        var spaceBuilder = CreateNamespaceBuilder();
        using var inspection = InspectAssemblies(spaceBuilder);
        GenerateExports(inspection);
        GenerateImports(inspection);
        GenerateSerializer(inspection);
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

    private void GenerateExports (AssemblyInspection inspection)
    {
        var generator = new ExportGenerator();
        var content = generator.Generate(inspection);
        Directory.CreateDirectory(Path.GetDirectoryName(ExportsFilePath)!);
        File.WriteAllText(ExportsFilePath, content);
    }

    private void GenerateImports (AssemblyInspection inspection)
    {
        var generator = new ImportGenerator(EntryAssemblyName);
        var content = generator.Generate(inspection);
        Directory.CreateDirectory(Path.GetDirectoryName(ImportsFilePath)!);
        File.WriteAllText(ImportsFilePath, content);
    }

    private void GenerateSerializer (AssemblyInspection inspection)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(inspection);
        Directory.CreateDirectory(Path.GetDirectoryName(SerializerFilePath)!);
        File.WriteAllText(SerializerFilePath, content);
    }
}
