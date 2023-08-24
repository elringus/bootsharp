using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Builder;

public sealed class BuildBootsharp : Task
{
    [Required] public required string BuildDir { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required bool EmbedBinaries { get; set; }

    public override bool Execute ()
    {
        var sources = GenerateSources();
        PublishLibrary(sources.Library);
        PublishDeclaration(sources.Declaration);
        PublishSourceMap();
        if (!EmbedBinaries) PublishBinaries(sources.Assemblies);
        return true;
    }

    private (IReadOnlyList<Assembly> Assemblies,
        string Library, string Declaration) GenerateSources ()
    {
        var builder = CreateNamespaceBuilder();
        using var inspector = InspectAssemblies(builder);
        return (inspector.Assemblies,
            GenerateLibrary(inspector, builder),
            GenerateDeclaration(inspector, builder));
    }

    private void PublishLibrary (string source)
    {
        var path = Path.Combine(PublishDir, "bootsharp.js");
        File.WriteAllText(path, source);
        Log.LogMessage(MessageImportance.High, $"Bootsharp ES module is published at {path}.");
    }

    private void PublishDeclaration (string source)
    {
        var file = Path.Combine(PublishDir, "bootsharp.d.ts");
        File.WriteAllText(file, source);
    }

    private void PublishSourceMap ()
    {
        var source = Path.Combine(JSLibraryDir, "bootsharp.js.map");
        var destination = Path.Combine(PublishDir, "bootsharp.js.map");
        File.Copy(source, destination, true);
    }

    private void PublishBinaries (IEnumerable<Assembly> assemblies)
    {
        var wasmPath = Path.Combine(PublishDir, Path.GetFileName(WasmFile));
        var wasmBytes = File.ReadAllBytes(WasmFile);
        File.WriteAllBytes(wasmPath, wasmBytes);
        Directory.CreateDirectory(Path.Combine(PublishDir, "managed"));
        foreach (var assembly in assemblies)
            File.WriteAllBytes(Path.Combine(PublishDir, $"managed/{assembly.Name}"), assembly.Bytes);
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(BuildDir, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspector InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        inspector.InspectInDirectory(BuildDir);
        inspector.Report(Log);
        return inspector;
    }

    private string GenerateLibrary (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var runtimeJS = File.ReadAllText(Path.Combine(JSLibraryDir, "bootsharp.js"));
        var generator = new LibraryGenerator(spaceBuilder, inspector, runtimeJS, EntryAssemblyName);
        return EmbedBinaries
            ? generator.GenerateEmbedded(File.ReadAllBytes(WasmFile))
            : generator.GenerateSideLoad(Path.GetFileName(WasmFile));
    }

    private string GenerateDeclaration (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new DeclarationGenerator(spaceBuilder, EmbedBinaries);
        generator.LoadDeclarations(JSLibraryDir);
        return generator.Generate(inspector);
    }
}
