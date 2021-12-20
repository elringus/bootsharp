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
    public static void Emit (string assemblyPath, params string[] code)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var trees = code.Select(c => CSharpSyntaxTree.ParseText(c));
        var compilation = CreateCompilation(assemblyName, trees);
        var result = compilation.Emit(assemblyPath);
        foreach (var diagnostic in result.Diagnostics)
            Assert.Null(diagnostic.GetMessage());
    }

    private static CSharpCompilation CreateCompilation (string assemblyName, IEnumerable<SyntaxTree> trees)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var coreRefDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var references = new[] {
            MetadataReference.CreateFromFile(Path.Combine(coreRefDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JSFunctionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JSInvokableAttribute).Assembly.Location)
        };
        return CSharpCompilation.Create(assemblyName, trees, references, options);
    }
}
