using Xunit;

namespace Bootsharp.Test;

public class JSTypesTest
{
    private interface IMock;

    [Fact]
    public void ParametersAreNullByDefault ()
    {
        var attribute = new JSExportAttribute(new[] { typeof(IMock) });
        Assert.Null(attribute.NamePattern);
        Assert.Null(attribute.NameReplacement);
        Assert.Null(attribute.InvokePattern);
        Assert.Null(attribute.InvokeReplacement);
    }

    [Fact]
    public void CommonParametersEqualArguments ()
    {
        var types = new[] { typeof(IMock) };
        var attribute = new JSExportAttribute(types) {
            NamePattern = "NamePattern",
            NameReplacement = "NameReplacement",
            InvokePattern = "InvokePattern",
            InvokeReplacement = "InvokeReplacement"
        };
        Assert.Equal(types, attribute.Types);
        Assert.Equal("NamePattern", attribute.NamePattern);
        Assert.Equal("NameReplacement", attribute.NameReplacement);
        Assert.Equal("InvokePattern", attribute.InvokePattern);
        Assert.Equal("InvokeReplacement", attribute.InvokeReplacement);
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        var attribute = new JSImportAttribute(new[] { typeof(IMock) }) {
            EventPattern = "EventPattern",
            EventReplacement = "EventReplacement"
        };
        Assert.Equal("EventPattern", attribute.EventPattern);
        Assert.Equal("EventReplacement", attribute.EventReplacement);
    }
}
