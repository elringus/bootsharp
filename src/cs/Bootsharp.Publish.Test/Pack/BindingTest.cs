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
        AddAssembly(WithClass("[Export] public static void Inv () {}"));
        Execute();
        Contains(
            """
            import { exports } from "./exports";
            import { Event } from "./event";
            import { instances } from "./instances";
            """);
    }

    [Fact]
    public void WhenDebugEnabledEmitsAndUsesExportImportHelpers ()
    {
        Task.Debug = true;
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]

            public interface IExportedStatic { int State { get; set; } }
            public interface IImportedInstanced { event Action? Changed; }

            public partial class Class
            {
                [Import] public static event Action? Evt;
                [Export] public static Task<int> InvAsync () => Task.FromResult(0);
                [Export] public static void UseImported (IImportedInstanced inst) {}
                [Import] public static void Fun () {}
            }
            """));
        Execute();
        Contains("function getExport");
        Contains("function getImport");
        Contains("""getExport("Class_InvokeEvt")""");
        Contains("""getExport("Class_InvAsync")""");
        Contains("""getExport("Bootsharp_Generated_Imports_JSImportedInstanced_InvokeChanged")""");
        Contains("""getExport("Bootsharp_Generated_Exports_JSExportedStatic_GetPropertyState")""");
        Contains("""getImport(this.funHandler, this.funSerializedHandler, "Class.fun")""");
    }

    [Fact]
    public void WhenDebugDisabledDoesntEmitAndDoesntUseExportImportHelpers ()
    {
        Task.Debug = false;
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]

            public interface IExportedStatic { int State { get; set; } }

            public class Class
            {
                [Export] public static Task<int> InvAsync () => Task.FromResult(0);
                [Import] public static void Fun () {}
            }
            """));
        Execute();
        DoesNotContain("function getExport");
        DoesNotContain("function getImport");
        DoesNotContain("""getExport("Class_InvAsync")""");
        DoesNotContain("""getExport("Bootsharp_Generated_Exports_JSExportedStatic_GetPropertyState")""");
        DoesNotContain("""getImport(this.funHandler, this.funSerializedHandler, "Class.fun")""");
    }

    [Fact]
    public void BindingForInvokableMethodIsGenerated ()
    {
        AddAssembly(WithClass("Foo.Bar", "[Export] public static void Nya () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Class: {
                        nya: () => exports.Foo_Bar_Class_Nya()
                    }
                }
            };
            """);
    }

    [Fact]
    public void BindingForFunctionMethodIsGenerated ()
    {
        AddAssembly(WithClass("Foo.Bar", "[Import] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { return this.funSerializedHandler; }
                    }
                }
            };
            """);
    }

    [Fact]
    public void BindingForStaticEventsIsGenerated ()
    {
        AddAssembly(
            WithClass("[Export] public static event Action? ExpEvt;"),
            WithClass("[Export] public static event Action<string>? Evt;"),
            WithClass("[Import] public static event Action<int, bool>? ImpEvt;"));
        Execute();
        Contains(
            """
            export const Class = {
                expEvt: new Event(),
                broadcastExpEvtSerialized: () => Class.expEvt.broadcast(),
                evt: new Event(),
                broadcastEvtSerialized: (obj) => Class.evt.broadcast(obj),
                impEvt: importEvent((arg1, arg2) => exports.Class_InvokeImpEvt(arg1, arg2))
            };
            """);
    }

    [Fact]
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Bar () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Class: {
                    bar: () => exports.Foo_Class_Bar()
                }
            };
            """);
    }

    [Fact]
    public void WhenSpaceContainDotsObjectCreatedForEachPart ()
    {
        AddAssembly(WithClass("Foo.Bar.Nya", "[Export] public static void Bar () {}"));
        Execute();
        Contains(
            """
            export const Foo = {
                Bar: {
                    Nya: {
                        Class: {
                            bar: () => exports.Foo_Bar_Nya_Class_Bar()
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
            WithClass("Foo", "[Export] public static void Foo () {}"),
            WithClass("Bar.Nya", "[Import] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { return this.funSerializedHandler; }
                    }
                }
            };
            export const Foo = {
                Class: {
                    foo: () => exports.Foo_Class_Foo()
                }
            };
            """);
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Bar () {}"));
        AddAssembly(WithClass("Foo", "[Import] public static void Fun () {}"));
        Execute();
        Once("export const Foo");
        Contains("bar: () => exports.Foo_Class_Bar()");
        Contains(
            """
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                    get funSerialized() { return this.funSerializedHandler; }
            """);
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedUnderSameObject ()
    {
        AddAssembly(
            WithClass("Nya.Foo", "[Export] public static void Foo () {}"),
            WithClass("Nya.Bar", "[Import] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Nya = {
                Bar: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { return this.funSerializedHandler; }
                    }
                },
                Foo: {
                    Class: {
                        foo: () => exports.Nya_Foo_Class_Foo()
                    }
                }
            };
            """);
    }

    [Fact]
    public void DifferentSpacesStartingEquallyAreNotAssignedToSameObject ()
    {
        AddAssembly(
            WithClass("Foo", "[Export] public static void Method () {}"),
            WithClass("FooBar.Baz", "[Export] public static void Method () {}")
        );
        Execute();
        Contains(
            """
            export const Foo = {
                Class: {
                    method: () => exports.Foo_Class_Method()
                }
            };
            export const FooBar = {
                Baz: {
                    Class: {
                        method: () => exports.FooBar_Baz_Class_Method()
                    }
                }
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveObjects ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static int Foo () => 0;"));
        AddAssembly(WithClass("Bar.Nya", "[Import] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Bar = {
                Nya: {
                    Class: {
                        get fun() { return this.funHandler; },
                        set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                        get funSerialized() { return this.funSerializedHandler; }
                    }
                }
            };
            export const Foo = {
                Class: {
                    foo: () => exports.Foo_Class_Foo()
                }
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleClassesAssignedToRespectiveObjects ()
    {
        AddAssembly(
            With("public class ClassA { [Export] public static void Inv () {} }"),
            With("public class ClassB { [Import] public static void Fun () {} }"));
        Execute();
        Contains(
            """
            export const ClassA = {
                inv: () => exports.ClassA_Inv()
            };
            export const ClassB = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void WhenNoSpaceBindingsAreAssignedToClassObject ()
    {
        AddAssembly(
            WithClass("[Export] public static Task<int> Nya () => Task.FromResult(0);"),
            WithClass("[Import] public static void Fun () {}"));
        Execute();
        Contains(
            """
            export const Class = {
                nya: () => exports.Class_Nya(),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void VariablesConflictingWithJSTypesAreRenamed ()
    {
        AddAssembly(WithClass("[Export] public static void Fun (string function) {}"));
        Execute();
        Contains(
            """
            export const Class = {
                fun: (fn) => exports.Class_Fun(fn)
            };
            """);
    }

    [Fact]
    public void SerializesUserType ()
    {
        AddAssembly(
            With("public record Info (DateTimeOffset Date, nint Ptr, Info? Self);"),
            WithClass("[Export] public static event Action<Info?[]?, Info>? ExpEvt;"),
            WithClass("[Import] public static event Action<int, Info>? ImpEvt;"),
            WithClass("[Export] public static Info Foo (Info i) => default;"),
            WithClass("[Import] public static Info? Bar (Info? i) => default;"));
        Execute();
        Contains(
            """
            export const Class = {
                expEvt: new Event(),
                broadcastExpEvtSerialized: (arg1, arg2) => Class.expEvt.broadcast(deserialize(arg1, InfoArray) ?? undefined, deserialize(arg2, Info)),
                impEvt: importEvent((arg1, arg2) => exports.Class_InvokeImpEvt(arg1, serialize(arg2, Info))),
                foo: (i) => deserialize(exports.Class_Foo(serialize(i, Info)), Info),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = (i) => serialize(this.barHandler(deserialize(i, Info)), Info); },
                get barSerialized() { return this.barSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void SerializesPrimitivesUnderUserType ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public struct Structure;
            public enum Enumeration { A, B }

            public record Node(
                bool Boolean,
                byte Byte,
                sbyte SByte,
                short Int16,
                ushort UInt16,
                uint UInt32,
                long Int64,
                ulong UInt64,
                float Single,
                decimal Decimal,
                char Char,
                string String,
                DateTime DateTime,
                DateTimeOffset DateTimeOffset,
                nint NInt,
                int Int,
                int? NullableInt,
                Structure Struct,
                Structure? NullableStruct,
                Enumeration Enum,
                Enumeration? NullableEnum);

            public class Class
            {
                [Export] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains("const System_Boolean = types.Boolean;");
        Contains("const System_Byte = types.Byte;");
        Contains("const System_SByte = types.SByte;");
        Contains("const System_Int16 = types.Int16;");
        Contains("const System_UInt16 = types.UInt16;");
        Contains("const System_UInt32 = types.UInt32;");
        Contains("const System_Int64 = types.Int64;");
        Contains("const System_UInt64 = types.UInt64;");
        Contains("const System_Single = types.Single;");
        Contains("const System_Decimal = types.Decimal;");
        Contains("const System_Char = types.Char;");
        Contains("const System_String = types.String;");
        Contains("const System_DateTime = types.DateTime;");
        Contains("const System_DateTimeOffset = types.DateTimeOffset;");
        Contains("const System_IntPtr = types.IntPtr;");
        Contains("const System_Int32 = types.Int32;");
        Contains("const System_Int32OrNull = types.Nullable(System_Int32);");
        Contains("const Space_Structure = binary(write_Space_Structure, read_Space_Structure);");
        Contains("const Space_StructureOrNull = types.Nullable(Space_Structure);");
        Contains("const Space_Enumeration = types.Int32;");
        Contains("const Space_EnumerationOrNull = types.Nullable(Space_Enumeration);");
    }

    [Fact]
    public void OrdersSelfReferencedElementBeforeCollection ()
    {
        AddAssembly(With(
            """
            public record Node (List<Node>? Children);

            public class Class
            {
                [Export] public static Node Echo (Node node) => node;
            }
            """));
        Execute();
        Contains(
            """
            const Node = binary(write_Node, read_Node);
            const System_Collections_Generic_List_Of_Node = types.List(Node);
            """);
    }

    [Fact]
    public void AwaitsWhenSerializingInAsyncFunctions ()
    {
        AddAssembly(
            With("public record Info;"),
            WithClass("[Export] public static Task<Info> Foo (Info i) => default;"),
            WithClass("[Import] public static Task<Info?> Bar (Info? i) => default;"),
            WithClass("[Export] public static Task<IReadOnlyList<Info>> Baz () => default;"),
            WithClass("[Import] public static Task<IReadOnlyList<Info>> Yaz () => default;"));
        Execute();
        Contains(
            """
            export const Class = {
                foo: async (i) => deserialize(await exports.Class_Foo(serialize(i, Info)), Info),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = async (i) => serialize(await this.barHandler(deserialize(i, Info)), Info); },
                get barSerialized() { return this.barSerializedHandler; },
                baz: async () => deserialize(await exports.Class_Baz(), System_Collections_Generic_IReadOnlyList_Of_Info),
                get yaz() { return this.yazHandler; },
                set yaz(handler) { this.yazHandler = handler; this.yazSerializedHandler = async () => serialize(await this.yazHandler(), System_Collections_Generic_IReadOnlyList_Of_Info); },
                get yazSerialized() { return this.yazSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void ExportedEnumsAreDeclaredInJS ()
    {
        AddAssembly(
            WithClass("n", "public enum Foo { A, B }"),
            WithClass("n", "[Export] public static Foo GetFoo () => default;"));
        Execute();
        Contains(
            """
            export const n = {
                Class: {
                    getFoo: () => deserialize(exports.n_Class_GetFoo(), n_Foo),
                    Foo: { "0": "A", "1": "B", "A": 0, "B": 1 }
                }
            };
            """);
    }

    [Fact]
    public void DoesntDeclareSystemEnums ()
    {
        AddAssembly(
            WithClass("n", "public enum Foo { A, B }"),
            WithClass("n", "[Export] public static Task<Foo> GetFoo () => default;"));
        Execute();
        Contains("Foo");
        DoesNotContain("LayoutKind");
        DoesNotContain("SecurityRuleSet");
        DoesNotContain("MethodAttributes");
        DoesNotContain("MethodImplAttributes");
    }

    [Fact]
    public void CustomEnumIndexesArePreservedInJS ()
    {
        AddAssembly(
            With("n", "public enum Foo { A = 1, B = 6 }"),
            WithClass("n", "[Export] public static Foo GetFoo () => default;"));
        Execute();
        Contains(
            """
            export const n = {
                Foo: { "1": "A", "6": "B", "A": 1, "B": 6 },
                Class: {
                    getFoo: () => deserialize(exports.n_Class_GetFoo(), n_Foo)
                }
            };
            """);
    }

    [Fact]
    public void RespectsSpacePreferenceInStaticMembers ()
    {
        AddAssembly(
            With(
                """
                [assembly: Bootsharp.Preferences(
                    Space = [@"^Foo\.Bar\.(\S+)", "$1"]
                )]
                """),
            WithClass("Foo.Bar.Nya", "[Export] public static Task GetNya () => Task.CompletedTask;"),
            WithClass("Foo.Bar.Fun", "[Import] public static void OnFun () {}"));
        Execute();
        Contains(
            """
            export const Fun = {
                Class: {
                    get onFun() { return this.onFunHandler; },
                    set onFun(handler) { this.onFunHandler = handler; this.onFunSerializedHandler = () => this.onFunHandler(); },
                    get onFunSerialized() { return this.onFunSerializedHandler; }
                }
            };
            export const Nya = {
                Class: {
                    getNya: () => exports.Foo_Bar_Nya_Class_GetNya()
                }
            };
            """);
    }

    [Fact]
    public void RespectsSpacePreferenceInStaticInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:Preferences(Space = [@".+", "Foo"])]
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public interface IExported { void Inv (); }
            public interface IImported { void Fun (); }
            """));
        Execute();
        Contains(
            """
            export const Foo = {
                inv: () => exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void RespectsFunctionPreference ()
    {
        AddAssembly(
            With("""[assembly:Preferences(Function = [@".+", "foo"])]"""),
            WithClass("Space", "[Export] public static void Inv () {}")
        );
        Execute();
        Contains(
            """
            export const Space = {
                Class: {
                    foo: () => exports.Space_Class_Inv()
                }
            };
            """);
    }

    [Fact]
    public void IgnoresBindingsInGeneratedNamespace ()
    {
        AddAssembly(With("Bootsharp.Generated",
            """
            public static class Exports { [Export] public static void Inv () {} }
            public static class Imports { [Import] public static void Fun () {} }
            """));
        Execute();
        DoesNotContain("inv: () =>");
        DoesNotContain("get fun()");
    }

    [Fact]
    public void GeneratesForMethodsInStaticInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Info (string Value);

            public interface IExported { Info Inv (string str, Info info); }
            public interface IImported { Info Fun (string str, Info info); }
            """));
        Execute();
        Contains(
            """
            export const Space = {
                Exported: {
                    inv: (str, info) => deserialize(exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(str, serialize(info, Space_Info)), Space_Info)
                },
                Imported: {
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = (str, info) => serialize(this.funHandler(str, deserialize(info, Space_Info)), Space_Info); },
                    get funSerialized() { return this.funSerializedHandler; }
                }
            };
            """);
    }

    [Fact]
    public void GeneratesForMethodsInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { Info Inv (IExported inst, Info info); }
            public interface IImported { Info Fun (IImported inst, Info info); }

            public partial class Class
            {
                [Export] public static Task<IExported> GetExported (IImported inst) => default;
                [Import] public static Task<IImported> GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            class JSExported {
                constructor(_id) { this._id = _id; }
                inv(inst, info) { return Exported.inv(this._id, inst, info); }
            }

            export const Class = {
                getExported: async (inst) => instances.export(await exports.Class_GetExported(instances.import(inst)), id => new JSExported(id)),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = async (inst) => instances.import(await this.getImportedHandler(instances.export(inst, id => new JSExported(id)))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const Exported = {
                inv: (_id, inst, info) => deserialize(exports.Bootsharp_Generated_Exports_JSExported_Inv(_id, inst._id, serialize(info, Info)), Info)
            };
            export const Imported = {
                funSerialized: (_id, inst, info) => serialize(instances.imported(_id).fun(instances.imported(inst), deserialize(info, Info)), Info)
            };
            """);
    }

    [Fact]
    public void GeneratesForPropertiesInStaticInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Info (string Value);

            public interface IExported
            {
                Info? State { get; set; }
                int Count { set; }
            }

            public interface IImported
            {
                Info? State { get; set; }
                int Count { set; }
            }
            """));
        Execute();
        Contains(
            """
            export const Space = {
                Exported: {
                    get state() { return deserialize(exports.Bootsharp_Generated_Exports_Space_JSExported_GetPropertyState(), Space_Info) ?? undefined; },
                    set state(value) { exports.Bootsharp_Generated_Exports_Space_JSExported_SetPropertyState(serialize(value, Space_Info)); },
                    set count(value) { exports.Bootsharp_Generated_Exports_Space_JSExported_SetPropertyCount(value); }
                },
                Imported: {
                    get state() { return this._state; },
                    getPropertyStateSerialized() { return serialize(this.state, Space_Info); },
                    set state(value) { this._state = value; },
                    setPropertyStateSerialized(value) { this.state = deserialize(value, Space_Info); },
                    set count(value) { this._count = value; },
                    setPropertyCountSerialized(value) { this.count = value; }
                }
            };
            """);
    }

    [Fact]
    public void GeneratesForPropertiesInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported
            {
                Info? State { get; set; }
                IExported Exported { get; }
                IImported Imported { set; }
            }

            public interface IImported
            {
                Info? State { get; set; }
                IImported Imported { get; }
                IExported Exported { set; }
            }

            public partial class Class
            {
                [Export] public static IExported GetExported (IImported inst) => default;
                [Import] public static IImported GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            class JSExported {
                constructor(_id) { this._id = _id; }
                get state() { return Exported.getPropertyState(this._id); }
                set state(value) { Exported.setPropertyState(this._id, value); }
                get exported() { return Exported.getPropertyExported(this._id); }
                set imported(value) { Exported.setPropertyImported(this._id, value); }
            }

            export const Class = {
                getExported: (inst) => instances.export(exports.Class_GetExported(instances.import(inst)), id => new JSExported(id)),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = (inst) => instances.import(this.getImportedHandler(instances.export(inst, id => new JSExported(id)))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const Exported = {
                getPropertyState(_id) { return deserialize(exports.Bootsharp_Generated_Exports_JSExported_GetPropertyState(_id), Info) ?? undefined; },
                setPropertyState(_id, value) { exports.Bootsharp_Generated_Exports_JSExported_SetPropertyState(_id, serialize(value, Info)); },
                getPropertyExported(_id) { return instances.export(exports.Bootsharp_Generated_Exports_JSExported_GetPropertyExported(_id), id => new JSExported(id)); },
                setPropertyImported(_id, value) { exports.Bootsharp_Generated_Exports_JSExported_SetPropertyImported(_id, instances.import(value)); }
            };
            export const Imported = {
                getPropertyStateSerialized(_id) { return serialize(instances.imported(_id).state, Info); },
                setPropertyStateSerialized(_id, value) { instances.imported(_id).state = deserialize(value, Info); },
                getPropertyImportedSerialized(_id) { return instances.import(instances.imported(_id).imported); },
                setPropertyExportedSerialized(_id, value) { instances.imported(_id).exported = instances.export(value, id => new JSExported(id)); }
            };
            """);
    }

    [Fact]
    public void GeneratesForEventsInStaticInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Info (string Value);

            public interface IExported { event Action<Info> Evt; }
            public interface IImported { event Action<Info> Evt; }
            """));
        Execute();
        Contains(
            """
            export const Space = {
                Exported: {
                    evt: new Event(),
                    broadcastEvtSerialized: (obj) => Space.Exported.evt.broadcast(deserialize(obj, Space_Info))
                },
                Imported: {
                    evt: importEvent((obj) => exports.Bootsharp_Generated_Imports_Space_JSImported_InvokeEvt(serialize(obj, Space_Info)))
                }
            };
            """);
    }

    [Fact]
    public void GeneratesForEventsInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { event Action<IExported, Info>? Changed; }
            public interface IImported { event Action<IImported, Info>? Changed; }

            public partial class Class
            {
                [Export] public static IExported GetExported (IImported inst) => default;
                [Import] public static IImported GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            function register_IImported(instance) {
                return instances.import(instance, _id => {
                    instance.changed.subscribe(handleChanged);
                    return () => {
                        instance.changed.unsubscribe(handleChanged);
                    };

                    function handleChanged(arg1, arg2) { exports.Bootsharp_Generated_Imports_JSImported_InvokeChanged(_id, register_IImported(arg1), serialize(arg2, Info)); }
                });
            }
            """);
        Contains(
            """
            class JSExported {
                constructor(_id) { this._id = _id; }
                changed = new Event();
                broadcastChanged(arg1, arg2) { this.changed.broadcast(arg1, arg2); }
                static broadcastChanged(_id, arg1, arg2) { instances.export(_id, id => new JSExported(id)).broadcastChanged(arg1, arg2); }
            }

            export const Class = {
                getExported: (inst) => instances.export(exports.Class_GetExported(register_IImported(inst)), id => new JSExported(id)),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = (inst) => register_IImported(this.getImportedHandler(instances.export(inst, id => new JSExported(id)))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const Exported = {
                broadcastChangedSerialized(_id, arg1, arg2) { JSExported.broadcastChanged(_id, instances.export(arg1, id => new JSExported(id)), deserialize(arg2, Info)); }
            };
            """);
    }

    [Fact]
    public void DoesNotEmitDuplicateInterfaceRegistrations ()
    {
        AddAssembly(With(
            """
            public interface IImported
            {
                event Action? Changed;
                event Action<string>? Done;
            }

            public class Class
            {
                [Export] public static void UseImported (IImported instance) {}
            }
            """));
        Execute();
        Once("function register_IImported");
    }

    [Fact]
    public void IgnoresImplementedInterfaceMethods ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]
            [assembly:Import(typeof(IImportedStatic))]

            public interface IExportedStatic { int Foo () => 0; }
            public interface IImportedStatic { int Foo () => 0; }
            public interface IExportedInstanced { int Foo () => 0; }
            public interface IImportedInstanced { int Foo () => 0; }

            public class Class
            {
                [Export] public static IExportedInstanced GetExported () => default;
                [Import] public static IImportedInstanced GetImported () => default;
            }
            """));
        Execute();
        DoesNotContain("Foo");
    }
}
