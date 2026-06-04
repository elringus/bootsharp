global using static Bootsharp.Generate.Test.Global;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bootsharp.Generate.Test;

internal static class Global
{
    private static readonly CSharpParseOptions parseOptions = new(LanguageVersion.Latest);
    private static readonly GeneratorDriverOptions driveOptions = new(default, trackIncrementalGeneratorSteps: true);
    private static readonly CSharpCompilationOptions compileOptions = new(OutputKind.DynamicallyLinkedLibrary,
        nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true);
    private static readonly ImmutableArray<MetadataReference> references = [
        ..((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Select(p => MetadataReference.CreateFromFile(p))
    ];
    private static readonly SyntaxTree prelude = Parse(
        """
        global using System;
        global using System.Threading.Tasks;
        global using Bootsharp;

        namespace Bootsharp;

        public sealed class ImportAttribute : Attribute { public ImportAttribute (params Type[] _) { } }
        public sealed class ExportAttribute : Attribute { }
        """);

    public static SyntaxTree Parse (string source)
    {
        return CSharpSyntaxTree.ParseText(source, parseOptions);
    }

    public static CSharpCompilation Compile (SyntaxTree source)
    {
        return CSharpCompilation.Create("Test", [prelude, source], references, compileOptions);
    }

    public static GeneratorDriver CreateGenerator ()
    {
        var generator = new SourceGenerator().AsSourceGenerator();
        return CSharpGeneratorDriver.Create([generator], parseOptions: parseOptions, driverOptions: driveOptions);
    }

    public static void Verify (string source, string expected)
    {
        expected = $"#nullable enable\n#pragma warning disable\n\n{expected}";
        var (code, errs) = Generate(source);
        Assert.Empty(errs);
        Assert.Equal(expected, code, ignoreLineEndingDifferences: true);
    }

    public static void VerifyNone (string source)
    {
        var (code, errs) = Generate(source);
        Assert.Empty(errs);
        Assert.Empty(code);
    }

    private static (string code, Diagnostic[] errs) Generate (string source)
    {
        var cmp = Compile(Parse(source));
        var generator = CreateGenerator().RunGeneratorsAndUpdateCompilation(cmp, out var output, out _);
        var result = generator.GetRunResult().Results.Single();
        var code = result.GeneratedSources.Select(s => s.SourceText.ToString()).FirstOrDefault("");
        var errs = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        return (code, errs);
    }
}
