using Microsoft.Build.Framework;

namespace Bootsharp.Publish;

/// <summary>
/// First pass: emits code to be picked by .NET's source generators.
/// </summary>
public sealed class BootsharpEmit : Microsoft.Build.Utilities.Task
{
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required string ExportsFilePath { get; set; }
    [Required] public required string ImportsFilePath { get; set; }
    [Required] public required string InterceptorsFilePath { get; set; }
    [Required] public required string DependenciesFilePath { get; set; }
    [Required] public required string SerializerFilePath { get; set; }
    [Required] public required string InteropFilePath { get; set; }

    public override bool Execute ()
    {
        var spaceBuilder = CreateNamespaceBuilder();
        using var inspection = InspectAssemblies(spaceBuilder);
        GenerateExports(inspection);
        GenerateImports(inspection);
        GenerateInterceptors(inspection);
        GenerateDependencies(inspection);
        GenerateSerializer(inspection);
        GenerateInterop(inspection);
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
        WriteGenerated(ExportsFilePath, content);
    }

    private void GenerateImports (AssemblyInspection inspection)
    {
        var generator = new ImportGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(ImportsFilePath, content);
    }

    private void GenerateInterceptors (AssemblyInspection inspection)
    {
        var generator = new InterceptorGenerator();
        var content = generator.Generate(inspection);
        WriteGenerated(InterceptorsFilePath, content);
    }

    private void GenerateDependencies (AssemblyInspection inspection)
    {
        var generator = new DependenciesGenerator();
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
