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
            WithClass("[JSInvokable] public static bool? Foo (int a, char b, DateTime c, DateTimeOffset d) => default;")
        );
        Execute();
        DoesNotContain("Binary<");
    }

    [Fact]
    public void SerializesPrimitivesUnderUserType ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public struct Structure;
            public enum Enumeration { A, B }

            public record Node(
                bool Boolean,
                byte Byte,
                sbyte SByte,
                short Int16,
                ushort UInt16,
                uint UInt32,
                long Int64,
                ulong UInt64,
                float Single,
                decimal Decimal,
                char Char,
                string String,
                DateTime DateTime,
                DateTimeOffset DateTimeOffset,
                nint NInt,
                int Int,
                int? NullableInt,
                Structure Struct,
                Structure? NullableStruct,
                Enumeration Enum,
                Enumeration? NullableEnum);

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("Binary<global::System.Boolean> System_Boolean = Serializer.Boolean");
        Contains("Binary<global::System.Byte> System_Byte = Serializer.Byte");
        Contains("Binary<global::System.SByte> System_SByte = Serializer.SByte");
        Contains("Binary<global::System.Int16> System_Int16 = Serializer.Int16");
        Contains("Binary<global::System.UInt16> System_UInt16 = Serializer.UInt16");
        Contains("Binary<global::System.UInt32> System_UInt32 = Serializer.UInt32");
        Contains("Binary<global::System.Int64> System_Int64 = Serializer.Int64");
        Contains("Binary<global::System.UInt64> System_UInt64 = Serializer.UInt64");
        Contains("Binary<global::System.Single> System_Single = Serializer.Single");
        Contains("Binary<global::System.Decimal> System_Decimal = Serializer.Decimal");
        Contains("Binary<global::System.Char> System_Char = Serializer.Char");
        Contains("Binary<global::System.String> System_String = Serializer.String");
        Contains("Binary<global::System.DateTime> System_DateTime = Serializer.DateTime");
        Contains("Binary<global::System.DateTimeOffset> System_DateTimeOffset = Serializer.DateTimeOffset");
        Contains("Binary<global::System.IntPtr> System_IntPtr = Serializer.IntPtr");
        Contains("Binary<global::System.Int32> System_Int32 = Serializer.Int32");
        Contains("Binary<global::System.Int32?> System_Int32OrNull = Serializer.Nullable(System_Int32)");
        Contains("Binary<global::Space.Structure> Space_Structure = new(Write_Space_Structure, Read_Space_Structure)");
        Contains("Binary<global::Space.Structure?> Space_StructureOrNull = Serializer.Nullable(Space_Structure)");
        Contains("Binary<global::Space.Enumeration> Space_Enumeration = Serializer.Enum<global::Space.Enumeration>()");
        Contains("Binary<global::Space.Enumeration?> Space_EnumerationOrNull = Serializer.Nullable(Space_Enumeration)");
    }

    [Fact]
    public void IgnoresNullableReferenceTypes ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Record;

            public record Node(
                Record Rec,
                Record? NullableRec,
                object Object,
                object? NullableObject);

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Once("Binary<global::Space.Record>");
        Once("Binary<global::System.Object>");
        DoesNotContain("Binary<global::Space.Record?>");
        DoesNotContain("Binary<global::System.Object?>");
    }

    [Fact]
    public void SerializesSelfReferencedTypes ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Node (string Id, Node? Parent, Node? Child);

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("Binary<global::Space.Node>");
    }

    [Fact]
    public void SerializesCollections ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Node(
                List<int> ListItems,
                IList<int> ListInterfaceItems,
                IReadOnlyList<int> ReadOnlyListItems,
                ICollection<int> CollectionItems,
                IReadOnlyCollection<int> ReadOnlyCollectionItems,
                Dictionary<string, DateTime> Map,
                IDictionary<string, DateTimeOffset> MapInterface,
                IReadOnlyDictionary<string, Node?> ReadOnlyMap);

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
                [JSInvokable] public static Task<byte[]> EchoBytesAsync (byte[] bytes) => default;
            }
            """));
        Execute();
        Contains("Binary<global::System.Collections.Generic.List<global::System.Int32>>");
        Contains("Binary<global::System.Collections.Generic.IList<global::System.Int32>>");
        Contains("Binary<global::System.Collections.Generic.IReadOnlyList<global::System.Int32>>");
        Contains("Binary<global::System.Collections.Generic.ICollection<global::System.Int32>>");
        Contains("Binary<global::System.Collections.Generic.IReadOnlyCollection<global::System.Int32>>");
        Contains("Binary<global::System.Collections.Generic.Dictionary<global::System.String, global::System.DateTime>>");
        Contains("Binary<global::System.Collections.Generic.IDictionary<global::System.String, global::System.DateTimeOffset>>");
        Contains("Binary<global::System.Collections.Generic.IReadOnlyDictionary<global::System.String, global::Space.Node>>");
        Contains("Binary<global::System.Byte[]>");
    }

    [Fact]
    public void OrdersNestedCollectionsAfterElements ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public class Item
            {
                public string? Value { get; init; }
            }

            public record Info(Item?[]?[]? Items);

            public class Class
            {
                [JSInvokable] public static Info Echo (Info info) => info;
            }
            """));
        Execute();
        var itemIdx = TestedContent.IndexOf("Space_Item", StringComparison.Ordinal);
        var innerArrayIdx = TestedContent.IndexOf("Space_ItemArray", StringComparison.Ordinal);
        var outerArrayIdx = TestedContent.IndexOf("Space_ItemArrayArray", StringComparison.Ordinal);
        Assert.True(itemIdx >= 0, "Expected item serializer.");
        Assert.True(innerArrayIdx > itemIdx, "Expected inner array serializer after item serializer.");
        Assert.True(outerArrayIdx > innerArrayIdx, "Expected outer array serializer after inner array serializer.");
    }

    [Fact]
    public void OrdersSelfReferencedElementBeforeCollection ()
    {
        AddAssembly(With(
            """
            public record Node (List<Node>? Children);

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        var nodeIdx = TestedContent.IndexOf("Binary<global::Node>", StringComparison.Ordinal);
        var listIdx = TestedContent.IndexOf("Binary<global::System.Collections.Generic.List<global::Node>>", StringComparison.Ordinal);
        Assert.True(nodeIdx >= 0 && listIdx >= 0);
        Assert.True(nodeIdx < listIdx);
    }

    [Fact]
    public void UsesParameterizedConstructorForGetterOnlyProperties ()
    {
        AddAssembly(With(
            """
            public class Node
            {
                public Node (string id) => Id = id;
                public string Id { get; }
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("new global::Node(@id);");
    }

    [Fact]
    public void UsesParameterlessConstructorForWritablePropertiesWhenAvailable ()
    {
        AddAssembly(With(
            """
            public class Node
            {
                public Node () { }
                public Node (string id) => Id = id;
                public string Id { get; set; } = string.Empty;
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("var _value_ = new global::Node();");
        Contains("_value_.Id = @id;");
    }

    [Fact]
    public void UsesObjectInitializerForPublicInitOnlyProperties ()
    {
        AddAssembly(With(
            """
            public class Node
            {
                public string Id { get; init; } = string.Empty;
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("new global::Node() { Id = @id };");
    }

    [Fact]
    public void DoesNotAssignConstructorBoundPropertiesTwice ()
    {
        AddAssembly(With(
            """
            public class RecordA (string Id)
            {
                public string Id { get; init; } = Id;
            }

            public class RecordB
            {
                public string Id { get; set; }

                public RecordB (string id) => Id = id;
            }

            public class Class
            {
                [JSInvokable] public static RecordA A (RecordA a) => a;
                [JSInvokable] public static RecordB B (RecordB b) => b;
            }
            """));
        Execute();
        Contains("new global::RecordA(@id);");
        Contains("new global::RecordB(@id);");
        DoesNotContain("{ Id = @id }");
        DoesNotContain("_value_.Id = @id;");
    }

    [Fact]
    public void UsesObjectInitializerForRequiredMembers ()
    {
        AddAssembly(With(
            """
            public readonly record struct CompletionItem
            {
                public required string Label { get; init; }
                public string? Detail { get; init; }
            }

            public class Class
            {
                [JSInvokable] public static CompletionItem Echo (CompletionItem item) => item;
            }
            """));
        Execute();
        Contains("new global::CompletionItem() { Label = @label, Detail = @detail };");
    }

    [Fact]
    public void AssignsRequiredWritableMembersAfterConstruction ()
    {
        AddAssembly(With(
            """
            public class CompletionItem
            {
                public CompletionItem () { }
                public required string Label { get; set; }
            }

            public class Class
            {
                [JSInvokable] public static CompletionItem Echo (CompletionItem item) => item;
            }
            """));
        Execute();
        Contains("new global::CompletionItem() { Label = @label }");
    }

    [Fact]
    public void UsesBackingFieldAssignmentWhenConstructorParameterNameDoesNotMatchProperty ()
    {
        AddAssembly(With(
            """
            public struct Node
            {
                public Node (string other) => Id = other;
                public string Id { get; }
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("<Id>k__BackingField");
    }

    [Fact]
    public void UsesBackingFieldAssignmentWhenConstructorParameterTypeDoesNotMatchProperty ()
    {
        AddAssembly(With(
            """
            public struct Node
            {
                public Node (int id) => Id = id.ToString();
                public string Id { get; }
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("<Id>k__BackingField");
    }

    [Fact]
    public void SerializersComputedProperties ()
    {
        AddAssembly(With(
            """
            public record Node
            {
                public string Id { get; set; }
                public string Computed => Id + "something";
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("System_String.Write(ref writer, value.Computed);");
    }

    [Fact]
    public void IgnoresWriteOnlyProperties ()
    {
        AddAssembly(With(
            """
            public record Node
            {
                public string WriteOnly { set { } }
            }

            public class Class
            {
                [JSInvokable] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        DoesNotContain("WriteOnly");
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
        Contains("Binary<global::RecordA> ");
        Contains("Binary<global::RecordB> ");
        Contains("Binary<global::RecordC> ");
        Contains("Binary<global::RecordA[]> ");
        Contains("Binary<global::RecordB[]> ");
        Contains("Binary<global::RecordC[]> ");
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
        Contains("Binary<global::RecordA>");
        Contains("Binary<global::RecordB>");
        Contains("Binary<global::RecordC>");
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
        DoesNotContain("Binary<");
    }

    [Fact]
    public void DoesntGenerateDuplicateSerializers ()
    {
        AddAssembly("Foo.Bar.dll", With(
            """
            namespace Foo.Bar;

            public record Record (IRegistry R);

            public interface IRegistry
            {
                Record GetRecord ();
                IReadOnlyList<Record> GetRecords ();
                IReadOnlyDictionary<string, Record> GetRecordMap ();
            }
            """));
        AddAssembly("Foo.dll", With(
            """
            using Foo.Bar;

            namespace Foo;

            public class Class
            {
                [JSFunction] public static IRegistry GetRegistry () => default;
                [JSInvokable] public static Record Echo (Record r) => default;
                [JSInvokable] public static Task<IReadOnlyList<Record?>> List (IReadOnlyList<Record?> r) => default;
                [JSInvokable] public static Task<IReadOnlyDictionary<string, Record?>> Map (IReadOnlyDictionary<string, Record?> r) => default;
            }
            """));
        Execute();
        Once("Binary<global::Foo.Bar.Record>");
        Once("Binary<global::System.Collections.Generic.IReadOnlyList<global::Foo.Bar.Record>>");
        Once("Binary<global::System.Collections.Generic.IReadOnlyDictionary<global::System.String, global::Foo.Bar.Record>>");
    }

    [Fact]
    public void SerializesAllTheCrawledSerializableTypes ()
    {
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
        Contains("Binary<global::y.Enum> ");
        Contains("Binary<global::n.Enum> ");
        Contains("Binary<global::y.Struct> ");
        Contains("Binary<global::n.Struct> ");
        Contains("Binary<global::n.ReadonlyStruct> ");
        Contains("Binary<global::y.ReadonlyStruct> ");
        Contains("Binary<global::n.ReadonlyStruct[]> ");
        Contains("Binary<global::y.ReadonlyStruct[]> ");
    }
}
