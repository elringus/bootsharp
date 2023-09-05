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
        Contains(
            """
            import { invoke, invokeVoid, invokeAsync, invokeVoidAsync } from "./exports";
            import { Event } from "./event";
            """);
    }

    [Fact]
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly("Asm.dll", With("Foo", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains(
            """
            export const Foo = {
                bar: () => invokeVoid("Asm/Foo.MockClass/Bar")
            };
            """);
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPart ()
    {
        AddAssembly("Asm.dll", With("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Nya: {
                        bar: () => invokeVoid("Asm/Foo.Bar.Nya.MockClass/Bar")
                    }
                }
            };
            """);
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnObject ()
    {
        AddAssembly("Asm.dll",
            With("Foo", "[JSInvokable] public static void Foo () { }"),
            With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    fun: undefined
                }
            };
            export const Foo = {
                foo: () => invokeVoid("Asm/Foo.MockClass/Foo")
            };
            """);
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly("Asm1.dll", With("Foo", "[JSInvokable] public static void Bar () { }"));
        AddAssembly("Asm2.dll", With("Foo", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains(
            """
            export const Foo = {
                bar: () => invokeVoid("Asm1/Foo.MockClass/Bar"),
                fun: undefined
            };
            """);
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedUnderSameObject ()
    {
        AddAssembly("Asm.dll",
            With("Nya.Foo", "[JSInvokable] public static void Foo () { }"),
            With("Nya.Bar", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains(
            """
            export const Nya = {
                Bar: {
                    fun: undefined
                },
                Foo: {
                    foo: () => invokeVoid("Asm/Nya.Foo.MockClass/Foo")
                }
            };
            """);
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        AddAssembly("Foo.Asm.dll", With("Foo.Bar", "[JSInvokable] public static void Nya () { }"));
        Task.Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    nya: () => invokeVoid("Foo.Asm/Foo.Bar.MockClass/Nya")
                }
            };
            """);
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        AddAssembly(With("Foo.Bar", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    fun: undefined
                }
            };
            """);
    }

    [Fact]
    public void BindingForEventMethodIsGenerated ()
    {
        AddAssembly(With("[JSEvent] public static void OnFoo (string bar) { }"));
        Task.Execute();
        Contains(
            """
            export const Global = {
                onFoo: new Event()
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly("Foo.Asm.dll", With("Foo", "[JSInvokable] public static int Foo () => 0;"));
        AddAssembly("Bar.Nya.Asm.dll", With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    fun: undefined
                }
            };
            export const Foo = {
                foo: () => invoke("Foo.Asm/Foo.MockClass/Foo")
            };
            """);
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToGlobalObject ()
    {
        AddAssembly("Asm.dll",
            With("[JSInvokable] public static Task<int> Nya () => Task.FromResult(0);"),
            With("[JSFunction] public static void Fun () { }"));
        Task.Execute();
        Contains(
            """
            export const Global = {
                nya: () => invokeAsync("Asm/MockClass/Nya"),
                fun: undefined
            };
            """);
    }

    [Fact]
    public void NamespaceAttributeOverrideObjectNames ()
    {
        AddAssembly("Asm.dll",
            With("""[assembly:JSNamespace(@"Foo\.Bar\.(\S+)", "$1")]""", false),
            With("Foo.Bar.Nya", "[JSInvokable] public static Task GetNya () => Task.CompletedTask;"),
            With("Foo.Bar.Fun", "[JSFunction] public static void OnFun () { }"));
        Task.Execute();
        Contains(
            """
            export const Fun = {
                onFun: undefined
            };
            export const Nya = {
                getNya: () => invokeVoidAsync("Asm/Foo.Bar.Nya.MockClass/GetNya")
            };
            """);
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly("Asm.dll", With("[JSInvokable] public static void Fun (string function) { }"));
        Task.Execute();
        Contains(
            """
            export const Global = {
                fun: (fn) => invokeVoid("Asm/MockClass/Fun", fn)
            };
            """);
    }

    [Fact]
    public void AsyncMethodsBindViaInvokeAsync ()
    {
        AddAssembly("Asm.dll",
            With("[JSInvokable] public static Task Asy () => default;"),
            With("[JSInvokable] public static Task<string> AsyValue () => default;"));
        Task.Execute();
        Contains(
            """
            export const Global = {
                asy: () => invokeVoidAsync("Asm/MockClass/Asy"),
                asyValue: () => invokeAsync("Asm/MockClass/AsyValue")
            };
            """);
    }

    [Fact]
    public void ExportedEnumsAreDeclaredInJS ()
    {
        AddAssembly("Asm.dll",
            With("n", "public enum Foo { A, B }"),
            With("n", "[JSInvokable] public static Foo GetFoo () => default;"));
        Task.Execute();
        Contains(
            """
            export const n = {
                getFoo: () => invoke("Asm/n.MockClass/GetFoo"),
                Foo: { A: "A", B: "B" }
            };
            """);
    }
}
