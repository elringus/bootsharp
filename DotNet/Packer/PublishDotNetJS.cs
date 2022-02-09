using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Packer;

public class PublishDotNetJS : Task
{
    [Required] public string BaseDir { get; set; }
    [Required] public string BlazorOutDir { get; set; }
    [Required] public string JSDir { get; set; }
    [Required] public string WasmFile { get; set; }
    [Required] public string EntryAssemblyName { get; set; }
    public bool Clean { get; set; } = true;
    public bool EmitSourceMap { get; set; }
    public bool EmitTypes { get; set; } = true;
    public string NamespacePattern { get; set; }

    public override bool Execute ()
    {
        GenerateSources(out var library, out var declaration);
        if (Clean) CleanBaseDirectory();
        PublishLibrary(library);
        if (EmitTypes) PublishTypes(declaration);
        if (EmitSourceMap) PublishSourceMap();
        return true;
    }

    private void GenerateSources (out string library, out string declaration)
    {
        using var inspector = InspectAssemblies();
        library = GenerateLibrary(inspector);
        declaration = GenerateDeclaration(inspector);
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

    private void PublishTypes (string source)
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

    private AssemblyInspector InspectAssemblies ()
    {
        var inspector = new AssemblyInspector(new(NamespacePattern));
        inspector.InspectInDirectory(BlazorOutDir);
        inspector.Report(Log);
        return inspector;
    }

    private string GenerateLibrary (AssemblyInspector inspector)
    {
        var generator = new LibraryGenerator();
        var js = File.ReadAllText(Path.Combine(JSDir, "dotnet.js"));
        var wasm = Convert.ToBase64String(File.ReadAllBytes(WasmFile));
        return generator.Generate(js, wasm, EntryAssemblyName, inspector);
    }

    private string GenerateDeclaration (AssemblyInspector inspector)
    {
        var generator = new DeclarationGenerator(new(NamespacePattern));
        generator.LoadDeclarations(JSDir);
        return generator.Generate(inspector);
    }
}
