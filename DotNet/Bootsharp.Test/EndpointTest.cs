using Xunit;
using static Bootsharp.Endpoint;

namespace Bootsharp.Test;

public class EndpointTest
{
    [Fact]
    public void BuildsCorrectMethodEndpoint ()
    {
        Assert.Equal("Asm/Space.Class/Method", Build("Asm", "Space.Class", "Method"));
        Assert.Equal("Foo.Bar.Baz/Nya.Far.Class.Nested/Method", Build("Foo.Bar.Baz", "Nya.Far.Class.Nested", "Method"));
    }

    [Fact]
    public void CanResolveValidMethodEndpoint ()
    {
        Assert.Equal(("Asm", "Space.Class", "Method"), Resolve("Asm/Space.Class/Method"));
        Assert.Equal(("Foo.Bar.Baz", "Nya.Far.Class.Nested", "Method"), Resolve("Foo.Bar.Baz/Nya.Far.Class.Nested/Method"));
    }

    [Fact]
    public void ThrowsOnInvalidMethodEndpoint ()
    {
        Assert.Throws<Error>(() => Resolve(""));
        Assert.Throws<Error>(() => Resolve("/"));
        Assert.Throws<Error>(() => Resolve("Foo"));
        Assert.Throws<Error>(() => Resolve("Foo/Bar"));
        Assert.Throws<Error>(() => Resolve("Foo/Bar.Baz"));
        Assert.Throws<Error>(() => Resolve("Foo/Bar.Baz/"));
        Assert.Throws<Error>(() => Resolve("Foo//Nya"));
        Assert.Throws<Error>(() => Resolve("Foo/ /Nya"));
        Assert.Throws<Error>(() => Resolve("/Foo/Bar/Nya"));
    }
}
