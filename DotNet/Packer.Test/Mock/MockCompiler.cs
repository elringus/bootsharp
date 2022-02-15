using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Packer.Test;

public class MockCompiler
{
    private static readonly string[] defaultUsings = {
        "System",
        "System.Collections.Generic",
        "System.Threading.Tasks",
        "DotNetJS",
        "Microsoft.JSInterop"
    };

    public void Compile (IEnumerable<MockClass> classes, string assemblyPath)
    {
        var sources = string.Join('\n', classes.Select(BuildSource));
        var compilation = CreateCompilation(assemblyPath, sources);
        var result = compilation.Emit(assemblyPath);
        Assert.True(result.Success);
    }

    private static string BuildSource (MockClass @class)
    {
        return string.Join('\n', defaultUsings.Select(u => $"using {u};")) +
               $"\nnamespace {@class.Space};" +
               $"\npublic class {@class.Name} {{ {string.Join('\n', @class.Lines)} }}";
    }

    private static CSharpCompilation CreateCompilation (string assemblyPath, string sources)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var tree = CSharpSyntaxTree.ParseText(sources);
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
