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
        AddAssembly(WithClass("[JSInvokable] public static void Inv () {}"));
        Execute();
        Contains(
            """
            import { exports } from "./exports";
            import { Event } from "./event";
            import { registerInstance, getInstance, disposeOnFinalize } from "./instances";
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
                        nya: () => getExports().Foo_Bar_Class_Nya()
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
            export const Class = {
                onFoo: new Event(),
                onFooSerialized: () => Class.onFoo.broadcast(),
                onBar: new Event(),
                onBarSerialized: (a) => Class.onBar.broadcast(a),
                onBaz: new Event(),
                onBazSerialized: (a, b) => Class.onBaz.broadcast(a, b)
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
                    bar: () => getExports().Foo_Class_Bar()
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
                            bar: () => getExports().Foo_Bar_Nya_Class_Bar()
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
                    foo: () => getExports().Foo_Class_Foo()
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
        Contains("bar: () => getExports().Foo_Class_Bar()");
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
                        foo: () => getExports().Nya_Foo_Class_Foo()
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
                    method: () => getExports().Foo_Class_Method()
                }
            };
            export const FooBar = {
                Baz: {
                    Class: {
                        method: () => getExports().FooBar_Baz_Class_Method()
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
                    foo: () => getExports().Foo_Class_Foo()
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
            export const ClassA = {
                inv: () => getExports().ClassA_Inv()
            };
            export const ClassB = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'ClassB.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToClassObject ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static Task<int> Nya () => Task.FromResult(0);"),
            WithClass("[JSFunction] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Class = {
                nya: () => getExports().Class_Nya(),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; }
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
            export const Class = {
                fun: (fn) => getExports().Class_Fun(fn)
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
            export const Class = {
                foo: (i) => deserialize(getExports().Class_Foo(serialize(i))),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = (i) => serialize(this.barHandler(deserialize(i))); },
                get barSerialized() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Class.bar' from C#. Make sure to assign function in JavaScript."); return this.barSerializedHandler; },
                baz: new Event(),
                bazSerialized: (i) => Class.baz.broadcast(deserialize(i)),
                yaz: new Event(),
                yazSerialized: (a, i) => Class.yaz.broadcast(a, deserialize(i))
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
            export const Class = {
                foo: async (i) => deserialize(await getExports().Class_Foo(serialize(i))),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = async (i) => serialize(await this.barHandler(deserialize(i))); },
                get barSerialized() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Class.bar' from C#. Make sure to assign function in JavaScript."); return this.barSerializedHandler; },
                baz: async () => deserialize(await getExports().Class_Baz()),
                get yaz() { return this.yazHandler; },
                set yaz(handler) { this.yazHandler = handler; this.yazSerializedHandler = async () => serialize(await this.yazHandler()); },
                get yazSerialized() { if (typeof this.yazHandler !== "function") throw Error("Failed to invoke 'Class.yaz' from C#. Make sure to assign function in JavaScript."); return this.yazSerializedHandler; }
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
                    getFoo: () => deserialize(getExports().n_Class_GetFoo()),
                    Foo: { "0": "A", "1": "B", "A": 0, "B": 1 }
                }
            };
            """);
    }

    [Fact]
    public void CustomEnumIndexesArePreservedInJS ()
    {
        AddAssembly(
            With("n", "public enum Foo { A = 1, B = 6 }"),
            WithClass("n", "[JSInvokable] public static Foo GetFoo () => default;"));
        Execute();
        Contains(
            """
            export const n = {
                Foo: { "1": "A", "6": "B", "A": 1, "B": 6 },
                Class: {
                    getFoo: () => deserialize(getExports().n_Class_GetFoo())
                }
            };
            """);
    }

    [Fact]
    public void RespectsSpacePreference ()
    {
        AddAssembly(
            With(
                """
                [assembly: Bootsharp.JSPreferences(
                    Space = [@"^Foo\.Bar\.(\S+)", "$1"]
                )]
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
                    getNya: () => getExports().Foo_Bar_Nya_Class_GetNya()
                }
            };
            """);
    }

    [Fact]
    public void RespectsFunctionPreference ()
    {
        AddAssembly(
            With("""[assembly:JSPreferences(Function = [@".+", "foo"])]"""),
            WithClass("Space", "[JSInvokable] public static void Inv () {}")
        );
        Execute();
        Contains(
            """
            export const Space = {
                Class: {
                    foo: () => getExports().Space_Class_Inv()
                }
            };
            """);
    }

    [Fact]
    public void IgnoresBindingsInGeneratedNamespace ()
    {
        AddAssembly(With("Bootsharp.Generated",
            """
            public static class Exports { [JSInvokable] public static void Inv () {} }
            public static class Imports { [JSFunction] public static void Fun () {} }
            """));
        Execute();
        Assert.DoesNotContain("inv: () =>", TestedContent);
        Assert.DoesNotContain("get fun()", TestedContent);
    }

    [Fact]
    public void GeneratesForStaticInteropInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(Space.IExported))]
            [assembly:JSImport(typeof(Space.IImported))]

            namespace Space;

            public enum Enum { A, B }

            public interface IExported { void Inv (string s, Enum e); }
            public interface IImported { void Fun (string s, Enum e); void NotifyEvt (string s, Enum e); }
            """));
        Execute();
        Contains(
            """
            export const Space = {
                Enum: { "0": "A", "1": "B", "A": 0, "B": 1 },
                Exported: {
                    inv: (s, e) => getExports().Bootsharp_Generated_Exports_Space_JSExported_Inv(s, serialize(e))
                },
                Imported: {
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = (s, e) => this.funHandler(s, deserialize(e)); },
                    get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Space.Imported.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; },
                    onEvt: new Event(),
                    onEvtSerialized: (s, e) => Space.Imported.onEvt.broadcast(s, deserialize(e))
                }
            };
            """);
    }

    [Fact]
    public void GeneratesForStaticInteropInterfacesWithSpacePref ()
    {
        AddAssembly(With(
            """
            [assembly:JSPreferences(Space = [@".+", "Foo"])]
            [assembly:JSExport(typeof(Space.IExported))]
            [assembly:JSImport(typeof(Space.IImported))]

            namespace Space;

            public enum Enum { A, B }

            public interface IExported { void Inv (string s, Enum e); }
            public interface IImported { void Fun (string s, Enum e); void NotifyEvt (string s, Enum e); }
            """));
        Execute();
        Contains(
            """
            export const Foo = {
                inv: (s, e) => getExports().Bootsharp_Generated_Exports_Space_JSExported_Inv(s, serialize(e)),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = (s, e) => this.funHandler(s, deserialize(e)); },
                get funSerialized() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.fun' from C#. Make sure to assign function in JavaScript."); return this.funSerializedHandler; },
                onEvt: new Event(),
                onEvtSerialized: (s, e) => Foo.onEvt.broadcast(s, deserialize(e)),
                Enum: { "0": "A", "1": "B", "A": 0, "B": 1 }
            };
            """);
    }

    [Fact]
    public void GeneratesForInstancedInteropInterfaces ()
    {
        AddAssembly(With(
            """
            public enum Enum { A, B }

            public interface IExported { Enum Inv (string str); }
            public interface IImported { void NotifyEvt(string str); }

            namespace Space
            {
                public interface IExported { void Inv (Enum en); }
                public interface IImported { Enum Fun (Enum en); }
            }

            public class Class
            {
                [JSInvokable] public static Space.IExported GetExported (Space.IImported inst) => default;
                [JSFunction] public static IImported GetImported (IExported inst) => Proxies.Get<Func<IExported, IImported>>("Class.GetImported")(inst);
            }
            """));
        Execute();
        Contains(
            """
            class JSExported {
                constructor(_id) { this._id = _id; disposeOnFinalize(this); }
                inv(str) { return Exported.inv(this._id, str); }
            }
            class Space_JSExported {
                constructor(_id) { this._id = _id; disposeOnFinalize(this); }
                inv(en) { Space.Exported.inv(this._id, en); }
            }
            """);
        Contains(
            """
            export const Class = {
                getExported: (inst) => new Space_JSExported(getExports().Class_GetExported(registerInstance(inst))),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = (inst) => registerInstance(this.getImportedHandler(new JSExported((inst))); },
                get getImportedSerialized() { if (typeof this.getImportedHandler !== "function") throw Error("Failed to invoke 'Class.getImported' from C#. Make sure to assign function in JavaScript."); return this.getImportedSerializedHandler; }
            };
            export const Exported = {
                inv: (_id, str) => deserialize(getExports().Bootsharp_Generated_Exports_JSExported_Inv(_id, str))
            };
            export const Imported = {
                onEvtSerialized: (_id, str) => getInstance(_id).onEvt.broadcast(str)
            };
            export const Space = {
                Exported: {
                    inv: (_id, en) => getExports().Bootsharp_Generated_Exports_Space_JSExported_Inv(_id, serialize(en))
                },
                Imported: {
                    funSerialized: (_id, en) => serialize(getInstance(_id).fun(deserialize(en)))
                }
            };
            """);
    }
}
