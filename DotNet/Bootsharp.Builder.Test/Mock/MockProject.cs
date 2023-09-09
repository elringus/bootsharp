using Microsoft.CodeAnalysis;

namespace Bootsharp.Builder.Test;

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

    public void WriteFile (string name, byte[] content)
    {
        var filePath = Path.Combine(Root, name);
        File.WriteAllBytes(filePath, content);
    }

    private static string CreateUniqueRootDirectory ()
    {
        var testAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.Combine(Path.GetDirectoryName(testAssembly));
        return Directory.CreateDirectory(Path.Combine(assemblyDir, $"temp{Guid.NewGuid():N}")).FullName;
    }

    private void CreateBuildResources ()
    {
        foreach (var path in GetReferencePaths())
            File.Copy(path, Path.Combine(Root, Path.GetFileName(path)), true);
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
