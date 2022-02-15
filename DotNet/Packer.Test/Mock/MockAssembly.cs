using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetJS;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.JSInterop;
using Xunit;

namespace Packer.Test;

public static class MockAssembly
{
    private static readonly string[] defaultUsings = {
        "System",
        "System.Collections.Generic",
        "System.Threading.Tasks",
        "DotNetJS",
        "Microsoft.JSInterop"
    };

    public static void Emit (string assemblyPath, IEnumerable<MockClass> sources)
    {
        var fileSource = string.Join('\n', sources.Select(BuildSource));
        var compilation = CreateCompilation(assemblyPath, fileSource);
        var result = compilation.Emit(assemblyPath);
        Assert.True(result.Success);
    }

    public static void EmitReferences (string directory)
    {
        foreach (var reference in CreateReferences())
            File.Copy(reference.FilePath, Path.Combine(directory, Path.GetFileName(reference.FilePath)));
    }

    private static string BuildSource (MockClass source)
    {
        return string.Join('\n', defaultUsings.Select(u => $"using {u};")) +
               $"\nnamespace {source.Space};" +
               $"\npublic class {source.Name} {{ {string.Join('\n', source.Lines)} }}";
    }

    private static PortableExecutableReference[] CreateReferences ()
    {
        var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        return new[] {
            MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JSFunctionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JSInvokableAttribute).Assembly.Location)
        };
    }

    private static CSharpCompilation CreateCompilation (string assemblyPath, string fileSource)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var tree = CSharpSyntaxTree.ParseText(fileSource);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var refs = GatherReferences(Path.GetDirectoryName(assemblyPath));
        return CSharpCompilation.Create(assemblyName, new[] { tree }, refs, options);
    }

    private static PortableExecutableReference[] GatherReferences (string directory)
    {
        var paths = Directory.GetFiles(directory, "*.dll");
        return paths.Select(p => MetadataReference.CreateFromFile(p)).ToArray();
    }
}
