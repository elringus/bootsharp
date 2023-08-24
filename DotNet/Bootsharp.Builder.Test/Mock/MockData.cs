using System;
using System.IO;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.CodeAnalysis;

namespace Bootsharp.Builder.Test;

public sealed class MockData : IDisposable
{
    public const string LibMainContent = "(function(){})();";
    public const string LibMapContent = "{version:3,file:\"dotnet.js\"}";

    public string BuildDir { get; }
    public string LibMainFile { get; }
    public string LibMapFile { get; }
    public BuildBootsharp Task { get; }

    public string GeneratedLibrary => ReadGeneratedFileText("bootsharp.js");
    public string GeneratedDeclaration => ReadGeneratedFileText("bootsharp.d.ts");
    public string GeneratedMap => ReadGeneratedFileText("bootsharp.js.map");

    private readonly string root = GetRandomRoot();
    private readonly MockCompiler compiler = new();

    public MockData ()
    {
        BuildDir = Path.Combine(root, "build");
        LibMainFile = Path.Combine(JSLibraryDir, "bootsharp-main.js");
        LibMapFile = Path.Combine(JSLibraryDir, "bootsharp-main.js.map");
        Task = CreateTask();
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(root, true);

    public void AddAssembly (MockAssembly assembly)
    {
        var assemblyPath = Path.Combine(BuildDir, assembly.Name);
        compiler.Compile(assembly.Sources, assemblyPath);
        Task.EntryAssemblyName = assembly.Name;
    }

    private string ReadGeneratedFileText (string fileName)
    {
        var filePath = Path.Combine(PublishDir, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }

    private BuildBootsharp CreateTask () => new() {
        BuildDir = BuildDir,
        EntryAssemblyName = "System.Runtime.dll",
        BuildEngine = BuildEngine.Create(),
        EmbedBinaries = true
    };

    private void CreateBuildResources ()
    {
        Directory.CreateDirectory(BuildDir);
        File.WriteAllText(LibMainFile, LibMainContent);
        File.WriteAllText(LibMapFile, LibMapContent);
        foreach (var path in GetReferencePaths())
            File.Copy(path, Path.Combine(BuildDir, Path.GetFileName(path)), true);
    }

    private static string GetRandomRoot ()
    {
        var testAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
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
