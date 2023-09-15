namespace Bootsharp.Builder.Test;

public class BindingTest : BuildTest
{
    protected override string TestedContent => GeneratedBindings;

    [Fact]
    public void WhenNoBindingsNothingIsGenerated ()
    {
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void InteropFunctionsImported ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        Execute();
        Contains(
            """
            import { exports } from "./exports";
            import { Event } from "./event";
            """);
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        AddAssembly(With("Foo.Bar", "[JSInvokable] public static void Nya () { }"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    nya: () => exports.Foo_Bar_MockClass.Nya()
                }
            };
            """);
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        AddAssembly(With("Foo.Bar", "[JSFunction] public static void Fun () { }"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    get fun() { return this.$fun; },
                    set fun($fun) { this.$fun = () => $fun(); }
                }
            };
            """);
    }

    [Fact]
    public void BindingForEventMethodIsGenerated ()
    {
        AddAssembly(With("[JSEvent] public static void OnFoo (string bar) { }"));
        Execute();
        Contains(
            """
            export const Global = {
                onFoo: new Event()
            };
            """);
    }

    [Fact]
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        Execute();
        Contains(
            """
            export const Foo = {
                bar: () => exports.Foo_MockClass.Bar()
            };
            """);
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPart ()
    {
        AddAssembly(With("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Nya: {
                        bar: () => exports.Foo_Bar_Nya_MockClass.Bar()
                    }
                }
            };
            """);
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnObject ()
    {
        AddAssembly(
            With("Foo", "[JSInvokable] public static void Foo () { }"),
            With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    get fun() { return this.$fun; },
                    set fun($fun) { this.$fun = () => $fun(); }
                }
            };
            export const Foo = {
                foo: () => exports.Foo_MockClass.Foo()
            };
            """);
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        AddAssembly(With("Foo", "[JSFunction] public static void Fun () { }"));
        Execute();
        Contains(
            """
            export const Foo = {
                bar: () => exports.Foo_MockClass.Bar(),
                get fun() { return this.$fun; },
                set fun($fun) { this.$fun = () => $fun(); }
            };
            """);
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedUnderSameObject ()
    {
        AddAssembly(
            With("Nya.Foo", "[JSInvokable] public static void Foo () { }"),
            With("Nya.Bar", "[JSFunction] public static void Fun () { }"));
        Execute();
        Contains(
            """
            export const Nya = {
                Bar: {
                    get fun() { return this.$fun; },
                    set fun($fun) { this.$fun = () => $fun(); }
                },
                Foo: {
                    foo: () => exports.Nya_Foo_MockClass.Foo()
                }
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly("Foo.Asm.dll", With("Foo", "[JSInvokable] public static int Foo () => 0;"));
        AddAssembly("Bar.Nya.Asm.dll", With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Execute();
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
        Execute();
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
        Execute();
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
        Execute();
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
        Execute();
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
        Execute();
        Contains(
            """
            export const n = {
                getFoo: () => invoke("Asm/n.MockClass/GetFoo"),
                Foo: { A: "A", B: "B" }
            };
            """);
    }
}