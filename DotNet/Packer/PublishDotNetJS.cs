using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Packer;

public class PublishDotNetJS : Task
{
    [Required] public string BaseDir { get; set; } = null!;
    [Required] public string BlazorOutDir { get; set; } = null!;
    [Required] public string JSDir { get; set; } = null!;
    [Required] public string WasmFile { get; set; } = null!;
    [Required] public string EntryAssemblyName { get; set; } = null!;
    public bool EmbedBinaries { get; set; } = true;
    public bool Clean { get; set; } = true;

    public override bool Execute ()
    {
        var (library, declaration) = GenerateSources();
        if (Clean) CleanBaseDirectory();
        PublishLibrary(library);
        PublishDeclaration(declaration);
        PublishSourceMap();
        return true;
    }

    private (string library, string declaration) GenerateSources ()
    {
        var builder = CreateNamespaceBuilder();
        using var inspector = InspectAssemblies(builder);
        return (GenerateLibrary(inspector, builder), GenerateDeclaration(inspector, builder));
    }

    private void CleanBaseDirectory ()
    {
        Directory.Delete(BaseDir, true);
        Directory.CreateDirectory(BaseDir);
    }

    private void PublishLibrary (string source)
    {
        var path = Path.Combine(BaseDir, "dotnet.js");
        File.WriteAllText(path, source);
        Log.LogMessage(MessageImportance.High, $"JavaScript UMD library is published at {path}.");
    }

    private void PublishDeclaration (string source)
    {
        var file = Path.Combine(BaseDir, "dotnet.d.ts");
        File.WriteAllText(file, source);
    }

    private void PublishSourceMap ()
    {
        var source = Path.Combine(JSDir, "dotnet.js.map");
        var destination = Path.Combine(BaseDir, "dotnet.js.map");
        File.Copy(source, destination, true);
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

    private string GenerateLibrary (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new LibraryGenerator(spaceBuilder);
        var runtimeJS = File.ReadAllText(Path.Combine(JSDir, "dotnet.js"));
        if (!EmbedBinaries) return generator.GenerateSideLoad(runtimeJS, inspector);
        var wasm = Convert.ToBase64String(File.ReadAllBytes(WasmFile));
        return generator.GenerateEmbedded(runtimeJS, wasm, EntryAssemblyName, inspector);
    }

    private string GenerateDeclaration (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new DeclarationGenerator(spaceBuilder, EmbedBinaries);
        generator.LoadDeclarations(JSDir);
        return generator.Generate(inspector);
    }
}
