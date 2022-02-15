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
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("exports.Foo = {};");
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPartInOrder ()
    {
        AddAssembly(With("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Matches(@"exports.Foo = {};\s*exports.Foo.Bar = {};\s*exports.Foo.Bar.Nya = {};");
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnObject ()
    {
        AddAssembly(
            With("Foo", "[JSInvokable] public static void Foo () { }"),
            With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo = {};");
        Contains("exports.Bar = {};");
        Contains("exports.Bar.Nya = {};");
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        AddAssembly(With("Foo", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Assert.Single(Matches("Foo"));
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedIndividually ()
    {
        AddAssembly(
            With("Nya.Foo", "[JSInvokable] public static void Foo () { }"),
            With("Nya.Bar", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Assert.Single(Matches("exports.Nya = {}"));
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        AddAssembly("foo.asm.dll", With("Foo.Bar", "[JSInvokable] public static void Nya () { }"));
        Task.Execute();
        Contains("exports.Foo.Bar.Nya = () => exports.invoke('foo.asm', 'Nya');");
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        AddAssembly("foo.asm.dll", With("Foo.Bar", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo.Bar.Fun = undefined;");
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly("foo.asm.dll", With("Foo", "[JSInvokable] public static void Foo () { }"));
        AddAssembly("bar.nya.asm.dll", With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo.Foo = () => exports.invoke('foo.asm', 'Foo');");
        Contains("exports.Bar.Nya.Fun = undefined;");
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly(With("[JSInvokable] public static void Fun (string function) { }"));
        Task.Execute();
        Contains("Fun = (fn) => exports.invoke");
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        AddAssembly(
            With("[JSInvokable] public static Task Asy () => default;"),
            With("[JSInvokable] public static ValueTask AsyValue () => default;"));
        Task.Execute();
        Contains("Asy = () => exports.invokeAsync");
        Contains("AsyValue = () => exports.invokeAsync");
    }

    [Fact]
    public void NamespacePatternOverrideObjectNames ()
    {
        // TODO
    }
}
