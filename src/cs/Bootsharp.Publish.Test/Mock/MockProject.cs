using Microsoft.CodeAnalysis;

namespace Bootsharp.Publish.Test;

public sealed class MockProject : IDisposable
{
    public string Root { get; }

    private readonly MockCompiler compiler = new();

    public MockProject ()
    {
        Root = CreateUniqueRootDirectory();
        CreateBuildResources();
    }

    public void Dispose () => Directory.Delete(Root, true);

    public void AddAssembly (MockAssembly assembly)
    {
        var assemblyPath = Path.Combine(Root, assembly.Name);
        compiler.Compile(assembly.Sources, assemblyPath);
    }

    public void WriteFile (string name, ReadOnlySpan<byte> content)
    {
        var filePath = Path.Combine(Root, name);
        File.WriteAllBytes(filePath, content.ToArray());
    }

    public void WriteFile (string name, string content)
    {
        var filePath = Path.Combine(Root, name);
        File.WriteAllText(filePath, content);
    }

    private static string CreateUniqueRootDirectory ()
    {
        var testAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        var dir = $"bootsharp-temp-{Guid.NewGuid():N}";
        return Directory.CreateDirectory(Path.Combine(assemblyDir, dir)).FullName;
    }

    private void CreateBuildResources ()
    {
        foreach (var path in GetReferencePaths())
            File.Copy(path, Path.Combine(Root, Path.GetFileName(path)), true);
    }

    private static string[] GetReferencePaths ()
    {
        var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        return [
            MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")).FilePath,
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location).FilePath,
            MetadataReference.CreateFromFile(typeof(JSExportAttribute).Assembly.Location).FilePath
        ];
    }
}
