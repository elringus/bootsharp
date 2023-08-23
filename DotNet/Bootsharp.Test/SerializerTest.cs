using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using static Bootsharp.Serializer;

namespace Bootsharp.Test;

public class SerializerTest
{
    [Fact]
    public void CanSerialize ()
    {
        Assert.Equal("{\"Items\":[{\"Id\":\"foo\"},{\"Id\":\"bar\"}]}",
            Serialize(new MockRecord(new MockItem[] { new("foo"), new("bar") })));
    }

    [Fact]
    public void CanDeserialize ()
    {
        Assert.Equal(new MockItem[] { new("foo"), new("bar") },
            ((MockRecord)Deserialize("{\"Items\":[{\"Id\":\"foo\"},{\"Id\":\"bar\"}]}", typeof(MockRecord))).Items);
    }

    [Fact]
    public void WhenDeserializationFailsErrorIsThrown ()
    {
        Assert.Throws<JsonException>(() => Deserialize("", typeof(int)));
    }

    [Fact]
    public void CanSerializeArgs ()
    {
        var args = new object[] { new MockRecord(new MockItem[] { new("foo") }), new MockItem[] { new("bar"), new("nya") }, "baz" };
        var serialized = SerializeArgs(args);
        Assert.Equal("{\"Items\":[{\"Id\":\"foo\"}]}", serialized[0]);
        Assert.Equal("[{\"Id\":\"bar\"},{\"Id\":\"nya\"}]", serialized[1]);
        Assert.Equal("\"baz\"", serialized[2]);
    }

    [Fact]
    public void CanDeserializeArgs ()
    {
        var @params = typeof(MockClass).GetMethod(nameof(MockClass.Copy))!.GetParameters();
        var args = new[] { "{\"Items\":[{\"Id\":\"foo\"}]}", "[{\"Id\":\"bar\"},{\"Id\":\"nya\"}]", "\"baz\"" };
        var deserialized = DeserializeArgs(@params, args);
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
            Assert.Throws<Error>(() => DeserializeArgs(@params, args)).Message);
    }

    [Fact]
    public void RespectsOptions ()
    {
        Assert.Equal("{\"Enum\":0}", Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{\"Enum\":null}", Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, ((MockItemWithEnum)Deserialize("{\"Enum\":0}", typeof(MockItemWithEnum))).Enum);
        Assert.Null(((MockItemWithEnum)Deserialize("{\"Enum\":null}", typeof(MockItemWithEnum))).Enum);
        Options = new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        Assert.Equal("{\"Enum\":\"Foo\"}", Serialize(new MockItemWithEnum(MockEnum.Foo)));
        Assert.Equal("{}", Serialize(new MockItemWithEnum(null)));
        Assert.Equal(MockEnum.Foo, ((MockItemWithEnum)Deserialize("{\"Enum\":\"Foo\"}", typeof(MockItemWithEnum))).Enum);
        Assert.Null(((MockItemWithEnum)Deserialize("{}", typeof(MockItemWithEnum))).Enum);
    }
}
