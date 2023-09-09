using System.Reflection;
using Bootsharp;

[assembly: JSNamespace("pattern", "replacement")]

namespace Bootsharp.Test;

public class NamespaceTest
{
    private readonly CustomAttributeData attribute = GetAttributeData();

    [Fact]
    public void ParametersEqualArguments ()
    {
        var attribute = new JSNamespaceAttribute("foo", "bar");
        Assert.Equal("foo", attribute.Pattern);
        Assert.Equal("bar", attribute.Replacement);
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

    private static CustomAttributeData GetAttributeData () =>
        typeof(NamespaceTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(JSNamespaceAttribute));
}
