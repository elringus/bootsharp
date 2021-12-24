using Xunit;

namespace Packer.Test;

public class LibraryTest : ContentTest
{
    protected override string TestedContent => Data.GeneratedLibrary;

    [Fact]
    public void LibraryContainsJSRuntime ()
    {
        Task.Execute();
        Contains(MockData.JSFileContent);
    }

    [Fact]
    public void LibraryExportsAssemblyObject ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("exports.foo = {};");
    }

    [Fact]
    public void WhenAssemblyNameContainDotsObjectCreatedForEachPartInOrder ()
    {
        Data.AddAssemblyWithName("foo.bar.nya.dll", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Matches(@"exports.foo = {};\s*exports.foo.bar = {};\s*exports.foo.bar.nya = {};");
    }

    [Fact]
    public void WhenMultipleAssembliesEachGetItsOwnObject ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Foo () { }");
        Data.AddAssemblyWithName("bar.nya.dll", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Contains("exports.foo = {};");
        Contains("exports.bar = {};");
        Contains("exports.bar.nya = {};");
    }

    [Fact]
    public void DifferentAssembliesWithSameRootAssignedToDifferentObjects ()
    {
        Data.AddAssemblyWithName("nya.foo.dll", "[JSInvokable] public static void Foo () { }");
        Data.AddAssemblyWithName("nya.bar.dll", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Assert.Single(Matches("exports.nya = {}"));
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        Data.AddAssemblyWithName("foo.bar.dll", "[JSInvokable] public static void Nya () { }");
        Task.Execute();
        Contains("exports.foo.bar.Nya = () => exports.invoke('foo.bar', 'Nya');");
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        Data.AddAssemblyWithName("foo.bar.dll", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Contains("exports.foo.bar.Fun = undefined;");
    }

    [Fact]
    public void BindingsFromMultipleAssembliesAssignedToRespectiveObjects ()
    {
        Data.AddAssemblyWithName("foo.dll", "[JSInvokable] public static void Foo () { }");
        Data.AddAssemblyWithName("bar.nya.dll", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Contains("exports.foo.Foo = () => exports.invoke('foo', 'Foo');");
        Contains("exports.bar.nya.Fun = undefined;");
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        Data.AddAssembly("[JSInvokable] public static void Fun (string function) { }");
        Task.Execute();
        Contains("Fun = (fn) => exports.invoke");
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        Data.AddAssembly(
            "[JSInvokable] public static Task Asy () => default;",
            "[JSInvokable] public static ValueTask AsyValue () => default;"
        );
        Task.Execute();
        Contains("Asy = () => exports.invokeAsync");
        Contains("AsyValue = () => exports.invokeAsync");
    }
}
