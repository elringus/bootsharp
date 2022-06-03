using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Generator.Test;

public class SourceGeneratorTest
{
    private readonly GeneratorVerifier<SourceGenerator> verifier = new();

    [Fact]
    public async Task WhenSourceIsEmptyNothingIsGenerated ()
    {
        verifier.TestCode = "";
        await verifier.RunAsync();
    }

    [Fact]
    public async Task WhenNoFunctionClassesNothingIsGenerated ()
    {
        verifier.TestCode = "partial class Foo { }";
        await verifier.RunAsync();
    }

    [Theory, MemberData(nameof(TestData.Functions), MemberType = typeof(TestData))]
    public async Task PartialFunctionsAreImplemented (string source, string expected)
    {
        verifier.TestCode = source;
        var expectedText = SourceText.From(expected, Encoding.UTF8);
        verifier.TestState.GeneratedSources.Add((typeof(SourceGenerator), "Functions0.g.cs", expectedText));
        await verifier.RunAsync();
    }

    [Theory, MemberData(nameof(TestData.Events), MemberType = typeof(TestData))]
    public async Task PartialEventsAreImplemented (string source, string expected)
    {
        verifier.TestCode = source;
        var expectedText = SourceText.From(expected, Encoding.UTF8);
        verifier.TestState.GeneratedSources.Add((typeof(SourceGenerator), "Events0.g.cs", expectedText));
        await verifier.RunAsync();
    }
}
