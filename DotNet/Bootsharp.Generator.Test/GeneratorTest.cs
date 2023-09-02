using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Bootsharp.Generator.Test;

public class GeneratorTest
{
    private static readonly List<(string file, string content)> sourceCache = new();
    private readonly Verifier<SourceGenerator> verifier = new();

    [Fact]
    public async Task WhenSourceIsEmptyNothingIsGenerated ()
    {
        await Verify("");
    }

    [Fact]
    public async Task NothingIsGeneratedWhenNoAttributes ()
    {
        await Verify("partial class Foo { }");
    }

    [Fact]
    public async Task WhenAttributeIsFromOtherNamespaceItsIgnored ()
    {
        await Verify(
            """
            [assembly:JSNamespace(@"Foo", "Bar")]
            public class JSNamespaceAttribute : System.Attribute { public JSNamespaceAttribute (string _, string __) { } }
            """);
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
        if (sourceCache.Count > 0)
        {
            foreach (var source in sourceCache)
                sources.Add(source);
            return;
        }
        var root = $"{Environment.CurrentDirectory}/../../../../Bootsharp";
        foreach (var path in Directory.EnumerateFiles($"{root}/Attributes", "*.cs"))
            sourceCache.Add((Path.GetFileName(path), File.ReadAllText(path)));
        foreach (var path in Directory.EnumerateFiles($"{root}/Interop", "*.cs"))
            sourceCache.Add((Path.GetFileName(path), File.ReadAllText(path)));
        sourceCache.Add(("Error.cs", File.ReadAllText($"{root}/Error.cs")));
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
