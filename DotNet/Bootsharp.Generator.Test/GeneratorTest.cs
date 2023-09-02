using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        protected override string DefaultTestProjectName => "GeneratorTest";
        protected override ParseOptions CreateParseOptions () => new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose);
        protected override bool IsCompilerDiagnosticIncluded (Diagnostic diagnostic, CompilerDiagnostics _) =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            // CS8795 is for missing generated method from System.Runtime.InteropServices.JavaScript (not generated in test run).
            diagnostic.Id != "CS8795";
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

    [Fact]
    public async Task DoesntEmitDuplicateRegistrations ()
    {
        verifier.TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;
        await Verify(
            """
            partial class FunctionAfterInvokable
            {
                [JSInvokable] static void Bar () { }
                [JSFunction] partial void Baz ();
            }
            partial class EventAfterInvokable
            {
                [JSInvokable] static void Bar () { }
                [JSEvent] partial void Baz ();
            }
            partial class EventAfterFunction
            {
                [JSFunction] partial void Bar ();
                [JSEvent] partial void Baz ();
            }
            """);
    }

    [Theory, MemberData(nameof(FunctionTest.Data), MemberType = typeof(InvokableTest))]
    public Task PartialInvokableAreImplemented (string source, string expected)
        => Verify(source, ("FooInvokable.g.cs", expected));

    [Theory, MemberData(nameof(FunctionTest.Data), MemberType = typeof(FunctionTest))]
    public Task PartialFunctionsAreImplemented (string source, string expected)
        => Verify(source, ("FooFunctions.g.cs", expected));

    [Theory, MemberData(nameof(EventTest.Data), MemberType = typeof(EventTest))]
    public Task PartialEventsAreImplemented (string source, string expected)
        => Verify(source, ("FooEvents.g.cs", expected));

    [Theory, MemberData(nameof(ExportTest.Data), MemberType = typeof(ExportTest))]
    public Task ExportTypesAreGenerated (string source, string expected)
        => Verify(source, ("IFooExport.g.cs", expected));

    [Theory, MemberData(nameof(ImportTest.Data), MemberType = typeof(ImportTest))]
    public Task ImportTypesAreGenerated (string source, string expected)
        => Verify(source, ("IFooImport.g.cs", expected));

    private async Task Verify (string source, params (string file, string content)[] expected)
    {
        IncludeBootsharpSources(verifier.TestState.Sources);
        IncludeCommonSource(ref source);
        verifier.TestCode = source;
        verifier.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        for (int i = 0; i < expected.Length; i++)
        {
            IncludeCommonExpected(ref expected[i].content);
            verifier.TestState.GeneratedSources.Add((typeof(SourceGenerator), expected[i].file,
                SourceText.From(expected[i].content, Encoding.UTF8)));
        }
        await verifier.RunAsync();
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
