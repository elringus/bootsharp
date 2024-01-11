using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bootsharp.Publish.Test;

public class MockCompiler
{
    private static readonly string[] defaultUsings = [
        "System",
        "System.Collections.Generic",
        "System.Threading.Tasks",
        "Bootsharp"
    ];

    public void Compile (IEnumerable<MockSource> sources, string assemblyPath)
    {
        var source = string.Join('\n', defaultUsings.Select(u => $"using {u};")) + '\n' +
                     string.Join('\n', sources.Select(BuildSource));
        var compilation = CreateCompilation(assemblyPath, source);
        var result = compilation.Emit(assemblyPath);
        if (result.Success) return;
        var error = $"Invalid test source code: {result.Diagnostics.First().GetMessage()}";
        Assert.Fail(string.Join('\n', [error, "---", source, "---"]));
    }

    private static string BuildSource (MockSource source)
    {
        var text = source.WrapInClass
            ? $"public partial class Class {{ {source.Code} }}"
            : source.Code;
        return source.Namespace is null ? text
            : $"namespace {source.Namespace} {{ {text} }}";
    }

    private static CSharpCompilation CreateCompilation (string assemblyPath, string text)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var tree = CSharpSyntaxTree.ParseText(text);
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
