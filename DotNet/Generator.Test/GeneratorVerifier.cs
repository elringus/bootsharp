using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;

namespace Generator.Test;

public class GeneratorVerifier<T> : CSharpSourceGeneratorTest<T, XUnitVerifier>
    where T : ISourceGenerator, new()
{
    protected override bool IsCompilerDiagnosticIncluded (Diagnostic _, CompilerDiagnostics __) => false;

    public Task VerifyAsync (string sourceCode, string expectedFile, string expectedCode)
    {
        TestCode = sourceCode;
        var expectedText = SourceText.From(expectedCode, Encoding.UTF8);
        TestState.GeneratedSources.Add((typeof(SourceGenerator), expectedFile, expectedText));
        return RunAsync();
    }
}
