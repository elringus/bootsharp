using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Generator.Test;

public class GeneratorVerifier<T> : CSharpSourceGeneratorTest<T, XUnitVerifier>
    where T : ISourceGenerator, new()
{
    protected override bool IsCompilerDiagnosticIncluded (Diagnostic _, CompilerDiagnostics __) => false;
}
