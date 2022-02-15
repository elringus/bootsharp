using Xunit;
using static Packer.Test.MockAssembly;

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
        AddAssembly(WithSpace("Foo", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("exports.Foo = {};");
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPartInOrder ()
    {
        AddAssembly(WithSpace("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Matches(@"exports.Foo = {};\s*exports.Foo.Bar = {};\s*exports.Foo.Bar.Nya = {};");
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnObject ()
    {
        AddAssembly(WithSpace("Foo", "[JSInvokable] public static void Foo () { }")
            .AddSpace("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo = {};");
        Contains("exports.Bar = {};");
        Contains("exports.Bar.Nya = {};");
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(WithSpace("Foo", "[JSInvokable] public static void Bar () { }"));
        AddAssembly(WithSpace("Foo", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Assert.Single(Matches("Foo"));
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedToDifferentObjects ()
    {
        Data.AddAssembly("Nya.Foo", "[JSInvokable] public static void Foo () { }");
        Data.AddAssembly("Nya.Bar", "[JSFunction] public static void Fun () { }");
        Task.Execute();
        Assert.Single(Matches("exports.Nya = {}"));
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        Data.AddAssembly("foo.asm.dll",
            new MockClass { Space = "Foo.Bar", Lines = new[] { "[JSInvokable] public static void Nya () { }" } }
        );
        Task.Execute();
        Contains("exports.Foo.Bar.Nya = () => exports.invoke('foo.asm', 'Nya');");
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        Data.AddAssembly("foo.asm.dll",
            new MockClass { Space = "Foo.Bar", Lines = new[] { "[JSFunction] public static void Fun () { }" } }
        );
        Task.Execute();
        Contains("exports.Foo.Bar.Fun = undefined;");
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        Data.AddAssembly("foo.asm.dll",
            new MockClass { Space = "Foo", Lines = new[] { "[JSInvokable] public static void Foo () { }" } }
        );
        Data.AddAssembly("bar.nya.asm.dll",
            new MockClass { Space = "Bar.Nya", Lines = new[] { "[JSFunction] public static void Fun () { }" } }
        );
        Task.Execute();
        Contains("exports.Foo.Foo = () => exports.invoke('foo.asm', 'Foo');");
        Contains("exports.Bar.Nya.Fun = undefined;");
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        Data.AddAssemblyTemp(new[] { "[JSInvokable] public static void Fun (string function) { }" });
        Task.Execute();
        Contains("Fun = (fn) => exports.invoke");
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        Data.AddAssemblyTemp(
            "[JSInvokable] public static Task Asy () => default;",
            "[JSInvokable] public static ValueTask AsyValue () => default;"
        );
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
