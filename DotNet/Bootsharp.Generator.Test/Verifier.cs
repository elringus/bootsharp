using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Bootsharp.Generator.Test;

// The package is outdated on public nuget due to: https://github.com/dotnet/roslyn-sdk/issues/1047
// .NET 7 and later sources: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json

public sealed class Verifier<T> : CSharpSourceGeneratorTest<T, XUnitVerifier>
    where T : IIncrementalGenerator, new()
{
    protected override string DefaultTestProjectName { get; } = "GeneratorTest";

    public Verifier () => ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

    protected override bool IsCompilerDiagnosticIncluded (Diagnostic diagnostic, CompilerDiagnostics _) =>
        diagnostic.Severity == DiagnosticSeverity.Error;
}
