using System.Text.Json;
using System.Text.Json.Serialization;
using static Bootsharp.Serializer;

namespace Bootsharp.Test;

public enum MockEnum { Foo, Bar }
public record MockItem(string Id);
public record MockItemWithEnum(MockEnum? Enum);
public record MockRecord(IReadOnlyList<MockItem> Items);

public class SerializerTest
{
    [Fact]
    public void CanSerialize ()
    {
        Assert.Equal("""{"items":[{"id":"foo"},{"id":"bar"}]}""",
            Serialize(new MockRecord(new MockItem[] { new("foo"), new("bar") })));
    }

    [Fact]
    public void CanDeserialize ()
    {
        Assert.Equal(new MockItem[] { new("foo"), new("bar") },
            Deserialize<MockRecord>("""{"items":[{"id":"foo"},{"id":"bar"}]}""").Items);
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
            Converters = { new JsonStringEnumConverter() }
        };
        Assert.Equal("{\"enum\":\"Foo\"}", Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{}", Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, (Deserialize<MockItemWithEnum>("{\"enum\":\"Foo\"}")).Enum);
        Assert.Null((Deserialize<MockItemWithEnum>("{}")).Enum);
    }
}
