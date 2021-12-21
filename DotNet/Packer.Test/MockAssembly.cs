using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DotNetJS;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.JSInterop;
using Xunit;

namespace Packer.Test;

public static class MockAssembly
{
    private static readonly PortableExecutableReference[] references = CreateReferences();

    public static void Emit (string assemblyPath, IEnumerable<Type> types)
    {
        var code = types.Select(t => ReadTypeSourceCode(t));
        var compilation = CreateCompilation(assemblyPath, code);
        var result = compilation.Emit(assemblyPath);
        AssertEmitted(result);
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

    private static string ReadTypeSourceCode (Type type, [CallerFilePath] string callerPath = "")
    {
        var directory = Path.GetDirectoryName(callerPath);
        var filePath = Path.Combine(directory, "MockSource", $"{type.Name}.cs");
        return File.ReadAllText(filePath);
    }

    private static CSharpCompilation CreateCompilation (string assemblyPath, IEnumerable<string> code)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var trees = code.Select(c => CSharpSyntaxTree.ParseText(c));
        var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        return CSharpCompilation.Create(assemblyName, trees, references, compileOptions);
    }

    [ExcludeFromCodeCoverage]
    private static void AssertEmitted (EmitResult result)
    {
        foreach (var diagnostic in result.Diagnostics)
            Assert.Null(diagnostic.GetMessage());
        Assert.True(result.Success);
    }
}
