using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Packer;

public class PublishDotNetJS : Task
{
    [Required] public string PublishDir { get; set; } = null!;
    [Required] public string BlazorOutDir { get; set; } = null!;
    [Required] public string JSDir { get; set; } = null!;
    [Required] public string WasmFile { get; set; } = null!;
    [Required] public string EntryAssemblyName { get; set; } = null!;
    public bool EmbedBinaries { get; set; } = true;
    public bool Clean { get; set; } = true;

    public override bool Execute ()
    {
        var sources = GenerateSources();
        if (Clean) CleanPublishDirectory();
        PublishLibrary(sources.Library);
        PublishDeclaration(sources.Declaration);
        PublishSourceMap();
        if (!EmbedBinaries) PublishBinaries(sources.WasmBytes, sources.Assemblies);
        return true;
    }

    private (byte[] WasmBytes, IReadOnlyList<Assembly> Assemblies,
        string Library, string Declaration) GenerateSources ()
    {
        var builder = CreateNamespaceBuilder();
        var wasmBytes = File.ReadAllBytes(WasmFile);
        using var inspector = InspectAssemblies(builder);
        return (wasmBytes, inspector.Assemblies,
            GenerateLibrary(wasmBytes, inspector, builder),
            GenerateDeclaration(inspector, builder));
    }

    private void CleanPublishDirectory ()
    {
        Directory.Delete(PublishDir, true);
        Directory.CreateDirectory(PublishDir);
    }

    private void PublishLibrary (string source)
    {
        var path = Path.Combine(PublishDir, "dotnet.js");
        File.WriteAllText(path, source);
        Log.LogMessage(MessageImportance.High, $"JavaScript UMD library is published at {path}.");
    }

    private void PublishDeclaration (string source)
    {
        var file = Path.Combine(PublishDir, "dotnet.d.ts");
        File.WriteAllText(file, source);
    }

    private void PublishSourceMap ()
    {
        var source = Path.Combine(JSDir, "dotnet.js.map");
        var destination = Path.Combine(PublishDir, "dotnet.js.map");
        File.Copy(source, destination, true);
    }

    private void PublishBinaries (byte[] wasmBytes, IEnumerable<Assembly> assemblies)
    {
        File.WriteAllBytes(Path.Combine(PublishDir, Path.GetFileName(WasmFile)), wasmBytes);
        Directory.CreateDirectory(Path.Combine(PublishDir, "managed"));
        foreach (var assembly in assemblies)
            File.WriteAllBytes(Path.Combine(PublishDir, $"managed/{assembly.Name}"), assembly.Bytes);
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(BlazorOutDir, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspector InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        inspector.InspectInDirectory(BlazorOutDir);
        inspector.Report(Log);
        return inspector;
    }

    private string GenerateLibrary (byte[] wasmBytes, AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new LibraryGenerator(spaceBuilder);
        var runtimeJS = File.ReadAllText(Path.Combine(JSDir, "dotnet.js"));
        if (!EmbedBinaries) return generator.GenerateSideLoad(runtimeJS, inspector);
        var wasm64 = Convert.ToBase64String(wasmBytes);
        return generator.GenerateEmbedded(runtimeJS, wasm64, EntryAssemblyName, inspector);
    }

    private string GenerateDeclaration (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new DeclarationGenerator(spaceBuilder, EmbedBinaries);
        generator.LoadDeclarations(JSDir);
        return generator.Generate(inspector);
    }
}
