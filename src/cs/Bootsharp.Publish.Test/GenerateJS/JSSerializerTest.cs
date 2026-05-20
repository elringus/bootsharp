namespace Bootsharp.Publish.Test;

public class JSSerializerTest : GenerateJSTest
{
    protected override string TestedContent { get => field ?? ReadProjectFile("generated/serializer.g.mjs") ?? ""; set; }

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
                [Export] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("$s.System_Boolean = $s.std.Boolean;");
        Contains("$s.System_Byte = $s.std.Byte;");
        Contains("$s.System_SByte = $s.std.SByte;");
        Contains("$s.System_Int16 = $s.std.Int16;");
        Contains("$s.System_UInt16 = $s.std.UInt16;");
        Contains("$s.System_UInt32 = $s.std.UInt32;");
        Contains("$s.System_Int64 = $s.std.Int64;");
        Contains("$s.System_UInt64 = $s.std.UInt64;");
        Contains("$s.System_Single = $s.std.Single;");
        Contains("$s.System_Decimal = $s.std.Decimal;");
        Contains("$s.System_Char = $s.std.Char;");
        Contains("$s.System_String = $s.std.String;");
        Contains("$s.System_DateTime = $s.std.DateTime;");
        Contains("$s.System_DateTimeOffset = $s.std.DateTimeOffset;");
        Contains("$s.System_IntPtr = $s.std.IntPtr;");
        Contains("$s.System_Int32 = $s.std.Int32;");
        Contains("$s.System_Int32OrNull = $s.std.Nullable($s.System_Int32);");
        Contains("$s.Space_Structure = $s.binary(write_Space_Structure, read_Space_Structure);");
        Contains("$s.Space_StructureOrNull = $s.std.Nullable($s.Space_Structure);");
        Contains("$s.Space_Enumeration = $s.std.Int32;");
        Contains("$s.Space_EnumerationOrNull = $s.std.Nullable($s.Space_Enumeration);");
    }

    [Fact]
    public void OrdersSelfReferencedElementBeforeCollection ()
    {
        AddAssembly(With(
            """
            public record Node (List<Node>? Children);

            public class Class
            {
                [Export] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains(
            """
            $s.Node = $s.binary(write_Node, read_Node);
            $s.System_Collections_Generic_List_Of_Node = $s.std.List($s.Node);
            """);
    }

    [Fact]
    public void SerializerConstantsLiveInSerializerFile ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);
            public class Class { [Export] public static Info Echo (Info i) => i; }
            """));
        Execute();
        Contains("$s.Info = ");
        DoesNotContain("index.g.mjs", "$s.Info = ");
    }

    [Fact]
    public void SerializedImportedInstanceImporterIsReferencedFromSerializerFile ()
    {
        AddAssembly(With(
            """
            public interface IImported { event Action? Changed; }
            public record Wrapper (IImported Inner);
            public class Class { [Export] public static Wrapper Echo (Wrapper w) => w; }
            """));
        Execute();
        Contains("$i.import_IImported");
    }
}
