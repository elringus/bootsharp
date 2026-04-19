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
            """);
    }

    [Fact]
    public void WhenDebugEnabledEmitsAndUsesExportImportHelpers ()
    {
        Task.Debug = true;
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]

            public interface IExportedStatic { int State { get; set; } }

            public class Class
            {
                [JSInvokable] public static Task<int> InvAsync () => Task.FromResult(0);
                [JSFunction] public static void Fun () {}
            }
            """));
        Execute();
        Contains("function getExport");
        Contains("function getImport");
        Contains("""getExport("Class_InvAsync")""");
        Contains("""getExport("Bootsharp_Generated_Exports_JSExportedStatic_GetPropertyState")""");
        Contains("""getImport(this.funHandler, this.funSerializedHandler, "Class.fun")""");
    }

    [Fact]
    public void WhenDebugDisabledDoesntEmitAndDoesntUseExportImportHelpers ()
    {
        Task.Debug = false;
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]

            public interface IExportedStatic { int State { get; set; } }

            public class Class
            {
                [JSInvokable] public static Task<int> InvAsync () => Task.FromResult(0);
                [JSFunction] public static void Fun () {}
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
        AddAssembly(WithClass("Foo.Bar", "[JSInvokable] public static void Nya () {}"));
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
        AddAssembly(WithClass("Foo.Bar", "[JSFunction] public static void Fun () {}"));
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
                    bar: () => exports.Foo_Class_Bar()
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
        AddAssembly(WithClass("Foo", "[JSInvokable] public static void Bar () {}"));
        AddAssembly(WithClass("Foo", "[JSFunction] public static void Fun () {}"));
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
            WithClass("Foo", "[JSInvokable] public static void Method () {}"),
            WithClass("FooBar.Baz", "[JSInvokable] public static void Method () {}")
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
            With("public class ClassA { [JSInvokable] public static void Inv () {} }"),
            With("public class ClassB { [JSFunction] public static void Fun () {} }"));
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
            WithClass("[JSInvokable] public static Task<int> Nya () => Task.FromResult(0);"),
            WithClass("[JSFunction] public static void Fun () {}"));
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
        AddAssembly(WithClass("[JSInvokable] public static void Fun (string function) {}"));
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
            WithClass("[JSInvokable] public static Info Foo (Info i) => default;"),
            WithClass("[JSFunction] public static Info? Bar (Info? i) => default;"),
            WithClass("[JSEvent] public static void Baz (Info?[]? i) {}"),
            WithClass("[JSEvent] public static void Yaz (int a, Info i) {}"));
        Execute();
        Contains(
            """
            export const Class = {
                foo: (i) => deserialize(exports.Class_Foo(serialize(i, Info)), Info),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = (i) => serialize(this.barHandler(deserialize(i, Info)), Info); },
                get barSerialized() { return this.barSerializedHandler; },
                baz: new Event(),
                bazSerialized: (i) => Class.baz.broadcast(deserialize(i, InfoArray) ?? undefined),
                yaz: new Event(),
                yazSerialized: (a, i) => Class.yaz.broadcast(a, deserialize(i, Info))
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
                [JSInvokable] public static Node Echo (Node node) => node;
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
                [JSInvokable] public static Node Echo (Node node) => node;
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
            WithClass("[JSInvokable] public static Task<Info> Foo (Info i) => default;"),
            WithClass("[JSFunction] public static Task<Info?> Bar (Info? i) => default;"),
            WithClass("[JSInvokable] public static Task<IReadOnlyList<Info>> Baz () => default;"),
            WithClass("[JSFunction] public static Task<IReadOnlyList<Info>> Yaz () => default;"));
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
            WithClass("n", "[JSInvokable] public static Foo GetFoo () => default;"));
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
            WithClass("n", "[JSInvokable] public static Task<Foo> GetFoo () => default;"));
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
            WithClass("n", "[JSInvokable] public static Foo GetFoo () => default;"));
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
                    inv: (s, e) => exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(s, serialize(e, Space_Enum))
                },
                Imported: {
                    get fun() { return this.funHandler; },
                    set fun(handler) { this.funHandler = handler; this.funSerializedHandler = (s, e) => this.funHandler(s, deserialize(e, Space_Enum)); },
                    get funSerialized() { return this.funSerializedHandler; },
                    onEvt: new Event(),
                    onEvtSerialized: (s, e) => Space.Imported.onEvt.broadcast(s, deserialize(e, Space_Enum))
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
                inv: (s, e) => exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(s, serialize(e, Space_Enum)),
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = (s, e) => this.funHandler(s, deserialize(e, Space_Enum)); },
                get funSerialized() { return this.funSerializedHandler; },
                onEvt: new Event(),
                onEvtSerialized: (s, e) => Foo.onEvt.broadcast(s, deserialize(e, Space_Enum)),
                Enum: { "0": "A", "1": "B", "A": 0, "B": 1 }
            };
            """);
    }

    [Fact]
    public void GeneratesPropertiesForInteropInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]
            [assembly:JSImport(typeof(IImportedStatic))]

            public record Info (string Value);

            public interface IExportedStatic
            {
                Info State { get; set; }
                IExportedInstanced Exported { get; }
                IImportedInstanced Imported { set; }
                int Count { set; }
            }

            public interface IImportedStatic
            {
                Info State { get; set; }
                IImportedInstanced Imported { get; }
                IExportedInstanced Exported { set; }
                int Count { set; }
            }

            public interface IExportedInstanced
            {
                Info State { get; set; }
                IExportedInstanced Exported { get; }
                IImportedInstanced Imported { set; }
            }

            public interface IImportedInstanced
            {
                Info State { get; set; }
                IImportedInstanced Imported { get; }
                IExportedInstanced Exported { set; }
            }

            public class Class
            {
                [JSInvokable] public static IExportedInstanced GetExported (IImportedInstanced inst) => default;
                [JSFunction] public static IImportedInstanced GetImported (IExportedInstanced inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            class JSExportedInstanced {
                constructor(_id) { this._id = _id; disposeOnFinalize(this, _id); }
                get state() { return ExportedInstanced.getPropertyState(this._id); }
                set state(value) { ExportedInstanced.setPropertyState(this._id, value); }
                get exported() { return ExportedInstanced.getPropertyExported(this._id); }
                set imported(value) { ExportedInstanced.setPropertyImported(this._id, value); }
            }

            export const Class = {
                getExported: (inst) => new JSExportedInstanced(exports.Class_GetExported(registerInstance(inst))),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = (inst) => registerInstance(this.getImportedHandler(new JSExportedInstanced(inst))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const ExportedInstanced = {
                getPropertyState(_id) { return deserialize(exports.Bootsharp_Generated_Exports_JSExportedInstanced_GetPropertyState(_id), Info); },
                setPropertyState(_id, value) { exports.Bootsharp_Generated_Exports_JSExportedInstanced_SetPropertyState(_id, serialize(value, Info)); },
                getPropertyExported(_id) { return new JSExportedInstanced(exports.Bootsharp_Generated_Exports_JSExportedInstanced_GetPropertyExported(_id)); },
                setPropertyImported(_id, value) { exports.Bootsharp_Generated_Exports_JSExportedInstanced_SetPropertyImported(_id, registerInstance(value)); }
            };
            export const ExportedStatic = {
                get state() { return deserialize(exports.Bootsharp_Generated_Exports_JSExportedStatic_GetPropertyState(), Info); },
                set state(value) { exports.Bootsharp_Generated_Exports_JSExportedStatic_SetPropertyState(serialize(value, Info)); },
                get exported() { return new JSExportedInstanced(exports.Bootsharp_Generated_Exports_JSExportedStatic_GetPropertyExported()); },
                set imported(value) { exports.Bootsharp_Generated_Exports_JSExportedStatic_SetPropertyImported(registerInstance(value)); },
                set count(value) { exports.Bootsharp_Generated_Exports_JSExportedStatic_SetPropertyCount(value); }
            };
            export const ImportedInstanced = {
                getPropertyStateSerialized(_id) { return serialize(getInstance(_id).state, Info); },
                setPropertyStateSerialized(_id, value) { getInstance(_id).state = deserialize(value, Info); },
                getPropertyImportedSerialized(_id) { return registerInstance(getInstance(_id).imported); },
                setPropertyExportedSerialized(_id, value) { getInstance(_id).exported = new JSExportedInstanced(value); }
            };
            export const ImportedStatic = {
                get state() { return this._state; },
                getPropertyStateSerialized() { return serialize(this.state, Info); },
                set state(value) { this._state = value; },
                setPropertyStateSerialized(value) { this.state = deserialize(value, Info); },
                get imported() { return this._imported; },
                getPropertyImportedSerialized() { return registerInstance(this.imported); },
                set exported(value) { this._exported = value; },
                setPropertyExportedSerialized(value) { this.exported = new JSExportedInstanced(value); },
                set count(value) { this._count = value; },
                setPropertyCountSerialized(value) { this.count = value; }
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
                getExported: async (inst) => new Space_JSExported(await exports.Class_GetExported(registerInstance(inst))),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = async (inst) => registerInstance(await this.getImportedHandler(new JSExported(inst))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const Exported = {
                inv: (_id, str) => deserialize(exports.Bootsharp_Generated_Exports_JSExported_Inv(_id, str), Enum)
            };
            export const Imported = {
                onEvtSerialized: (_id, str) => getInstance(_id).onEvt.broadcast(str)
            };
            export const Space = {
                Exported: {
                    inv: (_id, en) => exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(_id, serialize(en, Enum))
                },
                Imported: {
                    funSerialized: (_id, en) => serialize(getInstance(_id).fun(deserialize(en, Enum)), Enum)
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
}
