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

            function getExports() { if (exports == null) throw Error("Boot the runtime before invoking C# APIs."); return exports; }
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
                        set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                        get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.Bar.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
                onFooMarshaled: () => Class.onFoo.broadcast(),
                onBar: new Event(),
                onBarMarshaled: (a) => Class.onBar.broadcast(a),
                onBaz: new Event(),
                onBazMarshaled: (a, b) => Class.onBaz.broadcast(a, b)
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
                        set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                        get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Bar.Nya.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
                    set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                    get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
                        set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                        get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Nya.Bar.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
                        set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                        get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Bar.Nya.Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
                set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'ClassB.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
                set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = () => this.funHandler(); },
                get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Class.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; }
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
    public void MarshalsCustomType ()
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
                foo: (i) => unmarshal_Info(getExports().Class_Foo(marshal_Info(i))),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barMarshaledHandler = (i) => marshal_Info(this.barHandler(unmarshal_Info(i))); },
                get barMarshaled() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Class.bar' from C#. Make sure to assign function in JavaScript."); return this.barMarshaledHandler; },
                baz: new Event(),
                bazMarshaled: (i) => Class.baz.broadcast(unmarshal_Info_Array(i)),
                yaz: new Event(),
                yazMarshaled: (a, i) => Class.yaz.broadcast(a, unmarshal_Info(i))
            };
            """);
    }

    [Fact]
    public void AwaitsWhenMarshalingInAsyncFunctions ()
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
                foo: async (i) => unmarshal_Info(await getExports().Class_Foo(marshal_Info(i))),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barMarshaledHandler = async (i) => marshal_Info(await this.barHandler(unmarshal_Info(i))); },
                get barMarshaled() { if (typeof this.barHandler !== "function") throw Error("Failed to invoke 'Class.bar' from C#. Make sure to assign function in JavaScript."); return this.barMarshaledHandler; },
                baz: async () => unmarshal_System_Collections_Generic_IReadOnlyList_Info(await getExports().Class_Baz()),
                get yaz() { return this.yazHandler; },
                set yaz(handler) { this.yazHandler = handler; this.yazMarshaledHandler = async () => marshal_System_Collections_Generic_IReadOnlyList_Info(await this.yazHandler()); },
                get yazMarshaled() { if (typeof this.yazHandler !== "function") throw Error("Failed to invoke 'Class.yaz' from C#. Make sure to assign function in JavaScript."); return this.yazMarshaledHandler; }
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
                    getFoo: () => unmarshal_n_Foo(getExports().n_Class_GetFoo()),
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
                    getFoo: () => unmarshal_n_Foo(getExports().n_Class_GetFoo())
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
                    set onFun(handler) { this.onFunHandler = handler; this.onFunMarshaledHandler = () => this.onFunHandler(); },
                    get onFunMarshaled() { if (typeof this.onFunHandler !== "function") throw Error("Failed to invoke 'Fun.Class.onFun' from C#. Make sure to assign function in JavaScript."); return this.onFunMarshaledHandler; }
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
        DoesNotContain("inv: () =>");
        DoesNotContain("get fun()");
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
                    inv: (s, e) => getExports().Bootsharp_Generated_Exports_Space_JSExported_Inv(s, marshal_Space_Enum(e))
                },
                Imported: {
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = (s, e) => this.funHandler(s, unmarshal_Space_Enum(e)); },
                    get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Space.Imported.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; },
                    onEvt: new Event(),
                    onEvtMarshaled: (s, e) => Space.Imported.onEvt.broadcast(s, unmarshal_Space_Enum(e))
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
                inv: (s, e) => getExports().Bootsharp_Generated_Exports_Space_JSExported_Inv(s, marshal_Space_Enum(e)),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funMarshaledHandler = (s, e) => this.funHandler(s, unmarshal_Space_Enum(e)); },
                get funMarshaled() { if (typeof this.funHandler !== "function") throw Error("Failed to invoke 'Foo.fun' from C#. Make sure to assign function in JavaScript."); return this.funMarshaledHandler; },
                onEvt: new Event(),
                onEvtMarshaled: (s, e) => Foo.onEvt.broadcast(s, unmarshal_Space_Enum(e)),
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
                [JSInvokable] public static Task<Space.IExported> GetExported (Space.IImported inst) => default;
                [JSFunction] public static Task<IImported> GetImported (IExported inst) => Proxies.Get<Func<IExported, Task<IImported>>>("Class.GetImported")(inst);
            }
            """));
        Execute();
        Contains(
            """
            class Space_JSExported {
                constructor(_id) { this._id = _id; disposeOnFinalize(this, _id); }
                inv(en) { Space.Exported.inv(this._id, en); }
            }
            class JSExported {
                constructor(_id) { this._id = _id; disposeOnFinalize(this, _id); }
                inv(str) { return Exported.inv(this._id, str); }
            }
            """);
        Contains(
            """
            export const Class = {
                getExported: async (inst) => new Space_JSExported(await getExports().Class_GetExported(registerInstance(inst))),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedMarshaledHandler = async (inst) => registerInstance(await this.getImportedHandler(new JSExported(inst))); },
                get getImportedMarshaled() { if (typeof this.getImportedHandler !== "function") throw Error("Failed to invoke 'Class.getImported' from C#. Make sure to assign function in JavaScript."); return this.getImportedMarshaledHandler; }
            };
            export const Exported = {
                inv: (_id, str) => unmarshal_Enum(getExports().Bootsharp_Generated_Exports_JSExported_Inv(_id, str))
            };
            export const Imported = {
                onEvtMarshaled: (_id, str) => getInstance(_id).onEvt.broadcast(str)
            };
            export const Space = {
                Exported: {
                    inv: (_id, en) => getExports().Bootsharp_Generated_Exports_Space_JSExported_Inv(_id, marshal_Enum(en))
                },
                Imported: {
                    funMarshaled: (_id, en) => marshal_Enum(getInstance(_id).fun(unmarshal_Enum(en)))
                }
            };
            """);
    }

    [Fact]
    public void IgnoresImplementedInterfaceMethods ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]
            [assembly:JSImport(typeof(IImportedStatic))]

            public interface IExportedStatic { int Foo () => 0; }
            public interface IImportedStatic { int Foo () => 0; }
            public interface IExportedInstanced { int Foo () => 0; }
            public interface IImportedInstanced { int Foo () => 0; }

            public class Class
            {
                [JSInvokable] public static IExportedInstanced GetExported () => default;
                [JSFunction] public static IImportedInstanced GetImported () => default;
            }
            """));
        Execute();
        DoesNotContain("Foo");
    }

    [Fact]
    public void GeneratesMarshalMethods ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public enum Enum { Foo, Bar }
            public struct Struct { public int Int { get; set; } public Enum Enum { get; set; } }
            public record Record (string String, int Int, int? NullInt, byte[] ByteArr, Struct Struct,
                IReadOnlyList<byte> ByteList, IList<Struct> StructList, IReadOnlyDictionary<int, string> Dict,
                Dictionary<Struct, Struct> StructDict);

            public class Class
            {
                [JSInvokable] public static void Inv (Record r) { }
            }
            """));
        Execute();
        Contains("function marshal_Space_Struct(obj) { return [ obj.int, obj.enum ]; }");
        Contains("function marshal_Space_Record(obj) { return obj == null ? null : [ obj.string == null ? null : obj.string, obj.int, obj.nullInt == null ? null : obj.nullInt, obj.byteArr == null ? null : obj.byteArr, marshal_Space_Struct(obj.struct), obj.byteList == null ? null : obj.byteList, obj.structList == null ? null : obj.structList.map(marshal_Space_Struct), obj.dict == null ? null : [...obj.dict.keys(), ...obj.dict.values()], obj.structDict == null ? null : [...Array.from(obj.structDict.keys(), marshal_Space_Struct), ...Array.from(obj.structDict.values(), marshal_Space_Struct)] ]; }");
    }

    [Fact]
    public void GeneratesUnmarshalMethods ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public enum Enum { Foo, Bar }
            public struct Struct { public int Int { get; set; } public Enum Enum { get; set; } }
            public record Record (string String, int Int, int? NullInt, byte[] ByteArr, Struct Struct,
                IReadOnlyList<byte> ByteList, IList<Struct> StructList, IReadOnlyDictionary<int, string> Dict,
                Dictionary<Struct, Struct> StructDict, List<string> StringList, string[] StringArray);

            public class Class
            {
                [JSInvokable] public static Record Inv () => default;
            }
            """));
        Execute();
        Contains("function unmarshal_Space_Struct(raw) { return { int: raw[0], enum: raw[1] }; }");
        Contains("function unmarshal_Space_Record(raw) { return raw == null ? undefined : { string: raw[0] == null ? undefined : raw[0], int: raw[1], nullInt: raw[2] == null ? undefined : raw[2], byteArr: raw[3] == null ? undefined : raw[3], struct: unmarshal_Space_Struct(raw[4]), byteList: raw[5] == null ? undefined : raw[5], structList: raw[6] == null ? undefined : raw[6].map(unmarshal_Space_Struct), dict: raw[7] == null ? undefined : new Map(raw[7].slice(0, raw[7].length / 2).map((obj, idx) => [obj, raw[7][idx + raw[7].length / 2] == null ? undefined : raw[7][idx + raw[7].length / 2]])), structDict: raw[8] == null ? undefined : new Map(raw[8].slice(0, raw[8].length / 2).map((obj, idx) => [unmarshal_Space_Struct(obj), unmarshal_Space_Struct(raw[8][idx + raw[8].length / 2])])), stringList: raw[9] == null ? undefined : raw[9], stringArray: raw[10] == null ? undefined : raw[10] }; }");
    }
}
