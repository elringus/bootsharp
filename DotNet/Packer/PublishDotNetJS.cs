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
    public bool EmitTypes { get; set; }

    private readonly AssemblyInspector inspector = new();
    private readonly SourceGenerator sourceGenerator = new();
    private readonly TypeGenerator typeGenerator = new();

    public override bool Execute ()
    {
        InspectAssemblies();
        if (Clean) CleanBaseDirectory();
        var librarySource = GenerateLibrarySource();
        PublishLibrary(librarySource);
        if (EmitSourceMap) PublishSourceMap();
        if (EmitTypes) PublishTypes();
        return true;
    }

    private void InspectAssemblies ()
    {
        inspector.InspectInDirectory(BlazorOutDir);
        inspector.Report(Log);
    }

    private void CleanBaseDirectory ()
    {
        Directory.Delete(BaseDir, true);
        Directory.CreateDirectory(BaseDir);
    }

    private string GenerateLibrarySource ()
    {
        var wasm = GetRuntimeWasm();
        var js = GetRuntimeJS();
        return sourceGenerator.Generate(js, wasm, EntryAssemblyName, inspector);
    }

    private void PublishLibrary (string source)
    {
        var path = Path.Combine(BaseDir, "dotnet.js");
        File.WriteAllText(path, source);
        Log.LogMessage(MessageImportance.High, $"JavaScript UMD library is published at {path}.");
    }

    private void PublishSourceMap ()
    {
        var source = Path.Combine(JSDir, "dotnet.js.map");
        var destination = Path.Combine(BaseDir, "dotnet.js.map");
        File.Copy(source, destination, true);
    }

    private void PublishTypes ()
    {
        typeGenerator.LoadDefinitions(JSDir);
        var source = typeGenerator.Generate(inspector);
        var file = Path.Combine(BaseDir, "dotnet.d.ts");
        File.WriteAllText(file, source);
    }

    private string GetRuntimeJS ()
    {
        var path = Path.Combine(JSDir, "dotnet.js");
        return File.ReadAllText(path);
    }

    private string GetRuntimeWasm ()
    {
        var binary = File.ReadAllBytes(WasmFile);
        return Convert.ToBase64String(binary);
    }
}
