using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Bootsharp.Test;

public class SerializerTest
{
    private readonly Serializer serializer = new();

    [Fact]
    public void CanSerialize ()
    {
        Assert.Equal("{\"Items\":[{\"Id\":\"foo\"},{\"Id\":\"bar\"}]}",
            serializer.Serialize(new MockRecord(new MockItem[] { new("foo"), new("bar") })));
    }

    [Fact]
    public void CanDeserialize ()
    {
        Assert.Equal(new MockItem[] { new("foo"), new("bar") },
            ((MockRecord)serializer.Deserialize("{\"Items\":[{\"Id\":\"foo\"},{\"Id\":\"bar\"}]}", typeof(MockRecord))).Items);
    }

    [Fact]
    public void WhenDeserializationFailsErrorIsThrown ()
    {
        Assert.Throws<JsonException>(() => serializer.Deserialize("", typeof(int)));
    }

    [Fact]
    public void CanDeserializeArgs ()
    {
        var @params = typeof(MockClass).GetMethod(nameof(MockClass.Copy))!.GetParameters();
        var args = new[] { "{\"Items\":[{\"Id\":\"foo\"}]}", "[{\"Id\":\"bar\"},{\"Id\":\"nya\"}]", "\"baz\"" };
        var deserialized = serializer.DeserializeArgs(args, @params);
        Assert.Equal(new MockItem[] { new("foo") }, ((MockRecord)deserialized[0]).Items);
        Assert.Equal(new MockItem[] { new("bar"), new("nya") }, deserialized[1]);
        Assert.Equal("baz", deserialized[2]);
    }

    [Fact]
    public void WhenArgLengthIsAboveParamLengthErrorIsThrown ()
    {
        var @params = typeof(MockClass).GetMethod(nameof(MockClass.Do))!.GetParameters();
        var args = new[] { "\"baz\"" };
        Assert.Contains("the method doesn't accept as many arguments",
            Assert.Throws<Error>(() => serializer.DeserializeArgs(args, @params)).Message);
    }

    [Fact]
    public void RespectsOptions ()
    {
        Assert.Equal("{\"Enum\":0}", serializer.Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{\"Enum\":null}", serializer.Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, ((MockItemWithEnum)serializer.Deserialize("{\"Enum\":0}", typeof(MockItemWithEnum))).Enum);
        Assert.Null(((MockItemWithEnum)serializer.Deserialize("{\"Enum\":null}", typeof(MockItemWithEnum))).Enum);
        Serializer.Options = new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        Assert.Equal("{\"Enum\":\"Foo\"}", serializer.Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{}", serializer.Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, ((MockItemWithEnum)serializer.Deserialize("{\"Enum\":\"Foo\"}", typeof(MockItemWithEnum))).Enum);
        Assert.Null(((MockItemWithEnum)serializer.Deserialize("{}", typeof(MockItemWithEnum))).Enum);
    }
}
