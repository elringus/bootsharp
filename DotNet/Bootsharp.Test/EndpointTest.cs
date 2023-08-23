using Xunit;
using static Bootsharp.EndpointBuilder;
using static Bootsharp.EndpointResolver;

namespace Bootsharp.Test;

public class EndpointTest
{
    [Fact]
    public void BuildsCorrectMethodEndpoint ()
    {
        Assert.Equal("Space/Class/Method", BuildMethod("Space", "Class", "Method"));
        Assert.Equal("Foo.Bar.Baz/Class.Nested/Method", BuildMethod("Foo.Bar.Baz", "Class.Nested", "Method"));
    }

    [Fact]
    public void BuildsCorrectFunctionEndpoint ()
    {
        Assert.Equal("Space/func", BuildFunction("Space", "func"));
        Assert.Equal("Foo.Bar.Baz/func", BuildFunction("Foo.Bar.Baz", "func"));
    }

    [Fact]
    public void CanResolveValidMethodEndpoint ()
    {
        Assert.Equal(("Space", "Class", "Method"), ResolveMethod("Space/Class/Method"));
        Assert.Equal(("Foo.Bar.Baz", "Class.Nested", "Method"), ResolveMethod("Foo.Bar.Baz/Class.Nested/Method"));
    }

    [Fact]
    public void ThrowsOnInvalidMethodEndpoint ()
    {
        Assert.Throws<Error>(() => ResolveMethod(""));
        Assert.Throws<Error>(() => ResolveMethod("/"));
        Assert.Throws<Error>(() => ResolveMethod("Foo"));
        Assert.Throws<Error>(() => ResolveMethod("Foo/Bar"));
        Assert.Throws<Error>(() => ResolveMethod("Foo/Bar/"));
        Assert.Throws<Error>(() => ResolveMethod("Foo//Nya"));
        Assert.Throws<Error>(() => ResolveMethod("Foo/ /Nya"));
        Assert.Throws<Error>(() => ResolveMethod("/Foo/Bar/Nya"));
    }

    [Fact]
    public void CanResolveValidFunctionEndpoint ()
    {
        Assert.Equal(("Space", "func"), ResolveFunction("Space/func"));
        Assert.Equal(("Foo.Bar.Baz", "func"), ResolveFunction("Foo.Bar.Baz/func"));
    }

    [Fact]
    public void ThrowsOnInvalidFunctionEndpoint ()
    {
        Assert.Throws<Error>(() => ResolveFunction(""));
        Assert.Throws<Error>(() => ResolveFunction("/"));
        Assert.Throws<Error>(() => ResolveFunction("Foo"));
        Assert.Throws<Error>(() => ResolveFunction("Foo/Bar/"));
        Assert.Throws<Error>(() => ResolveFunction("Foo//Nya"));
        Assert.Throws<Error>(() => ResolveFunction("Foo/ /Nya"));
        Assert.Throws<Error>(() => ResolveFunction("/Foo/Bar/Nya"));
    }
}
