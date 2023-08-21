using Xunit;

namespace Bootsharp.Test;

public class JSTypesTest
{
    private interface IMock { }

    [Fact]
    public void ExportParametersEqualArguments ()
    {
        var types = new[] { typeof(IMock) };
        var attribute = new JSExportAttribute(types, "foo", "bar", "nya", "far");
        Assert.Equal(types, attribute.Types);
        Assert.Equal("foo", attribute.NamePattern);
        Assert.Equal("bar", attribute.NameReplacement);
        Assert.Equal("nya", attribute.InvokePattern);
        Assert.Equal("far", attribute.InvokeReplacement);
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        var types = new[] { typeof(IMock) };
        var attribute = new JSImportAttribute(types, "foo", "bar", "nya", "far");
        Assert.Equal(types, attribute.Types);
        Assert.Equal("foo", attribute.NamePattern);
        Assert.Equal("bar", attribute.NameReplacement);
        Assert.Equal("nya", attribute.InvokePattern);
        Assert.Equal("far", attribute.InvokeReplacement);
    }
}
