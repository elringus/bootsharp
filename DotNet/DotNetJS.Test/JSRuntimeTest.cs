using System.Text.Json.Serialization;
using Xunit;

namespace DotNetJS.Test;

public class JSRuntimeTest
{
    [Fact]
    public void ConfigureJsonAffectsInboundAndOutboundOptions ()
    {
        var runtime = new JSRuntime();
        runtime.ConfigureJson(o => o.NumberHandling = JsonNumberHandling.WriteAsString);
        Assert.Equal(JsonNumberHandling.WriteAsString, runtime.InboundJsonOptions.NumberHandling);
        Assert.Equal(JsonNumberHandling.WriteAsString, runtime.OutboundJsonOptions.NumberHandling);
    }
}
