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
        using var inspector = InspectAssemblies(spaceBuilder);
        GenerateExports(inspector);
        GenerateImports(inspector);
        GenerateSerializer(inspector);
        return true;
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(InspectedDirectory, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspector InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        inspector.InspectInDirectory(InspectedDirectory);
        inspector.Report(Log);
        return inspector;
    }

    private void GenerateExports (AssemblyInspector inspector)
    {
        var generator = new ExportGenerator();
        var content = generator.Generate(inspector);
        Directory.CreateDirectory(Path.GetDirectoryName(ExportsFilePath)!);
        File.WriteAllText(ExportsFilePath, content);
    }

    private void GenerateImports (AssemblyInspector inspector)
    {
        var generator = new ImportGenerator(EntryAssemblyName);
        var content = generator.Generate(inspector);
        Directory.CreateDirectory(Path.GetDirectoryName(ImportsFilePath)!);
        File.WriteAllText(ImportsFilePath, content);
    }

    private void GenerateSerializer (AssemblyInspector inspector)
    {
        var generator = new SerializerGenerator();
        var content = generator.Generate(inspector);
        Directory.CreateDirectory(Path.GetDirectoryName(SerializerFilePath)!);
        File.WriteAllText(SerializerFilePath, content);
    }
}
