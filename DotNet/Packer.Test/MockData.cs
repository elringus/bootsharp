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

    public string BaseDir { get; }
    public string BlazorOutDir { get; }
    public string JSDir { get; }
    public string WasmFile { get; }
    public string JSFile { get; }
    public string MapFile { get; }

    private readonly string root = GetRandomRoot();

    public MockData ()
    {
        BaseDir = Path.Combine(root, "base");
        BlazorOutDir = Path.Combine(BaseDir, "blazor");
        JSDir = Path.Combine(root, "js");
        WasmFile = Path.Combine(JSDir, "dotnet.wasm");
        JSFile = Path.Combine(JSDir, "dotnet.js");
        MapFile = Path.Combine(JSDir, "dotnet.js.map");
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(root, true);

    public void AddBlazorOutAssembly (string name, string code)
    {
        var path = Path.Combine(BlazorOutDir, name);
        MockAssembly.Emit(path, code);
    }

    public PublishDotNetJS CreateTask (string entryAssemblyName) => new() {
        BaseDir = BaseDir,
        BlazorOutDir = BlazorOutDir,
        JSDir = JSDir,
        WasmFile = WasmFile,
        EntryAssemblyName = entryAssemblyName,
        EmitSourceMap = true,
        EmitTypes = true,
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
        MockAssembly.EmitReferences(BlazorOutDir);
    }

    private static string GetRandomRoot ()
    {
        var testAssembly = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        return Path.Combine(assemblyDir, $"temp-{Guid.NewGuid()}");
    }
}
