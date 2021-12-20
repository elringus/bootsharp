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

    public MockData ()
    {
        BaseDir = GetRandomBaseDir();
        BlazorOutDir = Path.Combine(BaseDir, "blazor");
        JSDir = Path.Combine(BaseDir, "js");
        WasmFile = Path.Combine(JSDir, "dotnet.wasm");
        JSFile = Path.Combine(JSDir, "dotnet.js");
        MapFile = Path.Combine(JSDir, "dotnet.js.map");
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(BaseDir, true);

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
    }

    private static string GetRandomBaseDir ()
    {
        var testAssembly = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        return Path.Combine(assemblyDir, $"temp-{Guid.NewGuid()}");
    }
}
