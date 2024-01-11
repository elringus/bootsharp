namespace Bootsharp.Publish.Test;

public class SerializerTest : EmitTest
{
    protected override string TestedContent => GeneratedSerializer;

    [Fact]
    public void WhenNothingInspectedIsEmpty ()
    {
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void WhenNoSerializableTypesIsEmpty ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static bool? Foo (string a, int b, char c, DateTime d, DateTimeOffset e) => default;"),
            WithClass("[JSInvokable] public static byte[] Bar (int[] a, double[] b, string[] c) => default;")
        );
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void AssignsTypeInfoResolver ()
    {
        AddAssembly(
            With("n", "public record Info;"),
            WithClass("n", "[JSInvokable] public static void Foo (Info i) {}"));
        Execute();
        Contains(
            """
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void InjectTypeInfoResolver ()
                {
                    Serializer.Options.TypeInfoResolverChain.Add(SerializerContext.Default);
                }
            """);
    }

    [Fact] // .NET's generator indexes types by short names (w/o namespace) and fails on duplicates.
    public void AddsOnlyTopLevelTypesAndCrawledDuplicates ()
    {
        AddAssembly(
            With("y", "public struct Struct { public double A { get; set; } }"),
            With("n", "public struct Struct { public y.Struct S { get; set; } }"),
            With("n", "public readonly struct ReadonlyStruct { public double A { get; init; } }"),
            With("n", "public readonly record struct ReadonlyRecordStruct(double A);"),
            With("n", "public record class RecordClass(double A);"),
            With("n", "public enum Enum { A, B }"),
            With("n", "public class Foo { public Struct S { get; } public ReadonlyStruct Rs { get; } }"),
            WithClass("n", "public class Bar : Foo { public ReadonlyRecordStruct Rrs { get; } public RecordClass Rc { get; } }"),
            With("n", "public class Baz { public List<Class.Bar?> Bars { get; } public Enum E { get; } }"),
            WithClass("n", "[JSInvokable] public static Baz? GetBaz () => default;"));
        Execute();
        Assert.Equal(2, Matches("JsonSerializable").Count);
        Contains("[JsonSerializable(typeof(global::n.Baz)");
        Contains("[JsonSerializable(typeof(global::y.Struct)");
    }

    [Fact]
    public void AddsProxiesForListInterface ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Foo (IList<string> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IList<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.List<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.String[])");
    }

    [Fact]
    public void AddsProxiesForReadOnlyListInterface ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Foo (IReadOnlyList<string> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IReadOnlyList<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.List<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.String[])");
    }

    [Fact]
    public void AddsProxiesForDictInterface ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Foo (IDictionary<string, int> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IDictionary<global::System.String, global::System.Int32>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>)");
    }

    [Fact]
    public void AddsProxiesForReadOnlyDictInterface ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Foo (IReadOnlyDictionary<string, int[]> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IReadOnlyDictionary<global::System.String, global::System.Int32[]>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32[]>)");
    }

    [Fact]
    public void DoesntAddProxiesForTaskWithoutResult ()
    {
        AddAssembly(WithClass("[JSInvokable] public static Task Foo (Task bar) => default;"));
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void AddsProxiesForTaskWithResult ()
    {
        AddAssembly(
            With("public record Info;"),
            WithClass("[JSInvokable] public static Task<Info> Foo () => default;"),
            WithClass("[JSInvokable] public static Task<IReadOnlyList<bool>> Bar () => default;"));
        Execute();
        Contains("[JsonSerializable(typeof((global::Info, byte))");
        Contains("[JsonSerializable(typeof((global::System.Collections.Generic.IReadOnlyList<global::System.Boolean>, byte))");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.List<global::System.Boolean>)");
        Contains("[JsonSerializable(typeof(global::System.Boolean[])");
    }
}
