using System.IO;
using System.Threading.Tasks;
using Xunit;
using static Bootsharp.Method;

namespace Bootsharp.Test;

public class MethodTest
{
    [Fact]
    public void WhenEndpointNotFoundExceptionsIsThrown ()
    {
        Assert.Throws<FileNotFoundException>(() => Invoke("Foo/Bar/Baz"));
        Assert.Contains("class not found",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/Baz/Bar")).Message);
        Assert.Contains("class not found",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/MockClassWithNamespaceNotEqualAssemblyName/Do")).Message);
        Assert.Contains("method not found",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/MockClass/Baz")).Message);
    }

    [Fact]
    public void CanInvoke ()
    {
        const string json = "{\"Items\":[{\"Id\":\"foo\"}]}";
        Assert.Equal(json, Invoke("Bootsharp.Test/MockClass/Echo", new[] { json }));
    }

    [Fact]
    public void CanInvokeVoid ()
    {
        InvokeVoid("Bootsharp.Test/MockClass/Do");
    }

    [Fact]
    public async Task CanInvokeAsync ()
    {
        const string json = "{\"Items\":[{\"Id\":\"foo\"}]}";
        Assert.Equal(json, await InvokeAsync("Bootsharp.Test/MockClass/EchoAsync", new[] { json }));
    }

    [Fact]
    public async Task CanInvokeVoidAsync ()
    {
        var task = InvokeVoidAsync("Bootsharp.Test/MockClass/DoAsync");
        await task;
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void CanInvokeWithMultipleArgs ()
    {
        Assert.Equal("{\"Items\":[{\"Id\":\"nya\"},{\"Id\":\"nya\"}]}",
            Invoke("Bootsharp.Test/MockClass/Copy", new[] {
                "{\"Items\":[{\"Id\":\"foo\"}]}", "[{\"Id\":\"bar\"},{\"Id\":\"baz\"}]", "\"nya\""
            }));
    }

    [Fact]
    public void CanOmitOptionalArg ()
    {
        Assert.Equal("{\"Items\":[{\"Id\":\"bar\"},{\"Id\":\"baz\"}]}",
            Invoke("Bootsharp.Test/MockClass/Copy", new[] {
                "{\"Items\":[{\"Id\":\"foo\"}]}", "[{\"Id\":\"bar\"},{\"Id\":\"baz\"}]"
            }));
    }

    [Fact]
    public void CanOmitOptionArgument ()
    {
        const string json = "{\"Items\":[{\"Id\":\"foo\"}]}";
        Assert.Equal(json, Invoke("Bootsharp.Test/MockClass/Echo", new[] { json }));
    }

    [Fact]
    public void WhenInvokingVoidMethodWhileExpectingReturnValueErrorIsThrown ()
    {
        Assert.Contains("method didn't return any value",
            Assert.Throws<Error>(() => Invoke("Bootsharp.Test/MockClass/Do")).Message);
    }

    [Fact]
    public async Task WhenInvokeAsyncMethodDoesntReturnTaskErrorIsThrown ()
    {
        Assert.Contains("method didn't return task",
            (await Assert.ThrowsAsync<Error>(() => InvokeAsync("Bootsharp.Test/MockClass/Echo", new[] { "{\"Items\":[]}" }))).Message);
        Assert.Contains("method didn't return task",
            (await Assert.ThrowsAsync<Error>(() => InvokeVoidAsync("Bootsharp.Test/MockClass/Echo", new[] { "{\"Items\":[]}" }))).Message);
    }

    [Fact]
    public async Task WhenInvokeAsyncMethodTaskDoesntHaveResultIsNullErrorIsThrown ()
    {
        Assert.Contains("missing task result",
            (await Assert.ThrowsAsync<Error>(() => InvokeAsync("Bootsharp.Test/MockClass/DoAsync"))).Message);
    }
}
