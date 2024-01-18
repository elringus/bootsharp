namespace Bootsharp.Publish.Test;

public class BindingTest : PackTest
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
        AddAssembly(WithClass("Foo", "[JSInvokable] public static void Bar () {}"));
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
        AddAssembly(WithClass("Foo.Bar", "[JSInvokable] public static void Nya () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Class: {
                        nya: () => getExports().Foo_Bar_Class.Nya()
                    }
                }
            };
            """);
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        AddAssembly(WithClass("Foo.Bar", "[JSFunction] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.Bar.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                    }
                }
            };
            """);
    }

    [Fact]
    public void BindingForEventMethodIsGenerated ()
    {
        AddAssembly(
            WithClass("[JSEvent] public static void OnFoo () {}"),
            WithClass("[JSEvent] public static void OnBar (string a) {}"),
            WithClass("[JSEvent] public static void OnBaz (int a, bool b) {}"));
        Execute();
        Contains(
            """
            export const Global = {
                Class: {
                    onFoo: new Event(),
                    onFooSerialized: () => Global.Class.onFoo.broadcast(),
                    onBar: new Event(),
                    onBarSerialized: (a) => Global.Class.onBar.broadcast(a),
                    onBaz: new Event(),
                    onBazSerialized: (a, b) => Global.Class.onBaz.broadcast(a, b)
                }
            };
            """);
    }

    [Fact]
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly(WithClass("Foo", "[JSInvokable] public static void Bar () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Class: {
                    bar: () => getExports().Foo_Class.Bar()
                }
            };
            """);
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPart ()
    {
        AddAssembly(WithClass("Foo.Bar.Nya", "[JSInvokable] public static void Bar () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Nya: {
                        Class: {
                            bar: () => getExports().Foo_Bar_Nya_Class.Bar()
                        }
                    }
                }
            };
            """);
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnObject ()
    {
        AddAssembly(
            WithClass("Foo", "[JSInvokable] public static void Foo () {}"),
            WithClass("Bar.Nya", "[JSFunction] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Bar.Nya.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                    }
                }
            };
            export const Foo = {
                Class: {
                    foo: () => getExports().Foo_Class.Foo()
                }
            };
            """);
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(WithClass("Foo", "[JSInvokable] public static void Bar () {}"));
        AddAssembly(WithClass("Foo", "[JSFunction] public static void Fun () {}"));
        Execute();
        Assert.Single(Matches("export const Foo"));
        Contains("bar: () => getExports().Foo_Class.Bar()");
        Contains(
            """
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
            """);
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedUnderSameObject ()
    {
        AddAssembly(
            WithClass("Nya.Foo", "[JSInvokable] public static void Foo () {}"),
            WithClass("Nya.Bar", "[JSFunction] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Nya = {
                Bar: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Nya.Bar.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                    }
                },
                Foo: {
                    Class: {
                        foo: () => getExports().Nya_Foo_Class.Foo()
                    }
                }
            };
            """);
    }

    [Fact]
    public void DifferentSpacesStartingEquallyAreNotAssignedToSameObject ()
    {
        AddAssembly(
            WithClass("Foo", "[JSInvokable] public static void Method () {}"),
            WithClass("FooBar.Baz", "[JSInvokable] public static void Method () {}")
        );
        Execute();
        Contains(
            """
            export const Foo = {
                Class: {
                    method: () => getExports().Foo_Class.Method()
                }
            };
            export const FooBar = {
                Baz: {
                    Class: {
                        method: () => getExports().FooBar_Baz_Class.Method()
                    }
                }
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly(WithClass("Foo", "[JSInvokable] public static int Foo () => 0;"));
        AddAssembly(WithClass("Bar.Nya", "[JSFunction] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Bar.Nya.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                    }
                }
            };
            export const Foo = {
                Class: {
                    foo: () => getExports().Foo_Class.Foo()
                }
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleClassesAssignedToRespectiveObjects ()
    {
        AddAssembly(
            With("public class ClassA { [JSInvokable] public static void Inv () {} }"),
            With("public class ClassB { [JSFunction] public static void Fun () {} }"));
        Execute();
        Contains(
            """
            export const Global = {
                ClassA: {
                    inv: () => getExports().ClassA.Inv()
                },
                ClassB: {
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Global.ClassB.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                }
            };
            """);
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToGlobalObject ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static Task<int> Nya () => Task.FromResult(0);"),
            WithClass("[JSFunction] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Global = {
                Class: {
                    nya: () => getExports().Class.Nya(),
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Global.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
                }
            };
            """);
    }

    [Fact]
    public void PrefsAllowsOverridingObjectNames ()
    {
        AddAssembly(
            // With("""[assembly:JSNamespace(@"Foo\.Bar\.(\S+)", "$1")]"""),
            With(
                """
                [assembly:JSConfiguration<Prefs>]
                public class Prefs : Bootsharp.Preferences
                {
                    public override string BuildSpace (Type type, string @default) => @default.Replace("Foo.Bar.", "");
                }
                """),
            WithClass("Foo.Bar.Nya", "[JSInvokable] public static Task GetNya () => Task.CompletedTask;"),
            WithClass("Foo.Bar.Fun", "[JSFunction] public static void OnFun () {}"));
        Execute();
        Contains(
            """
            export const Fun = {
                Class: {
                    get onFun() { return this.onFunHandler; },
                    set onFun(handler) { this.onFunHandler = handler; this.onFunSerializedHandler = () => this.onFunHandler(); },
                    get onFunSerialized() { if (typeof this.onFunHandler !== "function") throw Error("Failed to invoke 'Fun.Class.onFun' from C#. Make sure to assign function in JavaScript."); return this.onFunSerializedHandler; }
                }
            };
            export const Nya = {
                Class: {
                    getNya: () => getExports().Foo_Bar_Nya_Class.GetNya()
                }
            };
            """);
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Fun (string function) {}"));
        Execute();
        Contains(
            """
            export const Global = {
                Class: {
                    fun: (fn) => getExports().Class.Fun(fn)
                }
            };
            """);
    }

    [Fact]
    public void SerializesCustomType ()
    {
        AddAssembly(
            With("public record Info;"),
            WithClass("[JSInvokable] public static Info Foo (Info i) => default;"),
            WithClass("[JSFunction] public static Info? Bar (Info? i) => default;"),
            WithClass("[JSEvent] public static void Baz (Info?[] i) {}"),
            WithClass("[JSEvent] public static void Yaz (int a, Info i) {}"));
        Execute();
        Contains(
            """
            export const Global = {
                Class: {
                    foo: (i) => deserialize(getExports().Class.Foo(serialize(i))),
                    get bar() { return this.barHandler; },
                    set bar(handler) { this.barHandler = handler; this.barSerializedHandler = (i) => serialize(this.barHandler(deserialize(i))); },
                    get barSerialized() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Global.Class.bar' from C#. Make sure to assign function in JavaScript."); return this.barSerializedHandler; },
                    baz: new Event(),
                    bazSerialized: (i) => Global.Class.baz.broadcast(deserialize(i)),
                    yaz: new Event(),
                    yazSerialized: (a, i) => Global.Class.yaz.broadcast(a, deserialize(i))
                }
            };
            """);
    }

    [Fact]
    public void AwaitsWhenSerializingInAsyncFunctions ()
    {
        AddAssembly(
            With("public record Info;"),
            WithClass("[JSInvokable] public static Task<Info> Foo (Info i) => default;"),
            WithClass("[JSFunction] public static Task<Info?> Bar (Info? i) => default;"),
            WithClass("[JSInvokable] public static Task<IReadOnlyList<Info>> Baz () => default;"),
            WithClass("[JSFunction] public static Task<IReadOnlyList<Info>> Yaz () => default;"));
        Execute();
        Contains(
            """
            export const Global = {
                Class: {
                    foo: async (i) => deserialize(await getExports().Class.Foo(serialize(i))),
                    get bar() { return this.barHandler; },
                    set bar(handler) { this.barHandler = handler; this.barSerializedHandler = async (i) => serialize(await this.barHandler(deserialize(i))); },
                    get barSerialized() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Global.Class.bar' from C#. Make sure to assign function in JavaScript."); return this.barSerializedHandler; },
                    baz: async () => deserialize(await getExports().Class.Baz()),
                    get yaz() { return this.yazHandler; },
                    set yaz(handler) { this.yazHandler = handler; this.yazSerializedHandler = async () => serialize(await this.yazHandler()); },
                    get yazSerialized() { if (typeof this.yazHandler !== "function") throw Error("Failed to invoke 'Global.Class.yaz' from C#. Make sure to assign function in JavaScript."); return this.yazSerializedHandler; }
                }
            };
            """);
    }

    [Fact]
    public void ExportedEnumsAreDeclaredInJS ()
    {
        AddAssembly(
            WithClass("n", "public enum Foo { A, B }"),
            WithClass("n", "[JSInvokable] public static Foo GetFoo () => default;"));
        Execute();
        Contains(
            """
            export const n = {
                Class: {
                    getFoo: () => deserialize(getExports().n_Class.GetFoo()),
                    Foo: {
                        "0": "A", "1": "B", "A": 0, "B": 1
                    }
                }
            };
            """);
    }

    [Fact]
    public void CustomEnumIndexesArePreservedInJS ()
    {
        AddAssembly(
            WithClass("n", "public enum Foo { A = 1, B = 6 }"),
            WithClass("n", "[JSInvokable] public static Foo GetFoo () => default;"));
        Execute();
        Contains(
            """
            export const n = {
                Class: {
                    getFoo: () => deserialize(getExports().n_Class.GetFoo()),
                    Foo: {
                        "1": "A", "6": "B", "A": 1, "B": 6
                    }
                }
            };
            """);
    }
}
