using System;
using System.IO;
using System.Reflection;
using Bootsharp;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.CodeAnalysis;
using Microsoft.JSInterop;

namespace Packer.Test;

public sealed class MockData : IDisposable
{
    public const string WasmFileContent = "mockwasmcontent";
    public const string JSFileContent = "(function(){})();";
    public const string MapFileContent = "{version:3,file:\"dotnet.js\"}";

    public string PublishDir { get; }
    public string BlazorOutDir { get; }
    public string JSDir { get; }
    public string WasmFile { get; }
    public string JSFile { get; }
    public string MapFile { get; }
    public PublishBootsharp Task { get; }

    public string GeneratedLibrary => ReadGeneratedFileText("dotnet.js");
    public string GeneratedDeclaration => ReadGeneratedFileText("dotnet.d.ts");
    public string GeneratedMap => ReadGeneratedFileText("dotnet.js.map");

    private readonly string root = GetRandomRoot();
    private readonly MockCompiler compiler = new();

    public MockData ()
    {
        PublishDir = Path.Combine(root, "publish");
        BlazorOutDir = Path.Combine(PublishDir, "blazor");
        JSDir = Path.Combine(root, "js");
        WasmFile = Path.Combine(JSDir, "dotnet.wasm");
        JSFile = Path.Combine(JSDir, "dotnet.js");
        MapFile = Path.Combine(JSDir, "dotnet.js.map");
        Task = CreateTask();
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(root, true);

    public void AddAssembly (MockAssembly assembly)
    {
        var assemblyPath = Path.Combine(BlazorOutDir, assembly.Name);
        compiler.Compile(assembly.Sources, assemblyPath);
        Task.EntryAssemblyName = assembly.Name;
    }

    private string ReadGeneratedFileText (string fileName)
    {
        var filePath = Path.Combine(PublishDir, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }

    private PublishBootsharp CreateTask () => new() {
        PublishDir = PublishDir,
        FrameworkDir = BlazorOutDir,
        JSDir = JSDir,
        WasmFile = WasmFile,
        EntryAssemblyName = "System.Runtime.dll",
        BuildEngine = BuildEngine.Create()
    };

    private void CreateBuildResources ()
    {
        Directory.CreateDirectory(PublishDir);
        Directory.CreateDirectory(BlazorOutDir);
        Directory.CreateDirectory(JSDir);
        File.WriteAllText(WasmFile, WasmFileContent);
        File.WriteAllText(JSFile, JSFileContent);
        File.WriteAllText(MapFile, MapFileContent);
        foreach (var path in GetReferencePaths())
            File.Copy(path, Path.Combine(BlazorOutDir, Path.GetFileName(path)));
    }

    private static string GetRandomRoot ()
    {
        var testAssembly = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        return Path.Combine(assemblyDir, $"temp{Guid.NewGuid():N}");
    }

    private static string[] GetReferencePaths ()
    {
        var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        return new[] {
            MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")).FilePath,
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location).FilePath,
            MetadataReference.CreateFromFile(typeof(JSFunctionAttribute).Assembly.Location).FilePath,
            MetadataReference.CreateFromFile(typeof(JSInvokableAttribute).Assembly.Location).FilePath
        };
    }
}
