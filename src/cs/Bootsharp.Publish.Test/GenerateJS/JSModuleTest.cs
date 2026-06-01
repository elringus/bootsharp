namespace Bootsharp.Publish.Test;

public class JSModuleTest : GenerateJSTest
{
    protected override string TestedContent { get => field ?? ReadProjectFile("generated/modules/index.g.mjs") ?? ""; set; }

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
        Contains("""getExport("JS_Export_IExportedStatic_GetState")""");
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
    public void EscapesReservedArgumentNames ()
    {
        AddAssembly(WithClass("[Export] public static void Foo (string Class, int Function) {}"));
        Execute();
        Contains(
            """
            export const Class = {
                foo: ($class, $function) => exports.Class_Foo($class, $function)
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
        TestedContent = ReadProjectFile("generated/modules/n.g.mjs");
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
                inv: (str, info) => deserialize(exports.JS_Export_Space_IExported_Inv(str, serialize(info, $s.Space_Info)), $s.Space_Info)
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
                get state() { return deserialize(exports.JS_Export_Space_IExported_GetState(), $s.Space_Info) ?? undefined; },
                set state(value) { exports.JS_Export_Space_IExported_SetState(serialize(value, $s.Space_Info)); },
                set count(value) { exports.JS_Export_Space_IExported_SetCount(value); }
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
                evt: importEvent((obj) => exports.JS_Import_Space_IImported_InvokeEvt(serialize(obj, $s.Space_Info)))
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
        Contains("../imports.g.mjs", """import * as foo_bar from "./modules/foo/bar.g.mjs";""");
        Contains("../imports.g.mjs", """import * as baz from "./modules/baz.g.mjs";""");
        Contains("../imports.g.mjs", """runtime.setModuleImports("foo/bar", foo_bar);""");
        Contains("../imports.g.mjs", """runtime.setModuleImports("baz", baz);""");
    }

    [Fact]
    public void NamespaceBindingFileExistsPerNamespace ()
    {
        AddAssembly(
            WithClass("Foo.Bar", "[Export] public static void A () {}"),
            WithClass("Baz", "[Export] public static void B () {}"),
            WithClass("[Export] public static void Root () {}"));
        Execute();
        Assert.NotNull(ReadProjectFile("generated/modules/foo/bar.g.mjs"));
        Assert.NotNull(ReadProjectFile("generated/modules/baz.g.mjs"));
        Assert.NotNull(ReadProjectFile("generated/modules/index.g.mjs"));
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
        DoesNotContain("../imports.g.mjs", "system");
    }

    [Fact]
    public void OverloadedMethodsAreDisambiguated ()
    {
        AddAssembly(With(
            """
            public class Class
            {
                [Export] public static void Foo (int a) {}
                [Export] public static void Foo (string a) {}
                [Export] public static void Foo (double b, double a) {}
                [Export] public static void Bar (int x) {}
                [Export] public static void Bar (int x, string name) {}
                [Export] public static void Baz (int x, int y) {}
                [Export] public static void Baz (int x, string y) {}
                [Export] public static void Qux (int a) {}
                [Export] public static void Qux (int a, int b) {}
                [Export] public static void Qux (int a, int b, int c) {}
                [Export] public static void X (int x, int y) {}
                [Export] public static void X (string x, int y) {}
                [Export] public static void X (int x, string y) {}
                [Export] public static void Bob (int x, int y, string z) {}
                [Export] public static void Bob (int x, string y, int q) {}
                [Export] public static void Change (double progress) {}
                [Export] public static void Change (string info) {}
                [Export] public static void Change (double progress, string info) {}
                [Export] public static void Start (string title) {}
                [Export] public static void Start (string title, string info) {}
                [Export] public static void Start (string title, double progress) {}
                [Export] public static void Start (string title, string info, double progress) {}
            }
            """));
        Execute();
        Contains(
            """
            export const Class = {
                foo: (a) => exports.Class_Foo(a),
                fooWithA: (a) => exports.Class_FooWithA(a),
                fooWithB: (b, a) => exports.Class_FooWithB(b, a),
                bar: (x) => exports.Class_Bar(x),
                barWithName: (x, name) => exports.Class_BarWithName(x, name),
                baz: (x, y) => exports.Class_Baz(x, y),
                bazWithXAndY: (x, y) => exports.Class_BazWithXAndY(x, y),
                qux: (a) => exports.Class_Qux(a),
                quxWithB: (a, b) => exports.Class_QuxWithB(a, b),
                quxWithBAndC: (a, b, c) => exports.Class_QuxWithBAndC(a, b, c),
                x: (x, y) => exports.Class_X(x, y),
                xWithStringAndInt32: (x, y) => exports.Class_XWithStringAndInt32(x, y),
                xWithInt32AndString: (x, y) => exports.Class_XWithInt32AndString(x, y),
                bob: (x, y, z) => exports.Class_Bob(x, y, z),
                bobWithQ: (x, y, q) => exports.Class_BobWithQ(x, y, q),
                change: (progress) => exports.Class_Change(progress),
                changeWithInfo: (info) => exports.Class_ChangeWithInfo(info),
                changeWithProgressAndInfo: (progress, info) => exports.Class_ChangeWithProgressAndInfo(progress, info),
                start: (title) => exports.Class_Start(title),
                startWithInfo: (title, info) => exports.Class_StartWithInfo(title, info),
                startWithProgress: (title, progress) => exports.Class_StartWithProgress(title, progress),
                startWithInfoAndProgress: (title, info, progress) => exports.Class_StartWithInfoAndProgress(title, info, progress)
            };
            """);
    }

    [Fact]
    public void GeneratesSupportedGenericMethods ()
    {
        AddAssembly(With(
            """
            public interface IShape {}
            public class Circle : IShape { public double Radius { get; set; } }
            public record Square : IShape { public double Side { get; set; } }

            public class Class
            {
                [Export] public static T Make<T> () where T : IShape => default!;
                [Export] public static void Take<T> (T shape) where T : IShape {}
            }
            """));
        Execute();
        Contains("makeOfCircle: () =>");
        Contains("makeOfSquare: () =>");
        Contains("takeOfCircle: (shape) =>");
        Contains("takeOfSquare: (shape) =>");
        Contains("exports.Class_MakeOfCircle()");
        Contains("exports.Class_MakeOfSquare()");
        Contains("exports.Class_TakeOfCircle(");
        Contains("exports.Class_TakeOfSquare(");
    }

    [Fact]
    public void DiscardsUnsupportedGenericMethods ()
    {
        AddAssembly(With(
            """
            public interface IShape {}
            public class Circle : IShape { public double Radius { get; set; } }
            public static class Box<T> { [Export] public static void Stored (T item) {} }
            public class Class
            {
                [Export] public static void Pair<T1, T2> () where T1 : IShape where T2 : IShape {}
                [Export] public static void Take<T> (T shape) where T : IShape {}
                [Export] public static void Many<T> (List<T> items) where T : IShape {}
                [Export] public static void Free<T> () {}
                [Export] public static void Real () {}
            }
            """));
        Execute();
        Contains("real:");
        Contains("takeOfCircle:");
        DoesNotContain("stored"); // method declared on a generic type
        DoesNotContain("pair"); // multiple type parameters
        DoesNotContain("many"); // type parameter used nested
        DoesNotContain("free"); // type parameter not constrained to a user type
    }

    [Fact]
    public void DiscardsGeneratedTypesFromGenericCandidates ()
    {
        AddAssembly(With(
            """
            public interface IShape {}
            public class Circle : IShape { public double Radius { get; set; } }
            namespace Bootsharp.Generated { public class JS_Import_Leaked : global::IShape {} }
            public class Class
            {
                [Export] public static T Make<T> () where T : IShape => default!;
            }
            """));
        Execute();
        Contains("makeOfCircle:");
        DoesNotContain("makeOfJS_Import_Leaked");
    }

    [Fact]
    public void RespectsPrefsInStatics ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public enum Enum { A, B }

            public class Class
            {
                [Export] public static Enum Method () => default;
                [Export] public static Enum Property { get; set; }
                [Export] public static event Action? Event;
            }

            public static class Prefs
            {
                [RenameModule]
                public static string Module (Type type, string @default) => "index";

                [RenameNode]
                public static string Node (Type type, string @default) => type.Name == "Class" ? "Foo" : @default;

                [RenameMember]
                public static string Member (MemberInfo info, string @default) => info.Name switch {
                    "Method" => "bar", "Property" => "baz", "Event" => "qux", _ => @default
                };
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

            public static class Prefs
            {
                [RenameModule]
                public static string Module (Type type, string @default) => "index";

                [RenameNode]
                public static string Node (Type type, string @default) => type.IsInterface ? "Foo" : @default;

                [RenameMember]
                public static string Member (MemberInfo info, string @default) => info.Name switch {
                    "Inv" => "bar", "Fun" => "baz", "State" => "qux", "Changed" => "quz", _ => @default
                };
            }
            """));
        Execute();
        Contains(
            """
            export const Foo = {
                quz: new Event(),
                broadcastChangedSerialized: () => Foo.quz.broadcast(),
                get qux() { return deserialize(exports.JS_Export_Space_IExported_GetState(), $s.Space_Enum); },
                set qux(value) { exports.JS_Export_Space_IExported_SetState(serialize(value, $s.Space_Enum)); },
                bar: (e) => exports.JS_Export_Space_IExported_Inv(serialize(e, $s.Space_Enum)),
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

            public static class Prefs
            {
                [RenameModule]
                public static string Module (Type type, string @default) => "index";

                [RenameNode]
                public static string Node (Type type, string @default) => type.Name == "IInst" ? "Foo" : @default;

                [RenameMember]
                public static string Member (MemberInfo info, string @default) => info.Name switch {
                    "Method" => "bar", "Property" => "baz", "Event" => "qux", _ => @default
                };
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
                getProperty(_id) { return deserialize(exports.JS_Export_Space_IInst_GetProperty(_id), $s.Space_Enum); },
                setProperty(_id, value) { exports.JS_Export_Space_IInst_SetProperty(_id, serialize(value, $s.Space_Enum)); },
                bar: (_id, e) => exports.JS_Export_Space_IInst_Method(_id, serialize(e, $s.Space_Enum))
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
