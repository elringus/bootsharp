using System;
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
    private static readonly PortableExecutableReference[] references = CreateReferences();

    public static void Emit (string assemblyPath, IEnumerable<string> sources)
    {
        var fileSource = BuildFileSource(sources);
        var compilation = CreateCompilation(assemblyPath, fileSource);
        var result = compilation.Emit(assemblyPath);
        Assert.True(result.Success);
    }

    public static void EmitReferences (string directory)
    {
        foreach (var reference in references)
            File.Copy(reference.FilePath, Path.Combine(directory, Path.GetFileName(reference.FilePath)));
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

    private static string BuildFileSource (IEnumerable<string> sources)
    {
        var usings = new[] { "System", "System.Threading.Tasks", "DotNetJS", "Microsoft.JSInterop" };
        var source = $"public class c{Guid.NewGuid():N} {{ {string.Join('\n', sources)} }}";
        return string.Join('\n', usings.Select(u => $"using {u};")) + source;
    }

    private static CSharpCompilation CreateCompilation (string assemblyPath, string fileSource)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var tree = CSharpSyntaxTree.ParseText(fileSource);
        var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        return CSharpCompilation.Create(assemblyName, new[] { tree }, references, compileOptions);
    }
}
