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
            [assembly:Import([])]
            public class ImportAttribute : System.Attribute { public ImportAttribute (Type[] _) { } }
            """);
    }

    [Fact]
    public async Task DoesntEmitDuplicateImportSources ()
    {
        verifier.TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;
        await Verify(
            """
            partial class ImportMethodAfterExport
            {
                [Export] static void Bar () { }
                [Import] partial void Baz ();
            }
            partial class ImportEventAfterExport
            {
                [Export] static void Bar () { }
                [Import] static event Action? Baz;
            }
            partial class ImportEventAfterMethod
            {
                [Import] partial void Bar ();
                [Import] static event Action? Baz;
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
                [Export] public static void Bar () { }
                [Import] public static void Baz () { }
                [Import] public static event Action? Nya;
            }
            """));
        await Verify("");
    }

    [Theory, MemberData(nameof(ImportMethodTest.Data), MemberType = typeof(ImportMethodTest))]
    public Task PartialImportMethodsImplemented (string source, string expected)
        => Verify(source, ("FooImports.g.cs", expected));

    [Theory, MemberData(nameof(ImportEventTest.Data), MemberType = typeof(ImportEventTest))]
    public Task PartialImportEventsImplemented (string source, string expected)
        => Verify(source, ("FooImports.g.cs", expected));

    private async Task Verify (string source, params (string file, string content)[] expected)
    {
        IncludeBootsharpSources(verifier.TestState.Sources);
        verifier.TestCode = source;
        for (int i = 0; i < expected.Length; i++)
        {
            IncludeCommonExpected(ref expected[i].content);
            verifier.TestState.GeneratedSources.Add((typeof(SourceGenerator), expected[i].file,
                SourceText.From(expected[i].content, Encoding.UTF8, SourceHashAlgorithm.Sha256)));
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
