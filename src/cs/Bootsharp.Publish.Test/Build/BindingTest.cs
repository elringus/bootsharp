namespace Bootsharp.Publish.Test;

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
            function getExports () { if (exports == null) throw Error("Boot the runtime before invoking C# APIs."); return exports; }
            function serialize(obj) { return JSON.stringify(obj); }
            function deserialize(json) { const result = JSON.parse(json); if (result === null) return undefined; return result; }
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
                    nya: () => getExports().Foo_Bar_MockClass.Nya()
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
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.Bar.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                }
            };
            """);
    }

    [Fact]
    public void BindingForEventMethodIsGenerated ()
    {
        AddAssembly(
            With("[JSEvent] public static void OnFoo () { }"),
            With("[JSEvent] public static void OnBar (string a) { }"),
            With("[JSEvent] public static void OnBaz (int a, bool b) { }"));
        Execute();
        Contains(
            """
            export const Global = {
                onFoo: new Event(),
                onFooSerialized: () => Global.onFoo.broadcast(),
                onBar: new Event(),
                onBarSerialized: (a) => Global.onBar.broadcast(a),
                onBaz: new Event(),
                onBazSerialized: (a, b) => Global.onBaz.broadcast(a, b)
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
                bar: () => getExports().Foo_MockClass.Bar()
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
                        bar: () => getExports().Foo_Bar_Nya_MockClass.Bar()
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
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Bar.Nya.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                }
            };
            export const Foo = {
                foo: () => getExports().Foo_MockClass.Foo()
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
        Contains("bar: () => getExports().Foo_MockClass.Bar()");
        Contains(
            """
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
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
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Nya.Bar.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                },
                Foo: {
                    foo: () => getExports().Nya_Foo_MockClass.Foo()
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
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Bar.Nya.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                }
            };
            export const Foo = {
                foo: () => getExports().Foo_MockClass.Foo()
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
                nya: () => getExports().MockClass.Nya(),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Global.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
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
                get onFun() { return this.onFunHandler; },
                set onFun(handler) { this.onFunHandler = handler; this.onFunSerializedHandler = () => this.onFunHandler(); },
                get onFunSerialized() { if (typeof this.onFunHandler !== "function") throw Error("Failed to invoke 'Fun.onFun' from C#. Make sure to assign function in JavaScript."); return this.onFunSerializedHandler; }
            };
            export const Nya = {
                getNya: () => getExports().Foo_Bar_Nya_MockClass.GetNya()
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
                fun: (fn) => getExports().MockClass.Fun(fn)
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
                foo: (i) => deserialize(getExports().MockClass.Foo(serialize(i))),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = (i) => serialize(this.barHandler(deserialize(i))); },
                get barSerialized() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Global.bar' from C#. Make sure to assign function in JavaScript."); return this.barSerializedHandler; },
                baz: new Event(),
                bazSerialized: (i) => Global.baz.broadcast(deserialize(i)),
                yaz: new Event(),
                yazSerialized: (a, i) => Global.yaz.broadcast(a, deserialize(i))
            };
            """);
    }

    [Fact]
    public void AwaitsWhenSerializingInAsyncFunctions ()
    {
        AddAssembly(
            With("public record Info;", false),
            With("[JSInvokable] public static Task<Info> Foo (Info i) => default;"),
            With("[JSFunction] public static Task<Info?> Bar (Info? i) => default;"),
            With("[JSInvokable] public static Task<IReadOnlyList<Info>> Baz () => default;"),
            With("[JSFunction] public static Task<IReadOnlyList<Info>> Yaz () => default;"));
        Execute();
        Contains(
            """
            export const Global = {
                foo: async (i) => deserialize(await getExports().MockClass.Foo(serialize(i))),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = async (i) => serialize(await this.barHandler(deserialize(i))); },
                get barSerialized() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Global.bar' from C#. Make sure to assign function in JavaScript."); return this.barSerializedHandler; },
                baz: async () => deserialize(await getExports().MockClass.Baz()),
                get yaz() { return this.yazHandler; },
                set yaz(handler) { this.yazHandler = handler; this.yazSerializedHandler = async () => serialize(await this.yazHandler()); },
                get yazSerialized() { if (typeof this.yazHandler !== "function") throw Error("Failed to invoke 'Global.yaz' from C#. Make sure to assign function in JavaScript."); return this.yazSerializedHandler; }
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
                getFoo: () => deserialize(getExports().n_MockClass.GetFoo()),
                Foo: { "0": "A", "1": "B", "A": 0, "B": 1 }
            };
            """);
    }
}
