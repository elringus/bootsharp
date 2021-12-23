using System.Threading.Tasks;
using Xunit;

namespace Generator.Test;

public class SourceGeneratorTest
{
    private readonly GeneratorVerifier<SourceGenerator> verifier = new();

    [Theory, MemberData(nameof(TestData.Functions), MemberType = typeof(TestData))]
    public async Task PartialFunctionsAreImplemented (string source, string expected)
    {
        await verifier.VerifyAsync(source, "Functions.cs", expected);
    }
}
