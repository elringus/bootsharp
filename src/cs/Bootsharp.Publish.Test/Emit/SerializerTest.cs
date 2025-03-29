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
        DoesNotContain("JsonSerializable");
    }

    [Fact]
    public void DoesntSerializeInstancedInteropInterfaces ()
    {
        AddAssembly(With(
            """
            namespace Space
            {
                public interface IExported { void Inv (); }
                public interface IImported { void Fun (); void NotifyEvt(); }
            }

            public interface IExported { void Inv (); }
            public interface IImported { void Fun (); void NotifyEvt(); }

            public class Class
            {
                [JSInvokable] public static Space.IExported GetExported (Space.IImported arg) => default;
                [JSFunction] public static Task<IImported> GetImported (IExported arg) => default;
            }
            """));
        Execute();
        DoesNotContain("JsonSerializable");
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
            WithClass("n", "[JSInvokable] public static Task<Baz?> GetBaz () => default;"));
        Execute();
        Assert.Equal(2, Matches("JsonSerializable").Count);
        Contains("[JsonSerializable(typeof(global::n.Baz)");
        Contains("[JsonSerializable(typeof(global::y.Struct)");
    }
}
