using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Bootsharp.Generate.Test;

public sealed class Verifier<T> : CSharpSourceGeneratorTest<T, DefaultVerifier>
    where T : IIncrementalGenerator, new()
{
    protected override string DefaultTestProjectName { get; } = "GeneratorTest";

    public Verifier () => ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

    protected override bool IsCompilerDiagnosticIncluded (Diagnostic diagnostic, CompilerDiagnostics _) =>
        diagnostic.Severity == DiagnosticSeverity.Error;
}
