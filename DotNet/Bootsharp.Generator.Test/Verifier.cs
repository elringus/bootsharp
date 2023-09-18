using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Bootsharp.Generator.Test;

public sealed class Verifier<T> : CSharpSourceGeneratorTest<T, XUnitVerifier>
    where T : IIncrementalGenerator, new()
{
    protected override string DefaultTestProjectName { get; } = "GeneratorTest";

    public Verifier () => ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

    protected override bool IsCompilerDiagnosticIncluded (Diagnostic diagnostic, CompilerDiagnostics _) =>
        diagnostic.Severity == DiagnosticSeverity.Error;
}
