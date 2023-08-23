using Xunit;
using static Bootsharp.EndpointBuilder;
using static Bootsharp.EndpointResolver;

namespace Bootsharp.Test;

public class EndpointTest
{
    [Fact]
    public void BuildsCorrectMethodEndpoint ()
    {
        Assert.Equal("Asm/Space/Class/Method", BuildInvokable("Asm", "Space", "Class", "Method"));
        Assert.Equal("Foo.Bar.Baz/Nya.Far/Class.Nested/Method", BuildInvokable("Foo.Bar.Baz", "Nya.Far", "Class.Nested", "Method"));
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
        Assert.Equal(("Asm", "Space", "Class", "Method"), ResolveInvokable("Asm/Space/Class/Method"));
        Assert.Equal(("Foo.Bar.Baz", "Nya.Far", "Class.Nested", "Method"), ResolveInvokable("Foo.Bar.Baz/Nya.Far/Class.Nested/Method"));
    }

    [Fact]
    public void ThrowsOnInvalidMethodEndpoint ()
    {
        Assert.Throws<Error>(() => ResolveInvokable(""));
        Assert.Throws<Error>(() => ResolveInvokable("/"));
        Assert.Throws<Error>(() => ResolveInvokable("Foo"));
        Assert.Throws<Error>(() => ResolveInvokable("Foo/Bar"));
        Assert.Throws<Error>(() => ResolveInvokable("Foo/Bar/Baz"));
        Assert.Throws<Error>(() => ResolveInvokable("Foo/Bar/Baz/"));
        Assert.Throws<Error>(() => ResolveInvokable("Foo//Nya"));
        Assert.Throws<Error>(() => ResolveInvokable("Foo/ /Nya"));
        Assert.Throws<Error>(() => ResolveInvokable("/Foo/Bar/Nya"));
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
