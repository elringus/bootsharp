using Xunit;

namespace Packer.Test;

public class LibraryTest : BuildTest
{
    [Fact]
    public void LibraryContainsJSRuntime ()
    {
        Task.Execute();
        Assert.Contains(MockData.JSFileContent, Data.GeneratedLibrary);
    }

    [Fact]
    public void LibraryExportsAssemblyObject ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Assert.Contains("exports.foo = {};", Data.GeneratedLibrary);
    }

    [Fact]
    public void WhenAssemblyNameContainDotsObjectCreateForEachPart ()
    {
        Data.AddAssemblyWithName("foo.bar.nya.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Assert.Contains("exports.foo = {};", Data.GeneratedLibrary);
        Assert.Contains("exports.foo.bar = {};", Data.GeneratedLibrary);
        Assert.Contains("exports.foo.bar.nya = {};", Data.GeneratedLibrary);
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        Data.AddAssemblyWithName("foo.bar.dll", "[JSInvokable] public static void Nya () { }");
        Task.Execute();
        Assert.Contains("exports.foo.bar.Nya = () => exports.invoke('foo.bar', 'Nya');", Data.GeneratedLibrary);
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        Data.AddAssemblyWithName("foo.bar.dll", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Assert.Contains("exports.foo.bar.Fun = undefined;", Data.GeneratedLibrary);
    }

    [Fact]
    public void BindFunctionIsAssertedAndAssignedToGlobal ()
    {
        Data.AddAssemblyWithName("foo.bar.dll", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Assert.Contains("global.DotNetJS_functions_foo_bar_Fun = exports.foo.bar.Fun || " +
                        "function() { throw new Error(\"Function 'dotnet.foo.bar.Fun' is not implemented.\"); }();", Data.GeneratedLibrary);
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        Data.AddAssembly("[JSInvokable] public static void Fun (string function) { }");
        Task.Execute();
        Assert.Contains("Fun = (fn) => exports.invoke", Data.GeneratedLibrary);
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        Data.AddAssembly(
            "[JSInvokable] public static Task Asy () => default;",
            "[JSInvokable] public static ValueTask AsyValue () => default;"
        );
        Task.Execute();
        Assert.Contains("Asy = () => exports.invokeAsync", Data.GeneratedLibrary);
        Assert.Contains("AsyValue = () => exports.invokeAsync", Data.GeneratedLibrary);
    }
}
