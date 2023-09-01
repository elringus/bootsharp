using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Bootsharp.Generator.Test;

public class GeneratorTest
{
    private class Verifier<T> : CSharpSourceGeneratorTest<T, XUnitVerifier>
        where T : ISourceGenerator, new()
    {
        protected override bool IsCompilerDiagnosticIncluded (Diagnostic _, CompilerDiagnostics __) => false;
    }

    private static readonly List<(string filename, string content)> bootsharpSourcesCache = new();
    private readonly Verifier<SourceGenerator> verifier = new();

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

    [Fact]
    public async Task WhenAttributeIsFromOtherNamespaceItsIgnored ()
    {
        verifier.TestCode =
            """
            [assembly:JSNamespace(@"Foo", "Bar")]
            public class JSNamespaceAttribute : System.Attribute { public JSNamespaceAttribute (string _, string __) { } }
            """;
        await verifier.RunAsync();
    }

    [Theory, MemberData(nameof(FunctionTest.Data), MemberType = typeof(FunctionTest))]
    public Task PartialFunctionsAreImplemented (string source, string expected)
        => Verify(source, expected, "FooFunctions.g.cs");

    [Theory, MemberData(nameof(EventTest.Data), MemberType = typeof(EventTest))]
    public Task PartialEventsAreImplemented (string source, string expected)
        => Verify(source, expected, "FooEvents.g.cs");

    [Theory, MemberData(nameof(ExportTest.Data), MemberType = typeof(ExportTest))]
    public Task ExportTypesAreGenerated (string source, string expected)
        => Verify(source, expected, "IFooExport.g.cs");

    [Theory, MemberData(nameof(ImportTest.Data), MemberType = typeof(ImportTest))]
    public Task ImportTypesAreGenerated (string source, string expected)
        => Verify(source, expected, "IFooImport.g.cs");

    private Task Verify (string source, string expected, string file)
    {
        IncludeCommonSource(ref source);
        IncludeCommonExpected(ref expected);
        IncludeBootsharpSources(verifier.TestState.Sources);
        var expectedText = SourceText.From(expected, Encoding.UTF8);
        verifier.TestState.GeneratedSources.Add((typeof(SourceGenerator), file, expectedText));
        verifier.TestCode = source;
        return verifier.RunAsync();
    }

    private static void IncludeBootsharpSources (SourceFileList sources)
    {
        if (bootsharpSourcesCache.Count > 0)
        {
            foreach (var source in bootsharpSourcesCache)
                sources.Add(source);
            return;
        }
        var root = $"{Environment.CurrentDirectory}/../../../../Bootsharp";
        foreach (var path in Directory.EnumerateFiles($"{root}/Attributes", "*.cs"))
            bootsharpSourcesCache.Add((Path.GetFileName(path), File.ReadAllText(path)));
        foreach (var path in Directory.EnumerateFiles($"{root}/Interop", "*.cs"))
            bootsharpSourcesCache.Add((Path.GetFileName(path), File.ReadAllText(path)));
        bootsharpSourcesCache.Add(("Error.cs", File.ReadAllText($"{root}/Error.cs")));
        IncludeBootsharpSources(sources);
    }

    private void IncludeCommonSource (ref string source) => source =
        $"""
         using Bootsharp;
         {source}
         """;

    private void IncludeCommonExpected (ref string expected) => expected =
        $"""
         #nullable enable
         #pragma warning disable
         using Bootsharp;
         using System.Diagnostics.CodeAnalysis;
         using System.Runtime.CompilerServices;
         {expected}
         #pragma warning restore
         #nullable restore
         """;
}
