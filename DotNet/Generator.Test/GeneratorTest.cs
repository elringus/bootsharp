using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Generator.Test;

public class GeneratorTest
{
    private class Verifier<T> : CSharpSourceGeneratorTest<T, XUnitVerifier>
        where T : ISourceGenerator, new()
    {
        protected override bool IsCompilerDiagnosticIncluded (Diagnostic _, CompilerDiagnostics __) => false;
    }

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
        expected = "#nullable enable\n#pragma warning disable\n" +
                   expected +
                   "\n#pragma warning restore\n#nullable restore\n";
        verifier.TestCode = source;
        verifier.TestState.Sources.Add(("Bootsharp.cs",
            """
            using System;

            namespace Bootsharp;

            public class JSFunctionAttribute : Attribute {  }
            public class JSEventAttribute : Attribute {  }
            public class JSTypeAttribute : Attribute {
                public Type[] Types { get; }
                public string? NamePattern { get; init; }
                public string? NameReplacement { get; init; }
                public string? InvokePattern { get; init; }
                public string? InvokeReplacement { get; init; }
                protected JSTypeAttribute (Type[] types) { Types = types; }
            }
            public class JSExportAttribute : JSTypeAttribute {
                public JSExportAttribute (Type[] types) : base(types) { }
            }
            public class JSImportAttribute : JSTypeAttribute {
                public string? EventPattern { get; init; }
                public string? EventReplacement { get; init; }
                public JSImportAttribute (Type[] types) : base(types) { }
            }
            public class JSNamespaceAttribute : Attribute {
                public string Pattern { get; }
                public string Replacement { get; }
                public JSNamespaceAttribute (string pattern, string replacement) {
                    Pattern = pattern;
                    Replacement = replacement;
                }
            }
            """));
        var expectedText = SourceText.From(expected, Encoding.UTF8);
        verifier.TestState.GeneratedSources.Add((typeof(SourceGenerator), file, expectedText));
        return verifier.RunAsync();
    }
}
