using Xunit;

namespace Bootsharp.Test;

public class EndpointTest
{
    private readonly EndpointBuilder builder = new();
    private readonly EndpointResolver resolver = new();

    [Fact]
    public void BuildsCorrectMethodEndpoint ()
    {
        Assert.Equal("Space/Class/Method", builder.BuildMethod("Space", "Class", "Method"));
        Assert.Equal("Foo.Bar.Baz/Class.Nested/Method", builder.BuildMethod("Foo.Bar.Baz", "Class.Nested", "Method"));
    }

    [Fact]
    public void BuildsCorrectFunctionEndpoint ()
    {
        Assert.Equal("Space/func", builder.BuildFunction("Space", "func"));
        Assert.Equal("Foo.Bar.Baz/func", builder.BuildFunction("Foo.Bar.Baz", "func"));
    }

    [Fact]
    public void CanResolveValidMethodEndpoint ()
    {
        Assert.Equal(("Space", "Class", "Method"), resolver.ResolveMethod("Space/Class/Method"));
        Assert.Equal(("Foo.Bar.Baz", "Class.Nested", "Method"), resolver.ResolveMethod("Foo.Bar.Baz/Class.Nested/Method"));
    }

    [Fact]
    public void ThrowsOnInvalidMethodEndpoint ()
    {
        Assert.Throws<Error>(() => resolver.ResolveMethod(""));
        Assert.Throws<Error>(() => resolver.ResolveMethod("/"));
        Assert.Throws<Error>(() => resolver.ResolveMethod("Foo"));
        Assert.Throws<Error>(() => resolver.ResolveMethod("Foo/Bar"));
        Assert.Throws<Error>(() => resolver.ResolveMethod("Foo/Bar/"));
        Assert.Throws<Error>(() => resolver.ResolveMethod("Foo//Nya"));
        Assert.Throws<Error>(() => resolver.ResolveMethod("Foo/ /Nya"));
        Assert.Throws<Error>(() => resolver.ResolveMethod("/Foo/Bar/Nya"));
    }

    [Fact]
    public void CanResolveValidFunctionEndpoint ()
    {
        Assert.Equal(("Space", "func"), resolver.ResolveFunction("Space/func"));
        Assert.Equal(("Foo.Bar.Baz", "func"), resolver.ResolveFunction("Foo.Bar.Baz/func"));
    }

    [Fact]
    public void ThrowsOnInvalidFunctionEndpoint ()
    {
        Assert.Throws<Error>(() => resolver.ResolveFunction(""));
        Assert.Throws<Error>(() => resolver.ResolveFunction("/"));
        Assert.Throws<Error>(() => resolver.ResolveFunction("Foo"));
        Assert.Throws<Error>(() => resolver.ResolveFunction("Foo/Bar/"));
        Assert.Throws<Error>(() => resolver.ResolveFunction("Foo//Nya"));
        Assert.Throws<Error>(() => resolver.ResolveFunction("Foo/ /Nya"));
        Assert.Throws<Error>(() => resolver.ResolveFunction("/Foo/Bar/Nya"));
    }
}
