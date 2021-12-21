using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

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
    public string ResultLibraryFile { get; }
    public string ResultMapFile { get; }
    public string ResultTypesFile { get; }
    public PublishDotNetJS Task { get; }

    private readonly string root = GetRandomRoot();
    private readonly Dictionary<string, List<MockSource>> addedAssemblies = new();

    public MockData ()
    {
        BaseDir = Path.Combine(root, "base");
        BlazorOutDir = Path.Combine(BaseDir, "blazor");
        JSDir = Path.Combine(root, "js");
        WasmFile = Path.Combine(JSDir, "dotnet.wasm");
        JSFile = Path.Combine(JSDir, "dotnet.js");
        MapFile = Path.Combine(JSDir, "dotnet.js.map");
        ResultLibraryFile = Path.Combine(BaseDir, "dotnet.js");
        ResultMapFile = Path.Combine(BaseDir, "dotnet.js.map");
        ResultTypesFile = Path.Combine(BaseDir, "dotnet.d.ts");
        Task = CreateTask();
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(root, true);

    public void AddBlazorOutAssembly (params MockSource[] sources)
    {
        var name = $"test{Guid.NewGuid():N}.dll";
        var path = Path.Combine(BlazorOutDir, name);
        var types = sources.Select(s => s.GetType()).Append(typeof(MockSource));
        MockAssembly.Emit(path, types);
        Task.EntryAssemblyName = name;
        addedAssemblies[name] = new List<MockSource>(sources);
    }

    public void AssertExpectedCodeGenerated ()
    {
        var libraryContent = File.ReadAllText(ResultLibraryFile);
        var typesContent = Task.EmitTypes ? File.ReadAllText(ResultTypesFile) : null;
        foreach (var (assembly, sources) in addedAssemblies)
        foreach (var source in sources)
            AssertExpectedLinesGenerated(assembly, source, libraryContent, typesContent);
    }

    private void AssertExpectedLinesGenerated (string assembly, MockSource source, string library, string types = default)
    {
        assembly = Path.GetFileNameWithoutExtension(assembly);
        AssertContainsExpectedLines(source.GetExpectedInitLines(assembly),
            ExtractLinesBetween(library, "// DotNetJSInitStart", "// DotNetJSInitEnd"));
        AssertContainsExpectedLines(source.GetExpectedBootLines(assembly),
            ExtractLinesBetween(library, "// DotNetJSBootStart", "// DotNetJSBootEnd"));
        if (!string.IsNullOrEmpty(types))
            AssertContainsExpectedLines(source.GetExpectedTypeLines(assembly),
                ExtractLinesBetween(types, "// MethodsStart", "// MethodsEnd"));
    }

    [ExcludeFromCodeCoverage]
    private void AssertContainsExpectedLines (string[] expected, string[] actual)
    {
        for (int i = 0; i < Math.Max(expected.Length, actual.Length); i++)
            if (i >= expected.Length) throw new Exception($"Actual contains extra: {actual[i]}");
            else if (i >= actual.Length) throw new Exception($"Actual misses: {expected[i]}");
            else Assert.Equal(expected[i], actual[i]);
    }

    private string[] ExtractLinesBetween (string content, string startMarker, string endMarker)
    {
        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal);
        var endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);
        var subContent = content.Substring(startIndex, endIndex - startIndex);
        var lines = subContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.TrimEntries);
        if (lines.Length <= 2) return Array.Empty<string>();
        return lines.Skip(1).Take(lines.Length - 2).ToArray();
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
