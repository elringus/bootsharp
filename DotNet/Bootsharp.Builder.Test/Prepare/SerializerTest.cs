namespace Bootsharp.Builder.Test;

public class SerializerTest : PrepareTest
{
    protected override string TestedContent => GeneratedSerializer;

    [Fact]
    public void WhenNothingInspectedHasOnlyString ()
    {
        Execute();
        Contains(
            """
            using System.Text.Json;
            using System.Text.Json.Serialization;

            namespace Bootsharp;

            [JsonSerializable(typeof(global::System.String), TypeInfoPropertyName = "System_String")]
            internal partial class SerializerContext : JsonSerializerContext
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void InjectTypeInfoResolver ()
                {
                    Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                }
            }
            """);
    }

    [Fact]
    public void WhenNoCrawledTypesHasOnlyString ()
    {
        AddAssembly(With("[JSInvokable] public static bool? Foo (string a, int b, double[] c) => default;"));
        Execute();
        Contains(
            """
            using System.Text.Json;
            using System.Text.Json.Serialization;

            namespace Bootsharp;

            [JsonSerializable(typeof(global::System.String), TypeInfoPropertyName = "System_String")]
            internal partial class SerializerContext : JsonSerializerContext
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void InjectTypeInfoResolver ()
                {
                    Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                }
            }
            """);
    }

    [Fact]
    public void AddsCrawledTypes ()
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
        Contains(
            """
            using System.Text.Json;
            using System.Text.Json.Serialization;

            namespace Bootsharp;

            [JsonSerializable(typeof(global::n.Baz), TypeInfoPropertyName = "n_Baz")]
            [JsonSerializable(typeof(global::n.MockClass+Bar), TypeInfoPropertyName = "n_MockClass_Bar")]
            [JsonSerializable(typeof(global::n.ReadonlyRecordStruct), TypeInfoPropertyName = "n_ReadonlyRecordStruct")]
            [JsonSerializable(typeof(global::n.RecordClass), TypeInfoPropertyName = "n_RecordClass")]
            [JsonSerializable(typeof(global::n.Struct), TypeInfoPropertyName = "n_Struct")]
            [JsonSerializable(typeof(global::n.ReadonlyStruct), TypeInfoPropertyName = "n_ReadonlyStruct")]
            [JsonSerializable(typeof(global::n.Foo), TypeInfoPropertyName = "n_Foo")]
            [JsonSerializable(typeof(global::n.Enum), TypeInfoPropertyName = "n_Enum")]
            [JsonSerializable(typeof(global::System.String), TypeInfoPropertyName = "System_String")]
            internal partial class SerializerContext : JsonSerializerContext
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void InjectTypeInfoResolver ()
                {
                    Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                }
            }
            """);
    }
}
