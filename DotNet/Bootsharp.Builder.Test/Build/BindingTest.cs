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
            function __inv () { if (exports == null) throw Error("Boot the runtime before invoking C# APIs."); return exports; }
            function parseJson(obj) { const result = JSON.parse(obj); if (result === null) return undefined; return result; }
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
                    nya: () => __inv().Foo_Bar_MockClass.Nya()
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
                    get _fun() { if (typeof this.$fun !== "function") throw Error("Failed to invoke 'Foo.Bar.fun' from C#. Make sure to assign function in JavaScript."); return this.__fun; },
                    get fun() { return this.$fun; },
                    set fun($fun) { this.__fun = () => this.$fun(); this.$fun = $fun; }
                }
            };
            """);
    }

    [Fact]
    public void BindingForEventMethodIsGenerated ()
    {
        AddAssembly(
            With("[JSEvent] public static void OnFoo (string bar) { }"),
            With("[JSEvent] public static void OnBaz (int yaz, bool nya) { }"));
        Execute();
        Contains(
            """
            export const Global = {
                onFoo: new Event(),
                onBaz: new Event()
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
                bar: () => __inv().Foo_MockClass.Bar()
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
                        bar: () => __inv().Foo_Bar_Nya_MockClass.Bar()
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
                    get _fun() { if (typeof this.$fun !== "function") throw Error("Failed to invoke 'Bar.Nya.fun' from C#. Make sure to assign function in JavaScript."); return this.__fun; },
                    get fun() { return this.$fun; },
                    set fun($fun) { this.__fun = () => this.$fun(); this.$fun = $fun; }
                }
            };
            export const Foo = {
                foo: () => __inv().Foo_MockClass.Foo()
            };
            """);
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        AddAssembly(With("Foo", "[JSFunction] public static void Fun () { }"));
        Execute();
        Assert.Single(Matches("export const Foo"));
        Contains("bar: () => __inv().Foo_MockClass.Bar()");
        Contains(
            """
                get _fun() { if (typeof this.$fun !== "function") throw Error("Failed to invoke 'Foo.fun' from C#. Make sure to assign function in JavaScript."); return this.__fun; },
                get fun() { return this.$fun; },
                set fun($fun) { this.__fun = () => this.$fun(); this.$fun = $fun; }
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
                    get _fun() { if (typeof this.$fun !== "function") throw Error("Failed to invoke 'Nya.Bar.fun' from C#. Make sure to assign function in JavaScript."); return this.__fun; },
                    get fun() { return this.$fun; },
                    set fun($fun) { this.__fun = () => this.$fun(); this.$fun = $fun; }
                },
                Foo: {
                    foo: () => __inv().Nya_Foo_MockClass.Foo()
                }
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static int Foo () => 0;"));
        AddAssembly(With("Bar.Nya", "[JSFunction] public static void Fun () { }"));
        Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    get _fun() { if (typeof this.$fun !== "function") throw Error("Failed to invoke 'Bar.Nya.fun' from C#. Make sure to assign function in JavaScript."); return this.__fun; },
                    get fun() { return this.$fun; },
                    set fun($fun) { this.__fun = () => this.$fun(); this.$fun = $fun; }
                }
            };
            export const Foo = {
                foo: () => __inv().Foo_MockClass.Foo()
            };
            """);
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToGlobalObject ()
    {
        AddAssembly(
            With("[JSInvokable] public static Task<int> Nya () => Task.FromResult(0);"),
            With("[JSFunction] public static void Fun () { }"));
        Execute();
        Contains(
            """
            export const Global = {
                nya: () => __inv().MockClass.Nya(),
                get _fun() { if (typeof this.$fun !== "function") throw Error("Failed to invoke 'Global.fun' from C#. Make sure to assign function in JavaScript."); return this.__fun; },
                get fun() { return this.$fun; },
                set fun($fun) { this.__fun = () => this.$fun(); this.$fun = $fun; }
            };
            """);
    }

    [Fact]
    public void NamespaceAttributeOverrideObjectNames ()
    {
        AddAssembly(
            With("""[assembly:JSNamespace(@"Foo\.Bar\.(\S+)", "$1")]""", false),
            With("Foo.Bar.Nya", "[JSInvokable] public static Task GetNya () => Task.CompletedTask;"),
            With("Foo.Bar.Fun", "[JSFunction] public static void OnFun () { }"));
        Execute();
        Contains(
            """
            export const Fun = {
                get _onFun() { if (typeof this.$onFun !== "function") throw Error("Failed to invoke 'Fun.onFun' from C#. Make sure to assign function in JavaScript."); return this.__onFun; },
                get onFun() { return this.$onFun; },
                set onFun($onFun) { this.__onFun = () => this.$onFun(); this.$onFun = $onFun; }
            };
            export const Nya = {
                getNya: () => __inv().Foo_Bar_Nya_MockClass.GetNya()
            };
            """);
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly(With("[JSInvokable] public static void Fun (string function) { }"));
        Execute();
        Contains(
            """
            export const Global = {
                fun: (fn) => __inv().MockClass.Fun(fn)
            };
            """);
    }

    [Fact]
    public void SerializesCustomType ()
    {
        AddAssembly(
            With("public record Info;", false),
            With("[JSInvokable] public static Info Foo (Info i) => default;"),
            With("[JSFunction] public static Info? Bar (Info? i) => default;"),
            With("[JSEvent] public static void Baz (Info?[] i) { }"),
            With("[JSEvent] public static void Yaz (int a, Info i) { }"));
        Execute();
        Contains(
            """
            export const Global = {
                foo: (i) => parseJson(__inv().MockClass.Foo(JSON.stringify(i))),
                get _bar() { if (typeof this.$bar !== "function") throw Error("Failed to invoke 'Global.bar' from C#. Make sure to assign function in JavaScript."); return this.__bar; },
                get bar() { return this.$bar; },
                set bar($bar) { this.__bar = (i) => JSON.stringify(this.$bar(parseJson(i))); this.$bar = $bar; },
                baz: new Event({ convert: (i) => [parseJson(i)] }),
                yaz: new Event({ convert: (a, i) => [a, parseJson(i)] })
            };
            """);
    }

    [Fact]
    public void AwaitsWhenSerializingInAsyncFunctions ()
    {
        AddAssembly(
            With("public record Info;", false),
            With("[JSInvokable] public static Task<Info> Foo (Info i) => default;"),
            With("[JSFunction] public static Task<Info?> Bar (Info? i) => default;"));
        Execute();
        Contains(
            """
            export const Global = {
                foo: async (i) => parseJson(await __inv().MockClass.Foo(JSON.stringify(i))),
                get _bar() { if (typeof this.$bar !== "function") throw Error("Failed to invoke 'Global.bar' from C#. Make sure to assign function in JavaScript."); return this.__bar; },
                get bar() { return this.$bar; },
                set bar($bar) { this.__bar = async (i) => JSON.stringify(await this.$bar(parseJson(i))); this.$bar = $bar; }
            };
            """);
    }

    [Fact]
    public void ExportedEnumsAreDeclaredInJS ()
    {
        AddAssembly(
            With("n", "public enum Foo { A, B }"),
            With("n", "[JSInvokable] public static Foo GetFoo () => default;"));
        Execute();
        Contains(
            """
            export const n = {
                getFoo: () => parseJson(__inv().n_MockClass.GetFoo()),
                Foo: { A: 0, B: 1 }
            };
            """);
    }
}
