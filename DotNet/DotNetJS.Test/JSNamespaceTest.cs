using System.Linq;
using System.Reflection;
using DotNetJS;
using Xunit;

[assembly: JSNamespace("pattern", "replacement")]

namespace DotNetJS.Test;

public class JSNamespaceTest
{
    private readonly CustomAttributeData attribute = GetAttributeData();

    [Fact]
    public void ParametersEqualArguments ()
    {
        var attribute = new JSNamespaceAttribute("foo", "bar", true);
        Assert.Equal("foo", attribute.Pattern);
        Assert.Equal("bar", attribute.Replacement);
        Assert.True(attribute.AppendType);
    }

    [Fact]
    public void PatternParameterIsTheFirstPositionalArgument ()
    {
        Assert.Equal("pattern", attribute.ConstructorArguments[0].Value);
    }

    [Fact]
    public void ReplacementParameterIsTheSecondPositionalArgument ()
    {
        Assert.Equal("replacement", attribute.ConstructorArguments[1].Value);
    }

    [Fact]
    public void AppendTypeParameterIsTheThirdPositionalArgument ()
    {
        Assert.False((bool)attribute.ConstructorArguments[2].Value!);
    }

    private static CustomAttributeData GetAttributeData ()
    {
        return typeof(JSNamespaceTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(JSNamespaceAttribute));
    }
}
