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

    public string TestAssemblyFile { get; }
    public string BaseDir { get; }
    public string BlazorOutDir { get; }
    public string JSDir { get; }
    public string WasmFile { get; }
    public string JSFile { get; }
    public string MapFile { get; }
    public string EntryAssemblyName { get; }

    public MockData ()
    {
        TestAssemblyFile = Assembly.GetExecutingAssembly().Location;
        BaseDir = GetRandomBaseDir(TestAssemblyFile);
        BlazorOutDir = Path.Combine(BaseDir, "blazor");
        JSDir = Path.Combine(BaseDir, "js");
        WasmFile = Path.Combine(JSDir, "dotnet.wasm");
        JSFile = Path.Combine(JSDir, "dotnet.js");
        MapFile = Path.Combine(JSDir, "dotnet.js.map");
        EntryAssemblyName = Path.GetFileName(TestAssemblyFile);
    }

    public void Dispose () => Directory.Delete(BaseDir, true);

    public void CreateBuildResources ()
    {
        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(BlazorOutDir);
        File.Copy(TestAssemblyFile, Path.Combine(BlazorOutDir, EntryAssemblyName));
        Directory.CreateDirectory(JSDir);
        File.WriteAllText(WasmFile, WasmFileContent);
        File.WriteAllText(JSFile, JSFileContent);
        File.WriteAllText(MapFile, MapFileContent);
    }

    public PublishDotNetJS CreateTask () => new() {
        BaseDir = BaseDir,
        BlazorOutDir = BlazorOutDir,
        JSDir = JSDir,
        WasmFile = WasmFile,
        EntryAssemblyName = EntryAssemblyName,
        EmitSourceMap = true,
        EmitTypes = true,
        BuildEngine = BuildEngine.Create()
    };

    private static string GetRandomBaseDir (string assemblyFile)
    {
        var assemblyDir = Path.Combine(Path.GetDirectoryName(assemblyFile));
        return Path.Combine(assemblyDir, $"temp-{Guid.NewGuid()}");
    }
}
