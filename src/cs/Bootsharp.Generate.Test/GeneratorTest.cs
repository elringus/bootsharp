using Microsoft.CodeAnalysis;

namespace Bootsharp.Generate.Test;

public class GeneratorTest
{
    [Theory, MemberData(nameof(ImportMethodTest.Data), MemberType = typeof(ImportMethodTest))]
    public void ImplementsImportedMethods (string source, string expected) => Verify(source, expected);

    [Theory, MemberData(nameof(ImportPropertyTest.Data), MemberType = typeof(ImportPropertyTest))]
    public void ImplementsImportedProperties (string source, string expected) => Verify(source, expected);

    [Theory, MemberData(nameof(ImportEventTest.Data), MemberType = typeof(ImportEventTest))]
    public void ImplementsImportedEvents (string source, string expected) => Verify(source, expected);

    [Fact]
    public void GeneratesNothingWhenSourceIsEmpty () =>
        VerifyNone("");

    [Fact]
    public void GeneratesNothingWithoutImportAttributes () =>
        VerifyNone("partial class Foo { [Export] static void Bar () { } }");

    [Fact]
    public void IgnoresAssemblyLevelImport () =>
        VerifyNone("[assembly: Import(typeof(string))]");

    [Fact]
    public void IgnoresImportAttributeFromOtherNamespace () =>
        VerifyNone(
            """
            namespace Other { public sealed class ImportAttribute : Attribute { } }

            partial class Foo
            {
                [Other.Import] partial void Bar ();
            }
            """);

    [Fact]
    public void MergesAllMembersOfClassIntoSingleSource () => Verify(
        """
        partial class Foo
        {
            [Export] static void Exported () { }
            [Import] partial void Method ();
            [Import] static partial int Prop { get; set; }
            [Import] static event Action? Event;
        }
        """,
        """
        unsafe partial class Foo
        {
            public static delegate* managed<void> Bootsharp_Method;
            partial void Method () => Bootsharp_Method();
            static partial global::System.Int32 Prop { get => Bootsharp_GetProp(); set => Bootsharp_SetProp(value); }
            public static delegate* managed<global::System.Int32> Bootsharp_GetProp;
            public static delegate* managed<global::System.Int32, void> Bootsharp_SetProp;
            internal static void Bootsharp_Invoke_Event () => Event?.Invoke();
        }
        """);

    [Fact]
    public void RunsIncrementally ()
    {
        var foo = Parse("partial class Foo { [Import] partial void Bar (); }");
        var compilation = Compile(foo);
        var generator = CreateGenerator().RunGenerators(compilation);

        // an unrelated edit must not re-run the semantic resolution ("members") or grouping ("classes")
        var unrelated = compilation.AddSyntaxTrees(Parse("class Other;"));
        generator = generator.RunGenerators(unrelated);
        AssertSteps(generator, IncrementalStepRunReason.Cached);

        // editing the import itself invalidates the cache and regenerates
        var edited = unrelated.ReplaceSyntaxTree(foo, Parse("partial class Foo { [Import] partial void Baz (); }"));
        generator = generator.RunGenerators(edited);
        AssertSteps(generator, IncrementalStepRunReason.Modified);

        static void AssertSteps (GeneratorDriver generator, IncrementalStepRunReason reason)
        {
            var steps = generator.GetRunResult().Results.Single().TrackedSteps;
            Assert.All(steps["members"], step => Assert.All(step.Outputs, o => Assert.Equal(reason, o.Reason)));
            Assert.All(steps["classes"], step => Assert.All(step.Outputs, o => Assert.Equal(reason, o.Reason)));
        }
    }
}
