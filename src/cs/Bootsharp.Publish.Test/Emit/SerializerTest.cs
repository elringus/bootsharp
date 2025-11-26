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
    public void SerializesTypesFromInteropMethods ()
    {
        AddAssembly(With(
            """
            public record RecordA;
            public record RecordB;
            public record RecordC;

            public class Class
            {
                [JSInvokable] public static Task<RecordA[]> A (RecordC c) => default;
                [JSFunction] public static RecordB[] B (RecordC[] c) => default;
            }
            """));
        Execute();
        Contains("[JsonSerializable(typeof(global::RecordA)");
        Contains("[JsonSerializable(typeof(global::RecordB)");
        Contains("[JsonSerializable(typeof(global::RecordC)");
        Contains("[JsonSerializable(typeof(global::RecordA[])");
        Contains("[JsonSerializable(typeof(global::RecordB[])");
        Contains("[JsonSerializable(typeof(global::RecordC[])");
    }

    [Fact]
    public void SerializesTypesFromInteropInterfaces ()
    {
        AddAssembly(With(
            """
            public record RecordA;
            public record RecordB;
            public record RecordC;
            public interface IExported { void Inv (RecordA a); }
            public interface IImported { void Fun (RecordB b); void NotifyEvt(RecordC c); }

            public class Class
            {
                [JSFunction] public static Task<IImported> GetImported (IExported arg) => default;
            }
            """));
        Execute();
        Contains("[JsonSerializable(typeof(global::RecordA)");
        Contains("[JsonSerializable(typeof(global::RecordB)");
        Contains("[JsonSerializable(typeof(global::RecordC)");
    }

    [Fact]
    public void DoesntSerializeInstancedInteropInterfacesThemselves ()
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

    [Fact]
    public void SerializesAllTheCrawledSerializableTypes ()
    {
        // .NET's generator indexes types by short names (w/o namespace) and fails on duplicates, so we have to add everything ourselves.
        // https://github.com/dotnet/runtime/issues/58938#issuecomment-1306731801
        AddAssembly(
            With("y", "public enum Enum { A, B }"),
            With("y", "public record Struct (double A, ReadonlyStruct[]? B);"),
            With("y", "public record ReadonlyStruct (Enum e);"),
            With("n", "public struct Struct { public y.Struct S { get; set; } public ReadonlyStruct[]? A { get; set; } }"),
            With("n", "public readonly struct ReadonlyStruct { public double A { get; init; } }"),
            With("n", "public readonly record struct ReadonlyRecordStruct(double A);"),
            With("n", "public record class RecordClass(double A);"),
            With("n", "public enum Enum { A, B }"),
            With("n", "public class Foo { public Struct S { get; } public ReadonlyStruct Rs { get; } }"),
            WithClass("n", "public class Bar : Foo { public ReadonlyRecordStruct Rrs { get; } public RecordClass Rc { get; } }"),
            With("n", "public class Baz { public List<Class.Bar?> Bars { get; } }"),
            WithClass("n", "[JSInvokable] public static Task<Baz?> GetBaz (Enum e) => default;"));
        Execute();
        Contains("[JsonSerializable(typeof(global::y.Enum)");
        Contains("[JsonSerializable(typeof(global::n.Enum)");
        Contains("[JsonSerializable(typeof(global::y.Struct)");
        Contains("[JsonSerializable(typeof(global::n.Struct)");
        Contains("[JsonSerializable(typeof(global::n.ReadonlyStruct)");
        Contains("[JsonSerializable(typeof(global::y.ReadonlyStruct)");
        Contains("[JsonSerializable(typeof(global::n.ReadonlyStruct[])");
        Contains("[JsonSerializable(typeof(global::y.ReadonlyStruct[])");
    }
}
