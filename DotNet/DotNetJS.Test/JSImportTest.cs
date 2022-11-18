using Xunit;

namespace DotNetJS.Test;

public class JSImportTest
{
    private interface IMock { }

    [Fact]
    public void ParameterEqualArgument ()
    {
        var types = new[] { typeof(IMock) };
        var attribute = new JSImportAttribute(types);
        Assert.Equal(types, attribute.Types);
    }
}
