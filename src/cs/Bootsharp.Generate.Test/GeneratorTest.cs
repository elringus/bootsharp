using System.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Bootsharp.Generate.Test;

public class GeneratorTest
{
    private static readonly List<(string file, string content)> sourceCache = [];
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
            [assembly:JSImport([])]
            public class JSImportAttribute : System.Attribute { public JSImportAttribute (Type[] _) { } }
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

    [Fact]
    public async Task DoesntAnalyzeGeneratedFiles ()
    {
        // otherwise it'll pick files emitted in publish task
        verifier.TestState.Sources.Add(("foo.g.cs",
            """
            public static partial class Foo
            {
                [JSInvokable] public static void Bar () { }
                [JSFunction] public static void Baz () { }
                [JSEvent] public static void Nya () { }
            }
            """));
        await Verify("");
    }

    [Theory, MemberData(nameof(FunctionTest.Data), MemberType = typeof(FunctionTest))]
    public Task PartialFunctionsImplemented (string source, string expected)
        => Verify(source, ("FooFunctions.g.cs", expected));

    [Theory, MemberData(nameof(EventTest.Data), MemberType = typeof(EventTest))]
    public Task PartialEventsImplemented (string source, string expected)
        => Verify(source, ("FooEvents.g.cs", expected));

    private async Task Verify (string source, params (string file, string content)[] expected)
    {
        IncludeBootsharpSources(verifier.TestState.Sources);
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
        var root = Path.GetFullPath($"{Environment.CurrentDirectory}/../../../../Bootsharp.Common");
        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            if (!path.Replace("\\", "/").Contains("/obj/"))
                sourceCache.Add((Path.GetFileName(path), File.ReadAllText(path)));
        sourceCache.Add(("GlobalUsings.cs",
            """
            global using System;
            global using System.Collections.Generic;
            global using System.IO;
            global using System.Linq;
            global using System.Threading.Tasks;
            global using Bootsharp;
            """));
        IncludeBootsharpSources(sources);
    }

    private void IncludeCommonExpected (ref string expected) =>
        expected = $"""
                    #nullable enable
                    #pragma warning disable
                    {expected}
                    """;
}
