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

    private void GenerateInvokables (AssemblyInspection inspection)
    {
        var generator = new InvokableGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(InvokablesFilePath, content);
    }

    private void GenerateFunctions (AssemblyInspection inspection)
    {
        var generator = new FunctionGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(FunctionsFilePath, content);
    }

    private void GenerateEvents (AssemblyInspection inspection)
    {
        var generator = new EventGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(EventsFilePath, content);
    }

    private void GenerateExports (AssemblyInspection inspection)
    {
        var generator = new ExportGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(ExportsFilePath, content);
    }

    private void GenerateImports (AssemblyInspection inspection)
    {
        var generator = new ImportGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(ImportsFilePath, content);
    }

    private void GenerateInteropExports (AssemblyInspection inspection)
    {
        var generator = new InteropExportGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(InteropExportsFilePath, content);
    }

    private void GenerateInteropImports (AssemblyInspection inspection)
    {
        var generator = new InteropImportGenerator(EntryAssemblyName);
        var content = generator.Generate(inspection);
        WriteGenerated(InteropImportsFilePath, content);
    }

    private void GenerateSerializer (AssemblyInspection inspection)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(SerializerFilePath, content);
    }

    private static void WriteGenerated (string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
