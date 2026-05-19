namespace Bootsharp.Publish.Test;

public class JSModuleTest : GenerateJSTest
{
    protected override string TestedContent { get => field ?? ReadProjectFile("generated/index.g.mjs") ?? ""; set; }

    [Fact]
    public void WhenDebugEnabledUsesExportImportHelpers ()
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
                [Export] public static void UseImported (IImportedInstanced it) {}
                [Import] public static void Fun () {}
            }
            """));
        Execute();
        Contains("""getExport("Class_InvokeEvt")""");
        Contains("""getExport("Class_InvAsync")""");
        Contains("""getExport("Bootsharp_Generated_Exports_JSExportedStatic_GetState")""");
        Contains("""getImport(this.funHandler, this.funSerializedHandler, "Class.fun")""");
    }

    [Fact]
    public void WhenDebugDisabledDoesNotUseExportImportHelpers ()
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
        DoesNotContain("getExport");
        DoesNotContain("getImport");
    }

    [Fact]
    public void BindingForStaticExportedMethodGenerated ()
    {
        AddAssembly(WithClass("Foo.Bar", "[Export] public static void Nya () {}"));
        Execute();
        Contains("foo/bar.g.mjs",
            """
            export const Class = {
                nya: () => exports.Foo_Bar_Class_Nya()
            };
            """);
    }

    [Fact]
    public void BindingForStaticImportedMethodGenerated ()
    {
        AddAssembly(WithClass("Foo.Bar", "[Import] public static void Fun () {}"));
        Execute();
        Contains("foo/bar.g.mjs",
            """
            export const Class = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void BindingForStaticEventGenerated ()
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
    public void BindingForStaticPropertyGenerated ()
    {
        AddAssembly(
            WithClass("[Export] public static int ExpProp { get; set; }"),
            WithClass("[Import] public static string ImpProp { get => default!; set { } }"));
        Execute();
        Contains(
            """
            export const Class = {
                get expProp() { return exports.Class_GetExpProp(); },
                set expProp(value) { exports.Class_SetExpProp(value); },
                getImpPropSerialized() { return this.impProp.get(); },
                setImpPropSerialized(value) { this.impProp.set(value); }
            };
            """);
    }

    [Fact]
    public void LibraryExportsNamespaceObject ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Bar () {}"));
        Execute();
        Contains("foo.g.mjs",
            """
            export const Class = {
                bar: () => exports.Foo_Class_Bar()
            };
            """);
    }

    [Fact]
    public void WhenSpaceContainDotsDirectoriesCreatedForEachPart ()
    {
        AddAssembly(WithClass("Foo.Bar.Nya", "[Export] public static void Bar () {}"));
        Execute();
        Contains("foo/bar/nya.g.mjs",
            """
            export const Class = {
                bar: () => exports.Foo_Bar_Nya_Class_Bar()
            };
            """);
    }

    [Fact]
    public void WhenMultipleSpacesEachGetItsOwnModule ()
    {
        AddAssembly(
            WithClass("Foo", "[Export] public static void Foo () {}"),
            WithClass("Bar.Nya", "[Import] public static void Fun () {}"));
        Execute();
        Contains("foo.g.mjs",
            """
            export const Class = {
                foo: () => exports.Foo_Class_Foo()
            };
            """);
        Contains("bar/nya.g.mjs",
            """
            export const Class = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void WhenMultipleAssembliesWithEqualSpaceObjectDeclaredOnlyOnce ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Bar () {}"));
        AddAssembly(WithClass("Foo", "[Import] public static void Fun () {}"));
        Execute();
        Once("foo.g.mjs", "export const Class");
    }

    [Fact]
    public void DifferentSpacesWithSameRootAssignedUnderSameDirectory ()
    {
        AddAssembly(
            WithClass("Nya.Foo", "[Export] public static void Foo () {}"),
            WithClass("Nya.Bar", "[Import] public static void Fun () {}"));
        Execute();
        Contains("nya/foo.g.mjs",
            """
            export const Class = {
                foo: () => exports.Nya_Foo_Class_Foo()
            };
            """);
        Contains("nya/bar.g.mjs",
            """
            export const Class = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void DifferentSpacesStartingEquallyAreNotAssignedToSameModule ()
    {
        AddAssembly(
            WithClass("Foo", "[Export] public static void Method () {}"),
            WithClass("FooBar.Baz", "[Export] public static void Method () {}")
        );
        Execute();
        Contains("foo.g.mjs",
            """
            export const Class = {
                method: () => exports.Foo_Class_Method()
            };
            """);
        Contains("foo-bar/baz.g.mjs",
            """
            export const Class = {
                method: () => exports.FooBar_Baz_Class_Method()
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleSpacesAssignedToRespectiveModules ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static int Foo () => 0;"));
        AddAssembly(WithClass("Bar.Nya", "[Import] public static void Fun () {}"));
        Execute();
        Contains("bar/nya.g.mjs",
            """
            export const Class = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = () => this.funHandler(); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
        Contains("foo.g.mjs",
            """
            export const Class = {
                foo: () => exports.Foo_Class_Foo()
            };
            """);
    }

    [Fact]
    public void BindingsFromMultipleClassesAssignedToRespectiveModules ()
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
    public void WhenNoSpaceBindingsAreAssignedToRootModule ()
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
                broadcastExpEvtSerialized: (arg1, arg2) => Class.expEvt.broadcast(deserialize(arg1, $s.InfoArray) ?? undefined, deserialize(arg2, $s.Info)),
                impEvt: importEvent((arg1, arg2) => exports.Class_InvokeImpEvt(arg1, serialize(arg2, $s.Info))),
                foo: (i) => deserialize(exports.Class_Foo(serialize(i, $s.Info)), $s.Info),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = (i) => serialize(this.barHandler(deserialize(i, $s.Info)), $s.Info); },
                get barSerialized() { return this.barSerializedHandler; }
            };
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
                foo: async (i) => deserialize(await exports.Class_Foo(serialize(i, $s.Info)), $s.Info),
                get bar() { return this.barHandler; },
                set bar(handler) { this.barHandler = handler; this.barSerializedHandler = async (i) => serialize(await this.barHandler(deserialize(i, $s.Info)), $s.Info); },
                get barSerialized() { return this.barSerializedHandler; },
                baz: async () => deserialize(await exports.Class_Baz(), $s.System_Collections_Generic_IReadOnlyList_Of_Info),
                get yaz() { return this.yazHandler; },
                set yaz(handler) { this.yazHandler = handler; this.yazSerializedHandler = async () => serialize(await this.yazHandler(), $s.System_Collections_Generic_IReadOnlyList_Of_Info); },
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
        Contains("n.g.mjs",
            """
            export const Class = {
                getFoo: () => deserialize(exports.n_Class_GetFoo(), $s.n_Class_Foo),
                Foo: {
                    "0": "A",
                    "1": "B",
                    "A": 0,
                    "B": 1
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
        TestedContent = ReadProjectFile("generated/n.g.mjs");
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
        Contains("n.g.mjs",
            """
            export const Class = {
                getFoo: () => deserialize(exports.n_Class_GetFoo(), $s.n_Foo)
            };
            export const Foo = {
                "1": "A",
                "6": "B",
                "A": 1,
                "B": 6
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
        DoesNotContain("bootsharp/generated.g.mjs", "inv: () =>");
        DoesNotContain("bootsharp/generated.g.mjs", "get fun()");
    }

    [Fact]
    public void GeneratesForMethodsInModules ()
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
        Contains("space.g.mjs",
            """
            export const IExported = {
                inv: (str, info) => deserialize(exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(str, serialize(info, $s.Space_Info)), $s.Space_Info)
            };
            export const IImported = {
                get fun() { return this.funHandler; },
                set fun(handler) { this.funHandler = handler; this.funSerializedHandler = (str, info) => serialize(this.funHandler(str, deserialize(info, $s.Space_Info)), $s.Space_Info); },
                get funSerialized() { return this.funSerializedHandler; }
            };
            """);
    }

    [Fact]
    public void GeneratesForPropertiesInModules ()
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
        Contains("space.g.mjs",
            """
            export const IExported = {
                get state() { return deserialize(exports.Bootsharp_Generated_Exports_Space_JSExported_GetState(), $s.Space_Info) ?? undefined; },
                set state(value) { exports.Bootsharp_Generated_Exports_Space_JSExported_SetState(serialize(value, $s.Space_Info)); },
                set count(value) { exports.Bootsharp_Generated_Exports_Space_JSExported_SetCount(value); }
            };
            export const IImported = {
                getStateSerialized() { return serialize(this.state.get(), $s.Space_Info); },
                setStateSerialized(value) { this.state.set(deserialize(value, $s.Space_Info)); },
                setCountSerialized(value) { this.count.set(value); }
            };
            """);
    }

    [Fact]
    public void GeneratesForEventsInModules ()
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
        Contains("space.g.mjs",
            """
            export const IExported = {
                evt: new Event(),
                broadcastEvtSerialized: (obj) => IExported.evt.broadcast(deserialize(obj, $s.Space_Info))
            };
            export const IImported = {
                evt: importEvent((obj) => exports.Bootsharp_Generated_Imports_Space_JSImported_InvokeEvt(serialize(obj, $s.Space_Info)))
            };
            """);
    }

    [Fact]
    public void ImportsAllModules ()
    {
        AddAssembly(
            WithClass("Foo.Bar", "[Export] public static void A () {}"),
            WithClass("Baz", "[Import] public static void B () {}"));
        Execute();
        Contains("imports.g.mjs", """import * as foo_bar from "./foo/bar.g.mjs";""");
        Contains("imports.g.mjs", """import * as baz from "./baz.g.mjs";""");
        Contains("imports.g.mjs", """runtime.setModuleImports("foo/bar", foo_bar);""");
        Contains("imports.g.mjs", """runtime.setModuleImports("baz", baz);""");
    }

    [Fact]
    public void NamespaceBindingFileExistsPerNamespace ()
    {
        AddAssembly(
            WithClass("Foo.Bar", "[Export] public static void A () {}"),
            WithClass("Baz", "[Export] public static void B () {}"),
            WithClass("[Export] public static void Root () {}"));
        Execute();
        Assert.NotNull(ReadProjectFile("generated/foo/bar.g.mjs"));
        Assert.NotNull(ReadProjectFile("generated/baz.g.mjs"));
        Assert.NotNull(ReadProjectFile("generated/index.g.mjs"));
    }

    [Fact]
    public void NestedGlobalEnumEmittedInIndexModule ()
    {
        AddAssembly(With(
            """
            public class Outer { public enum Inner { A, B } }
            public class Class { [Export] public static Outer.Inner Get () => default!; }
            """));
        Execute();
        Contains(
            """
            export const Class = {
                get: () => deserialize(exports.Class_Get(), $s.Outer_Inner)
            };
            export const Outer = {
                Inner: {
                    "0": "A",
                    "1": "B",
                    "A": 0,
                    "B": 1
                }
            };
            """);
    }

    [Fact]
    public void NestedEnumUnderNamespaceEmittedInSpaceModule ()
    {
        AddAssembly(With(
            """
            namespace n;
            public class Class { public enum Foo { A, B } }
            public class Holder { [Export] public static Class.Foo Get () => default!; }
            """));
        Execute();
        Contains("n.g.mjs",
            """
            export const Holder = {
                get: () => deserialize(exports.n_Holder_Get(), $s.n_Class_Foo)
            };
            export const Class = {
                Foo: {
                    "0": "A",
                    "1": "B",
                    "A": 0,
                    "B": 1
                }
            };
            """);
    }

    [Fact]
    public void DeeplyNestedEnumIsEmitted ()
    {
        AddAssembly(With(
            """
            public class A
            {
                public class B
                {
                    public enum C { X, Y }
                }
            }
            public class Class
            {
                [Export] public static A.B.C Get () => default;
            }
            """));
        Execute();
        Contains(
            """
            export const A = {
                B: {
                    C: {
                        "0": "X",
                        "1": "Y",
                        "X": 0,
                        "Y": 1
                    }
                }
            };
            """);
    }

    [Fact]
    public void NestedEnumsAreNotDeclaredTopLevel ()
    {
        AddAssembly(With(
            """
            public class Outer { public enum Nested { A, B } }
            public class Class { [Export] public static Outer.Nested Get () => default!; }
            """));
        Execute();
        Contains("Nested:");
        DoesNotContain("export const Nested");
    }

    [Fact]
    public void DoesNotEmitObjectsForUnrelatedTypes ()
    {
        AddAssembly(With(
            """
            public record Record;
            public class Outer { public record NestedRecord; }
            public class Class
            {
                public record InnerRecord;
                [Export] public static void Foo (Record record) {}
                [Export] public static void Bar (Outer.NestedRecord nested) {}
                [Export] public static void Baz (InnerRecord inner) {}
            }
            """));
        Execute();
        DoesNotContain("export const Record");
        DoesNotContain("export const NestedRecord");
        DoesNotContain("export const InnerRecord");
        DoesNotContain("export const Outer");
        DoesNotContain("Record:");
        DoesNotContain("NestedRecord:");
        DoesNotContain("InnerRecord:");
        DoesNotContain("Outer:");
    }

    [Fact]
    public void DoesNotEmitModulesForBclTypes ()
    {
        AddAssembly(
            With("public record Item;"),
            WithClass("[Export] public static IReadOnlyList<Item> GetItems () => default!;"),
            WithClass("[Export] public static byte[] GetBytes () => default!;"));
        Execute();
        DoesNotContain("imports.g.mjs", "system");
    }

    [Fact]
    public void RespectsPrefsInStatics ()
    {
        AddAssembly(With(
            """
            [assembly:Preferences(
                Space = [@".+", "index"],
                Name = [@"^Class$", "Foo"],
                Method = [@"^Method$", "bar"],
                Property = [@"^Property$", "baz"],
                Event = [@"^Event$", "qux"]
            )]

            namespace Space;

            public enum Enum { A, B }

            public class Class
            {
                [Export] public static Enum Method () => default;
                [Export] public static Enum Property { get; set; }
                [Export] public static event Action? Event;
            }
            """));
        Execute();
        Contains(
            """
            export const Foo = {
                qux: new Event(),
                broadcastEventSerialized: () => Foo.qux.broadcast(),
                get baz() { return deserialize(exports.Space_Class_GetProperty(), $s.Space_Enum); },
                set baz(value) { exports.Space_Class_SetProperty(serialize(value, $s.Space_Enum)); },
                bar: () => deserialize(exports.Space_Class_Method(), $s.Space_Enum)
            };
            export const Enum = {
                "0": "A",
                "1": "B",
                "A": 0,
                "B": 1
            };
            """);
    }

    [Fact]
    public void RespectsPrefsInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Preferences(
                Space = [@".+", "index"],
                Name = [@"^I.+$", "Foo"],
                Method = [@"^Inv$", "bar", @"^Fun$", "baz"],
                Property = [@"^State$", "qux"],
                Event = [@"^Changed$", "quz"]
            )]
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public enum Enum { A, B }

            public interface IExported
            {
                Enum State { get; set; }
                event Action? Changed;
                void Inv (Enum e);
            }
            public interface IImported
            {
                void Fun (Enum e);
            }
            """));
        Execute();
        Contains(
            """
            export const Foo = {
                quz: new Event(),
                broadcastChangedSerialized: () => Foo.quz.broadcast(),
                get qux() { return deserialize(exports.Bootsharp_Generated_Exports_Space_JSExported_GetState(), $s.Space_Enum); },
                set qux(value) { exports.Bootsharp_Generated_Exports_Space_JSExported_SetState(serialize(value, $s.Space_Enum)); },
                bar: (e) => exports.Bootsharp_Generated_Exports_Space_JSExported_Inv(serialize(e, $s.Space_Enum)),
                get baz() { return this.bazHandler; },
                set baz(handler) { this.bazHandler = handler; this.bazSerializedHandler = (e) => this.bazHandler(deserialize(e, $s.Space_Enum)); },
                get bazSerialized() { return this.bazSerializedHandler; }
            };
            export const Enum = {
                "0": "A",
                "1": "B",
                "A": 0,
                "B": 1
            };
            """);
    }

    [Fact]
    public void RespectsPrefsInInstanced ()
    {
        AddAssembly(With(
            """
            [assembly:Preferences(
                Space = [@".+", "index"],
                Name = [@"^IInst$", "Foo"],
                Method = [@"^Method$", "bar"],
                Property = [@"^Property$", "baz"],
                Event = [@"^Event$", "qux"]
            )]

            namespace Space;

            public enum Enum { A, B }

            public interface IInst
            {
                Enum Property { get; set; }
                event Action? Event;
                void Method (Enum e);
            }

            public class Class
            {
                [Export] public static IInst Get () => default;
            }
            """));
        Execute();
        Contains(
            """
            export const Class = {
                get: () => $i.resolve(exports.Space_Class_Get(), $i.Space_IInst)
            };
            export const Foo = {
                broadcastEventSerialized: (_id) => $i.resolve(_id, $i.Space_IInst).broadcastEvent(),
                getProperty(_id) { return deserialize(exports.Bootsharp_Generated_Exports_Space_JSInst_GetProperty(_id), $s.Space_Enum); },
                setProperty(_id, value) { exports.Bootsharp_Generated_Exports_Space_JSInst_SetProperty(_id, serialize(value, $s.Space_Enum)); },
                bar: (_id, e) => exports.Bootsharp_Generated_Exports_Space_JSInst_Method(_id, serialize(e, $s.Space_Enum))
            };
            export const Enum = {
                "0": "A",
                "1": "B",
                "A": 0,
                "B": 1
            };
            """);
    }
}
