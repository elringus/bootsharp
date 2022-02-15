using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Packer.Test;

public sealed class MockData : IDisposable
{
    public const string WasmFileContent = "mockwasmcontent";
    public const string JSFileContent = "(function(){})();";
    public const string MapFileContent = "{version:3,file:\"dotnet.js\"}";
    public const string InteropTypeContent = "export interface Interop {}";
    public const string BootTypeContent = "import from \"./interop\";\nexport interface Boot {}";

    public string BaseDir { get; }
    public string BlazorOutDir { get; }
    public string JSDir { get; }
    public string WasmFile { get; }
    public string JSFile { get; }
    public string MapFile { get; }
    public PublishDotNetJS Task { get; }

    public string GeneratedLibrary => ReadGeneratedFileText("dotnet.js");
    public string GeneratedMap => ReadGeneratedFileText("dotnet.js.map");
    public string GeneratedTypes => ReadGeneratedFileText("dotnet.d.ts");

    private readonly string root = GetRandomRoot();

    public MockData ()
    {
        BaseDir = Path.Combine(root, "base");
        BlazorOutDir = Path.Combine(BaseDir, "blazor");
        JSDir = Path.Combine(root, "js");
        WasmFile = Path.Combine(JSDir, "dotnet.wasm");
        JSFile = Path.Combine(JSDir, "dotnet.js");
        MapFile = Path.Combine(JSDir, "dotnet.js.map");
        Task = CreateTask();
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(root, true);

    public void AddAssemblyWithName (string assemblyName, params MockClass[] classes)
    {
        var path = Path.Combine(BlazorOutDir, assemblyName);
        MockAssembly.Emit(path, classes);
        Task.EntryAssemblyName = assemblyName;
    }

    public void AddAssemblyWithName (string assemblyName, params string[] classLines)
    {
        AddAssemblyWithName(assemblyName, new MockClass { Lines = classLines });
    }

    public void AddAssemblyWithName (string assemblyName)
    {
        AddAssemblyWithName(assemblyName, Array.Empty<MockClass>());
    }

    public void AddAssembly (params MockClass[] classes)
    {
        AddAssemblyWithName($"MockAssembly{Guid.NewGuid():N}.dll", classes);
    }

    public void AddAssembly (params string[] classLines)
    {
        AddAssembly(new MockClass { Lines = classLines });
    }

    private string ReadGeneratedFileText (string fileName)
    {
        var filePath = Path.Combine(BaseDir, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }

    private PublishDotNetJS CreateTask () => new() {
        BaseDir = BaseDir,
        BlazorOutDir = BlazorOutDir,
        JSDir = JSDir,
        WasmFile = WasmFile,
        BuildEngine = BuildEngine.Create()
    };

    private void CreateBuildResources ()
    {
        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(BlazorOutDir);
        Directory.CreateDirectory(JSDir);
        File.WriteAllText(WasmFile, WasmFileContent);
        File.WriteAllText(JSFile, JSFileContent);
        File.WriteAllText(MapFile, MapFileContent);
        File.WriteAllText(Path.Combine(JSDir, "interop.d.ts"), InteropTypeContent);
        File.WriteAllText(Path.Combine(JSDir, "boot.d.ts"), BootTypeContent);
        MockAssembly.EmitReferences(BlazorOutDir);
    }

    private static string GetRandomRoot ()
    {
        var testAssembly = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        return Path.Combine(assemblyDir, $"temp{Guid.NewGuid():N}");
    }
}
