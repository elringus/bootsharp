using Microsoft.Build.Framework;

namespace Bootsharp.Publish;

/// <summary>
/// First pass: emits code to be picked by .NET's source generators.
/// </summary>
public sealed class BootsharpEmit : Microsoft.Build.Utilities.Task
{
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required string InvokablesFilePath { get; set; }
    [Required] public required string FunctionsFilePath { get; set; }
    [Required] public required string EventsFilePath { get; set; }
    [Required] public required string ExportsFilePath { get; set; }
    [Required] public required string ImportsFilePath { get; set; }
    [Required] public required string InteropExportsFilePath { get; set; }
    [Required] public required string InteropImportsFilePath { get; set; }
    [Required] public required string SerializerFilePath { get; set; }

    public override bool Execute ()
    {
        var spaceBuilder = CreateNamespaceBuilder();
        using var inspection = InspectAssemblies(spaceBuilder);
        GenerateInvokables(inspection);
        GenerateFunctions(inspection);
        GenerateEvents(inspection);
        GenerateExports(inspection);
        GenerateImports(inspection);
        GenerateInteropExports(inspection);
        GenerateInteropImports(inspection);
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

    private void GenerateInvokables (AssemblyInspection inspection) { }

    private void GenerateFunctions (AssemblyInspection inspection) { }

    private void GenerateEvents (AssemblyInspection inspection) { }

    private void GenerateExports (AssemblyInspection inspection) { }

    private void GenerateImports (AssemblyInspection inspection) { }

    private void GenerateInteropExports (AssemblyInspection inspection)
    {
        var generator = new InteropExportGenerator();
        var content = generator.Generate(inspection);
        Directory.CreateDirectory(Path.GetDirectoryName(InteropExportsFilePath)!);
        File.WriteAllText(InteropExportsFilePath, content);
    }

    private void GenerateInteropImports (AssemblyInspection inspection)
    {
        var generator = new InteropImportGenerator(EntryAssemblyName);
        var content = generator.Generate(inspection);
        Directory.CreateDirectory(Path.GetDirectoryName(InteropImportsFilePath)!);
        File.WriteAllText(InteropImportsFilePath, content);
    }

    private void GenerateSerializer (AssemblyInspection inspection)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(inspection);
        Directory.CreateDirectory(Path.GetDirectoryName(SerializerFilePath)!);
        File.WriteAllText(SerializerFilePath, content);
    }
}
