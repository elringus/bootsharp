using static Bootsharp.Proxies;

namespace Bootsharp.Common.Test;

public class ProxiesTest
{
    [Fact]
    public void WhenEndpointNotFoundErrorIsThrown ()
    {
        Assert.Contains("Proxy 'foo' is not found.",
            Assert.Throws<Error>(() => Get<Action>("foo")).Message);
    }

    [Fact]
    public void WhenFunctionTypeIsWrongErrorIsThrown ()
    {
        Set("bar", null);
        Assert.Contains("Proxy 'bar' is not 'System.Action'.",
            Assert.Throws<Error>(() => Get<Action>("bar")).Message);
    }

    [Fact]
    public void CanSetAndGetDelegate ()
    {
        Set("echo", (int x, int y) => x + y);
        Assert.Equal(15, Get<Func<int, int, int>>("echo")(6, 9));
    }
}
