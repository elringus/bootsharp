using System.Text.Json;
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
}
