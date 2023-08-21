using System.Text.Json.Serialization;
using Xunit;

namespace Bootsharp.Test;

public class JSRuntimeTest
{
    [Fact]
    public void ConfigureJsonAffectsSerializerOptions ()
    {
        var runtime = new JSRuntime();
        runtime.ConfigureJson(o => o.NumberHandling = JsonNumberHandling.WriteAsString);
        Assert.Equal(JsonNumberHandling.WriteAsString, runtime.JsonSerializerOptions.NumberHandling);
    }
}
