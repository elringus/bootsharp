using System.Text.Json;
using System.Text.Json.Serialization;
using static Bootsharp.Serializer;

namespace Bootsharp.Common.Test;

public partial class SerializerTest
{
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int?))]
    [JsonSerializable(typeof(MockItem))]
    [JsonSerializable(typeof(MockRecord))]
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    )]
    internal partial class SerializerContext : JsonSerializerContext;

    [Fact]
    public void CanSerialize ()
    {
        Assert.Equal("""{"items":[{"id":"foo"},{"id":"bar"}]}""",
            Serialize(new MockRecord([new("foo"), new("bar")]), SerializerContext.Default.MockRecord));
    }

    [Fact]
    public void SerializesNullAsNull ()
    {
        Assert.Equal("null", Serialize(null, SerializerContext.Default.MockRecord));
    }

    [Fact]
    public void CanDeserialize ()
    {
        Assert.Equal([new("foo"), new("bar")],
            Deserialize("""{"items":[{"id":"foo"},{"id":"bar"}]}""", SerializerContext.Default.MockRecord).Items);
    }

    [Fact]
    public void DeserializesNullAndUndefinedAsDefault ()
    {
        Assert.Null(Deserialize(null, SerializerContext.Default.MockItem));
        Assert.Null(Deserialize("null", SerializerContext.Default.MockItem));
        Assert.Null(Deserialize("undefined", SerializerContext.Default.MockItem));
        Assert.Null(Deserialize(null, SerializerContext.Default.NullableInt32));
        Assert.Null(Deserialize("null", SerializerContext.Default.NullableInt32));
        Assert.Null(Deserialize("undefined", SerializerContext.Default.NullableInt32));
        Assert.False(Deserialize(null, SerializerContext.Default.Boolean));
        Assert.False(Deserialize<bool>("null", SerializerContext.Default.Boolean));
        Assert.False(Deserialize<bool>("undefined", SerializerContext.Default.Boolean));
    }

    [Fact]
    public void WhenDeserializationFailsErrorIsThrown ()
    {
        Assert.Throws<JsonException>(() => Deserialize("", SerializerContext.Default.Int32));
    }
}
