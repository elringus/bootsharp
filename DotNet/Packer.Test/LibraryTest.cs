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
    public void WhenEmbedBinariesDisabledLibraryExportsBootUris ()
    {
        AddAssembly("Foo.dll");
        Task.EmbedBinaries = false;
        Task.Execute();
        Contains("exports.getBootUris = () => ({");
        Contains("wasm: \"dotnet.wasm\"");
        Contains("entryAssembly: \"Foo.dll\"");
        Contains("assemblies: [");
        Contains("Foo.dll");
        Contains("DotNetJS.dll");
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
        Contains("exports.Foo.Bar.nya = () => exports.invoke('foo.asm', 'Nya');");
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
        Contains("exports.Asm.onFoo = new exports.Event();");
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly("foo.asm.dll", With("Foo", "[JSInvokable] public static void Foo () { }"));
        AddAssembly("bar.nya.asm.dll", With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Foo.foo = () => exports.invoke('foo.asm', 'Foo');");
        Contains("exports.Bar.Nya.fun = undefined;");
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToBindingsObject ()
    {
        AddAssembly("asm.dll",
            With("[JSInvokable] public static void Nya () { }"),
            With("[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains("exports.Bindings.nya = () => exports.invoke('asm', 'Nya');");
        Contains("exports.Bindings.fun = undefined;");
    }

    [Fact]
    public void NamespaceAttributeOverrideObjectNames ()
    {
        AddAssembly("asm.dll",
            With(@"[assembly:JSNamespace(@""Foo\.Bar\.(\S+)"", ""$1"")]", false),
            With("Foo.Bar.Nya", "[JSInvokable] public static void GetNya () { }"),
            With("Foo.Bar.Fun", "[JSFunction] public static void OnFun () { }"));
        Task.Execute();
        Contains("exports.Nya.getNya = () => exports.invoke('asm', 'GetNya');");
        Contains("exports.Fun.onFun = undefined;");
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly(With("[JSInvokable] public static void Fun (string function) { }"));
        Task.Execute();
        Contains("fun = (fn) => exports.invoke");
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        AddAssembly(
            With("[JSInvokable] public static Task Asy () => default;"),
            With("[JSInvokable] public static ValueTask AsyValue () => default;"));
        Task.Execute();
        Contains("asy = () => exports.invokeAsync");
        Contains("asyValue = () => exports.invokeAsync");
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
