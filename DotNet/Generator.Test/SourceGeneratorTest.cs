using System.Threading.Tasks;
using Xunit;

namespace Generator.Test;

public class SourceGeneratorTest
{
    private const string source = @"
partial class Foo
{
    [JSFunction]
    partial void Bar();
}
";
    private const string expected = @"
partial class Foo
{
partial void Bar () => JS.Invoke(""DotNetJS_functions_TestProject_Bar"");
}
";

    private readonly GeneratorVerifier<SourceGenerator> verifier = new();

    [Fact]
    public async Task Test1 ()
    {
        await verifier.VerifyAsync(source, "Functions.cs", expected);
    }
}
