using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static Bootsharp.Serializer;

namespace Bootsharp.Common.Test;

public enum MockEnum { Foo, Bar }
public record MockItem (string Id);
public record MockItemWithEnum (MockEnum? Enum);
public record MockRecord (IReadOnlyList<MockItem> Items);

public class SerializerTest
{
    public SerializerTest ()
    {
        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    [Fact]
    public void WhenInfoResolverNotAssignedThrowsError ()
    {
        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            TypeInfoResolver = null
        };
        Assert.Contains("Serializer info resolver is not assigned",
            Assert.Throws<Error>(() => Serialize("")).Message);
    }

    [Fact]
    public void WhenTypeInfoNotAvailableThrowsError ()
    {
        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            TypeInfoResolver = new MockResolver()
        };
        Assert.Contains("Failed to resolve serializer info",
            Assert.Throws<Error>(() => Serialize("")).Message);
    }

    [Fact]
    public void CanSerialize ()
    {
        Assert.Equal("""{"items":[{"id":"foo"},{"id":"bar"}]}""",
            Serialize(new MockRecord(new MockItem[] { new("foo"), new("bar") })));
    }

    [Fact]
    public void SerializesNullAsNull ()
    {
        Assert.Equal("null", Serialize(null));
    }

    [Fact]
    public void CanDeserialize ()
    {
        Assert.Equal(new MockItem[] { new("foo"), new("bar") },
            Deserialize<MockRecord>("""{"items":[{"id":"foo"},{"id":"bar"}]}""").Items);
    }

    [Fact]
    public void DeserializesNullAndUndefinedAsDefault ()
    {
        Assert.Null(Deserialize<MockItem>(null));
        Assert.Null(Deserialize<MockItem>("null"));
        Assert.Null(Deserialize<MockItem>("undefined"));
        Assert.Null(Deserialize<int?>(null));
        Assert.Null(Deserialize<int?>("null"));
        Assert.Null(Deserialize<int?>("undefined"));
        Assert.False(Deserialize<bool>(null));
        Assert.False(Deserialize<bool>("null"));
        Assert.False(Deserialize<bool>("undefined"));
    }

    [Fact]
    public void WhenDeserializationFailsErrorIsThrown ()
    {
        Assert.Throws<JsonException>(() => Deserialize<int>(""));
    }

    [Fact]
    public void RespectsOptions ()
    {
        Assert.Equal("{\"enum\":0}", Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{\"enum\":null}", Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, Deserialize<MockItemWithEnum>("{\"enum\":0}").Enum);
        Assert.Null((Deserialize<MockItemWithEnum>("{\"enum\":null}")).Enum);
        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        Assert.Equal("{\"enum\":\"Foo\"}", Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{}", Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, (Deserialize<MockItemWithEnum>("{\"enum\":\"Foo\"}")).Enum);
        Assert.Null((Deserialize<MockItemWithEnum>("{}")).Enum);
    }
}
