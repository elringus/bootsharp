using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Bootsharp.Test;

public class MethodTest
{
    [Fact]
    public void WhenEndpointNotFoundExceptionsIsThrown ()
    {
        Assert.Throws<FileNotFoundException>(() => Method.Invoke("Foo/Bar/Baz", Array.Empty<string>()));
        Assert.Contains("class not found",
            Assert.Throws<Error>(() => Method.Invoke("Bootsharp.Test/Baz/Bar", Array.Empty<string>())).Message);
        Assert.Contains("class not found",
            Assert.Throws<Error>(() => Method.Invoke("Bootsharp.Test/MockClassWithNamespaceNotEqualAssemblyName/Do", Array.Empty<string>())).Message);
        Assert.Contains("method not found",
            Assert.Throws<Error>(() => Method.Invoke("Bootsharp.Test/MockClass/Baz", Array.Empty<string>())).Message);
    }

    [Fact]
    public void CanInvoke ()
    {
        const string json = "{\"Items\":[{\"Id\":\"foo\"}]}";
        Assert.Equal(json, Method.Invoke("Bootsharp.Test/MockClass/Echo", new[] { json }));
    }

    [Fact]
    public void CanInvokeVoid ()
    {
        Method.InvokeVoid("Bootsharp.Test/MockClass/Do", Array.Empty<string>());
    }

    [Fact]
    public async Task CanInvokeAsync ()
    {
        const string json = "{\"Items\":[{\"Id\":\"foo\"}]}";
        Assert.Equal(json, await Method.InvokeAsync("Bootsharp.Test/MockClass/EchoAsync", new[] { json }));
    }

    [Fact]
    public async Task CanInvokeVoidAsync ()
    {
        var task = Method.InvokeVoidAsync("Bootsharp.Test/MockClass/DoAsync", Array.Empty<string>());
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void CanInvokeWithMultipleArgs ()
    {
        Assert.Equal("{\"Items\":[{\"Id\":\"nya\"},{\"Id\":\"nya\"}]}",
            Method.Invoke("Bootsharp.Test/MockClass/Copy", new[] {
                "{\"Items\":[{\"Id\":\"foo\"}]}", "[{\"Id\":\"bar\"},{\"Id\":\"baz\"}]", "\"nya\""
            }));
    }

    [Fact]
    public void CanOmitOptionalArg ()
    {
        Assert.Equal("{\"Items\":[{\"Id\":\"bar\"},{\"Id\":\"baz\"}]}",
            Method.Invoke("Bootsharp.Test/MockClass/Copy", new[] {
                "{\"Items\":[{\"Id\":\"foo\"}]}", "[{\"Id\":\"bar\"},{\"Id\":\"baz\"}]"
            }));
    }

    [Fact]
    public void CanOmitOptionArgument ()
    {
        const string json = "{\"Items\":[{\"Id\":\"foo\"}]}";
        Assert.Equal(json, Method.Invoke("Bootsharp.Test/MockClass/Echo", new[] { json }));
    }

    [Fact]
    public void WhenInvokingVoidMethodWhileExpectingReturnValueErrorIsThrown ()
    {
        Assert.Contains("method didn't return any value",
            Assert.Throws<Error>(() => Method.Invoke("Bootsharp.Test/MockClass/Do", Array.Empty<string>())).Message);
    }

    [Fact]
    public async Task WhenInvokeAsyncMethodDoesntReturnTaskErrorIsThrown ()
    {
        Assert.Contains("method didn't return task",
            (await Assert.ThrowsAsync<Error>(() => Method.InvokeAsync("Bootsharp.Test/MockClass/Echo", new[] { "{\"Items\":[]}" }))).Message);
        Assert.Contains("method didn't return task",
            (await Assert.ThrowsAsync<Error>(() => Method.InvokeVoidAsync("Bootsharp.Test/MockClass/Echo", new[] { "{\"Items\":[]}" }))).Message);
    }

    [Fact]
    public async Task WhenInvokeAsyncMethodTaskDoesntHaveResultIsNullErrorIsThrown ()
    {
        Assert.Contains("missing task result",
            (await Assert.ThrowsAsync<Error>(() => Method.InvokeAsync("Bootsharp.Test/MockClass/DoAsync", Array.Empty<string>()))).Message);
    }
}
