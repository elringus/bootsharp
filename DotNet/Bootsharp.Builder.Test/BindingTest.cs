using Xunit;

namespace Bootsharp.Builder.Test;

public class BindingTest : ContentTest
{
    protected override string TestedContent => GeneratedBindings;

    [Fact]
    public void WhenNoBindingsNothingIsGenerated ()
    {
        Task.Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void InteropFunctionsImported ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("import { invoke, invokeVoid, invokeAsync, invokeVoidAsync } from './exports';");
        Contains("import { Event } from './event';");
    }

    [Fact]
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("export const Foo = {");
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPart ()
    {
        AddAssembly(With("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("export const Foo = {");
        Contains("Bar: {");
        Contains("Nya: {");
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnObject ()
    {
        AddAssembly(
            With("Foo", "[JSInvokable] public static void Foo () { }"),
            With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("export const Foo = {");
        Contains("export const Bar = {");
        Contains("Nya: {");
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        AddAssembly(With("Foo", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Assert.Single(Matches("exports.Foo ="));
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
        Contains("exports.Foo.Bar.nya = () => invokeVoid('foo.asm/Foo.Bar.MockClass/Nya');");
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        AddAssembly(With("Foo.Bar", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo.Bar.fun = undefined;");
    }

    [Fact]
    public void BindingForEventMethodIsGenerated ()
    {
        AddAssembly(With("Asm", "[JSEvent] public static void OnFoo (string bar) { }"));
        Task.Execute();
        Contains("exports.Asm.onFoo = new Event();");
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly("foo.asm.dll", With("Foo", "[JSInvokable] public static int Foo () => 0;"));
        AddAssembly("bar.nya.asm.dll", With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo.foo = () => invoke('foo.asm/Foo.MockClass/Foo');");
        Contains("exports.Bar.Nya.fun = undefined;");
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToBindingsObject ()
    {
        AddAssembly("asm.dll",
            With("[JSInvokable] public static Task<int> Nya () => Task.FromResult(0);"),
            With("[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Bindings.nya = () => invokeAsync('asm/MockClass/Nya');");
        Contains("exports.Bindings.fun = undefined;");
    }

    [Fact]
    public void NamespaceAttributeOverrideObjectNames ()
    {
        AddAssembly("asm.dll",
            With("""[assembly:JSNamespace(@"Foo\.Bar\.(\S+)", "$1")]""", false),
            With("Foo.Bar.Nya", "[JSInvokable] public static Task GetNya () => Task.CompletedTask;"),
            With("Foo.Bar.Fun", "[JSFunction] public static void OnFun () { }"));
        Task.Execute();
        Contains("exports.Nya.getNya = () => invokeVoidAsync('asm/Foo.Bar.Nya.MockClass/GetNya');");
        Contains("exports.Fun.onFun = undefined;");
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly(With("[JSInvokable] public static void Fun (string function) { }"));
        Task.Execute();
        Contains("fun = (fn) => invoke");
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        AddAssembly(
            With("[JSInvokable] public static Task Asy () => default;"),
            With("[JSInvokable] public static Task<string> AsyValue () => default;"));
        Task.Execute();
        Contains("asy = () => invokeVoidAsync");
        Contains("asyValue = () => invokeAsync");
    }

    [Fact]
    public void ExportedEnumsAreDeclaredInJS ()
    {
        AddAssembly(
            With("n", "public enum Foo { A, B }"),
            With("n", "[JSInvokable] public static Foo GetFoo () => default;"));
        Task.Execute();
        Contains("exports.n = {};");
        Contains("exports.n.Foo = { A: \"A\", B: \"B\" };");
    }
}
