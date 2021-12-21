using System;
using System.Collections.Generic;
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
    private readonly List<MockSource> addedSources = new();

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
        addedSources.AddRange(sources);
        var name = Guid.NewGuid().ToString("N");
        var path = Path.Combine(BlazorOutDir, name);
        var code = sources.Select(s => File.ReadAllText(s.SourceFilePath)).ToArray();
        MockAssembly.Emit(path, code);
        Task.EntryAssemblyName = name;
    }

    public void AssertExpectedJSGenerated ()
    {
        var libraryContent = File.ReadAllText(ResultLibraryFile);
        var typesContent = Task.EmitTypes ? File.ReadAllText(ResultTypesFile) : null;
        Assert.StartsWith(JSFileContent, libraryContent);
        foreach (var source in addedSources)
            AssertExpectedLinesGenerated(source, libraryContent, typesContent);
    }

    private PublishDotNetJS CreateTask () => new() {
        BaseDir = BaseDir,
        BlazorOutDir = BlazorOutDir,
        JSDir = JSDir,
        WasmFile = WasmFile,
        BuildEngine = BuildEngine.Create()
    };

    private void AssertExpectedLinesGenerated (MockSource source, string libraryContent, string typesContent)
    {
        AssertActualContainsExpectedLines(source.GetExpectedInitLines(),
            ExtractLinesBetween(libraryContent, "// DotNetJSInitStart", "// DotNetJSInitEnd"));
        AssertActualContainsExpectedLines(source.GetExpectedBootLines(),
            ExtractLinesBetween(libraryContent, "// DotNetJSBootStart", "// DotNetJSBootEnd"));
        if (Task.EmitTypes)
            AssertActualContainsExpectedLines(source.GetExpectedTypeLines(),
                ExtractLinesBetween(typesContent, "// MethodsStart", "// MethodsEnd"));
    }

    private void AssertActualContainsExpectedLines (string[] expectedLines, string[] actualLines)
    {
        Assert.Equal(expectedLines.Length, actualLines.Length);
        foreach (var expected in expectedLines)
            Assert.Contains(actualLines, actual => actual.Trim() == expected.Trim());
    }

    private string[] ExtractLinesBetween (string content, string startMarker, string endMarker)
    {
        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal);
        var endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);
        var subContent = content.Substring(startIndex, endIndex - startIndex);
        var lines = subContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 2) return Array.Empty<string>();
        return lines.Skip(1).Take(lines.Length - 2).ToArray();
    }

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
