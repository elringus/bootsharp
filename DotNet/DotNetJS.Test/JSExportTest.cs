using Xunit;

namespace DotNetJS.Test;

public class JSExportTest
{
    private interface IMock { }

    [Fact]
    public void ParameterEqualArgument ()
    {
        var types = new[] { typeof(IMock) };
        var attribute = new JSExportAttribute(types);
        Assert.Equal(types, attribute.Types);
    }
}
