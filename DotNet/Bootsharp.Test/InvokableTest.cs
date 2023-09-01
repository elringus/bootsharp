using System.Threading.Tasks;
using Xunit;
using static Bootsharp.Invokable;

namespace Bootsharp.Test;

public class InvokableTest
{
    [Fact]
    public void WhenEndpointNotFoundExceptionsIsThrown ()
    {
        Assert.Contains("assembly not found",
            Assert.Throws<Error>(() => Invoke("Foo/Bar.Baz/Nya")).Message);
        Assert.Contains("class not found",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/Baz.Bar/Nya")).Message);
        Assert.Contains("class not found",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/Bootsharp.Test.Bar/Nya")).Message);
        Assert.Contains("method not found",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/Bootsharp.Test.MockClass/Nya")).Message);
    }

    [Fact]
    public void CanInvoke ()
    {
        const string json = """{"Items":[{"Id":"foo"}]}""";
        Assert.Equal(json, Invoke("Bootsharp.Test/Bootsharp.Test.MockClass/Echo", new[] { json }));
    }

    [Fact]
    public void CanInvokeWhenAssemblyNotEqualEndpoint ()
    {
        const string json = """{"Items":[{"Id":"foo"}]}""";
        Assert.Equal(json, Invoke("Bootsharp.Test/Bootsharp.Test.Other.MockClassWithNamespaceNotEqualAssemblyName/Echo", new[] { json }));
    }

    [Fact]
    public void CanInvokeVoid ()
    {
        InvokeVoid("Bootsharp.Test/Bootsharp.Test.MockClass/Do");
    }

    [Fact]
    public async Task CanInvokeAsync ()
    {
        const string json = """{"Items":[{"Id":"foo"}]}""";
        Assert.Equal(json, await InvokeAsync("Bootsharp.Test/Bootsharp.Test.MockClass/EchoAsync", new[] { json }));
    }

    [Fact]
    public async Task CanInvokeVoidAsync ()
    {
        var task = InvokeVoidAsync("Bootsharp.Test/Bootsharp.Test.MockClass/DoAsync");
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void CanInvokeWithMultipleArgs ()
    {
        Assert.Equal("""{"Items":[{"Id":"nya"},{"Id":"nya"}]}""",
            Invoke("Bootsharp.Test/Bootsharp.Test.MockClass/Copy", new[] {
                """{"Items":[{"Id":"foo"}]}""", """[{"Id":"bar"},{"Id":"baz"}]""", "\"nya\""
            }));
    }

    [Fact]
    public void CanOmitOptionalArg ()
    {
        Assert.Equal("""{"Items":[{"Id":"bar"},{"Id":"baz"}]}""",
            Invoke("Bootsharp.Test/Bootsharp.Test.MockClass/Copy", new[] {
                """{"Items":[{"Id":"foo"}]}""", """[{"Id":"bar"},{"Id":"baz"}]"""
            }));
    }

    [Fact]
    public void CanOmitOptionArgument ()
    {
        const string json = """{"Items":[{"Id":"foo"}]}""";
        Assert.Equal(json, Invoke("Bootsharp.Test/Bootsharp.Test.MockClass/Echo", new[] { json }));
    }

    [Fact]
    public void WhenInvokingVoidMethodWhileExpectingReturnValueErrorIsThrown ()
    {
        Assert.Contains("method didn't return any value",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/Bootsharp.Test.MockClass/Do")).Message);
    }

    [Fact]
    public async Task WhenInvokeAsyncMethodDoesntReturnTaskErrorIsThrown ()
    {
        Assert.Contains("method didn't return task",
            (await Assert.ThrowsAsync<Error>(() => InvokeAsync("Bootsharp.Test/Bootsharp.Test.MockClass/Echo", new[] { "{\"Items\":[]}" }))).Message);
        Assert.Contains("method didn't return task",
            (await Assert.ThrowsAsync<Error>(() => InvokeVoidAsync("Bootsharp.Test/Bootsharp.Test.MockClass/Echo", new[] { "{\"Items\":[]}" }))).Message);
    }

    [Fact]
    public async Task WhenInvokeAsyncMethodTaskDoesntHaveResultIsNullErrorIsThrown ()
    {
        Assert.Contains("missing task result",
            (await Assert.ThrowsAsync<Error>(() => InvokeAsync("Bootsharp.Test/Bootsharp.Test.MockClass/DoAsync"))).Message);
    }
}
