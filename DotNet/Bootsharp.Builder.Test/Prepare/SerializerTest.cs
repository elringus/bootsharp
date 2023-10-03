namespace Bootsharp.Builder.Test;

public class SerializerTest : PrepareTest
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
        AddAssembly(With("[JSInvokable] public static bool? Foo (string a, int b, double[] c) => default;"));
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void AssignsTypeInfoResolver ()
    {
        AddAssembly(
            With("n", "public record Info;", false),
            With("n", "[JSInvokable] public static void Foo (Info i) {}"));
        Execute();
        Contains(
            """
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void InjectTypeInfoResolver ()
                {
                    Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                }
            """);
    }

    [Fact]
    public void AddsOnlyTopLevelTypes ()
    {
        AddAssembly(
            With("n", "public struct Struct { public double A { get; set; } }", false),
            With("n", "public readonly struct ReadonlyStruct { public double A { get; init; } }", false),
            With("n", "public readonly record struct ReadonlyRecordStruct(double A);", false),
            With("n", "public record class RecordClass(double A);", false),
            With("n", "public enum Enum { A, B }", false),
            With("n", "public class Foo { public Struct S { get; } public ReadonlyStruct Rs { get; } }", false),
            With("n", "public class Bar : Foo { public ReadonlyRecordStruct Rrs { get; } public RecordClass Rc { get; } }"),
            With("n", "public class Baz { public List<MockClass.Bar?> Bars { get; } public Enum E { get; } }", false),
            With("n", "[JSInvokable] public static Baz? GetBaz () => default;"));
        Execute();
        Assert.Single(Matches("JsonSerializable"));
        Contains("[JsonSerializable(typeof(global::n.Baz)");
    }

    [Fact]
    public void AddsProxiesForListInterface ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo (IList<string> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IList<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.List<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.String[])");
    }

    [Fact]
    public void AddsProxiesForReadOnlyListInterface ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo (IReadOnlyList<string> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IReadOnlyList<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.List<global::System.String>)");
        Contains("[JsonSerializable(typeof(global::System.String[])");
    }

    [Fact]
    public void AddsProxiesForDictInterface ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo (IDictionary<string, int> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IDictionary<global::System.String, global::System.Int32>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>)");
    }

    [Fact]
    public void AddsProxiesForReadOnlyDictInterface ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo (IReadOnlyDictionary<string, int> a) {}"));
        Execute();
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IReadOnlyDictionary<global::System.String, global::System.Int32>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>)");
    }

    [Fact]
    public void DoesntAddProxiesForTaskWithoutResult ()
    {
        AddAssembly(With("[JSInvokable] public static Task Foo () => default;"));
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void AddsProxiesForTaskWithResult ()
    {
        AddAssembly(
            With("public record Info;", false),
            With("[JSInvokable] public static Task<Info> Foo () => default;"),
            With("[JSInvokable] public static Task<IReadOnlyList<bool>> Bar () => default;"));
        Execute();
        Contains("[JsonSerializable(typeof(global::Info)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.IReadOnlyList<global::System.Boolean>)");
        Contains("[JsonSerializable(typeof(global::System.Collections.Generic.List<global::System.Boolean>)");
        Contains("[JsonSerializable(typeof(global::System.Boolean[])");
    }
}
