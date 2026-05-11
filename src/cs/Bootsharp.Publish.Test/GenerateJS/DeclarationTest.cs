namespace Bootsharp.Publish.Test;

public class DeclarationTest : GenerateJSTest
{
    protected override string TestedContent { get => field ?? ReadProjectFile("generated/index.g.d.mts") ?? ""; set; }

    [Fact]
    public void DeclaresNamespace ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Bar () { }"));
        Execute();
        Contains("foo.g.d.mts",
            """
            export namespace Class {
                export function bar(): void;
            }
            """);
    }

    [Fact]
    public void DotsInSpaceArePreserved ()
    {
        AddAssembly(WithClass("Foo.Bar.Nya", "[Export] public static void Bar () { }"));
        Execute();
        Contains("foo/bar/nya.g.d.mts",
            """
            export namespace Class {
                export function bar(): void;
            }
            """);
    }

    [Fact]
    public void WhenNoNamespaceDeclaresUnderRoot ()
    {
        AddAssembly(
            With("public record Record;"),
            With("public enum Enum { A, B }"),
            WithClass("[Export] public static Enum Inv (Record r) => default;"));
        Execute();
        Contains(
            """
            export namespace Class {
                export function inv(r: Record): Enum;
            }
            export enum Enum {
                A,
                B
            }
            export type Record = Readonly<{
            }>;
            """);
    }

    [Fact]
    public void NestedTypesAreDeclaredUnderClassSpace ()
    {
        AddAssembly(
            With("public class Foo { public record Bar; }"),
            WithClass("[Export] public static void Inv (Foo.Bar r) {}"));
        Execute();
        Contains(
            """
            export namespace Class {
                export function inv(r: Foo.Bar): void;
            }
            export namespace Foo {
                export type Bar = Readonly<{
                }>;
            }
            """);
    }

    [Fact]
    public void CrawledTypeDoesNotOverrideSpecializedDeclaration ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(ModFoo))]

            public class StatFoo
            {
                [Export] public static int StatBar () => 0;
                public record StatItem (int X);
            }
            public class InstFoo
            {
                public int Value { get; set; }
                public record InstItem (int X);
            }
            public record SerFoo (int X)
            {
                public record SerItem (int Y);
            }
            public interface ModFoo
            {
                int ModBar ();
                public record ModItem (int X);
            }
            public class Class
            {
                [Export] public static int UseStat (StatFoo.StatItem i) => 0;
                [Export] public static InstFoo GetInst () => default;
                [Export] public static int UseInst (InstFoo.InstItem i) => 0;
                [Export] public static SerFoo GetSer () => default;
                [Export] public static int UseSer (SerFoo.SerItem i) => 0;
                [Export] public static int UseMod (ModFoo.ModItem i) => 0;
            }
            """));
        Execute();
        Contains("export function statBar(): number;");
        Contains("export interface InstFoo");
        Contains("export type SerFoo");
        Contains("export function modBar(): number;");
    }

    [Fact]
    public void FunctionDeclarationIsExportedForInvokableMethod ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Foo () { }"));
        Execute();
        Contains("foo.g.d.mts",
            """
            export namespace Class {
                export function foo(): void;
            }
            """);
    }

    [Fact]
    public void AssignableVariableIsExportedForFunctionCallback ()
    {
        AddAssembly(WithClass("Foo", "[Import] public static void OnFoo () { }"));
        Execute();
        Contains("foo.g.d.mts",
            """
            export namespace Class {
                export let onFoo: () => void;
            }
            """);
    }

    [Fact]
    public void EventPropertiesAreExportedForStaticEvents ()
    {
        AddAssembly(
            WithClass("Foo", "[Export] public static event Action? ExpEvt;"),
            WithClass("Foo", "[Export] public static event Action<string>? Evt;"),
            WithClass("Foo", "[Import] public static event Action<int, bool?>? ImpEvt;"));
        Execute();
        Contains("foo.g.d.mts",
            """
            export namespace Class {
                export const expEvt: Event<[]>;
                export const evt: Event<[obj: string]>;
                export const impEvt: Event<[arg1: number, arg2: boolean | undefined]>;
            }
            """);
    }

    [Fact]
    public void VariablesAreExportedForStaticProperties ()
    {
        AddAssembly(
            WithClass("Foo", "[Export] public static int ExpProp { get; set; }"),
            WithClass("Foo", "[Export] public static string ReadOnlyProp { get; }"),
            WithClass("Foo", "[Import] public static bool? ImpProp { get => default; set { } }"));
        Execute();
        Contains("foo.g.d.mts",
            """
            export namespace Class {
                export let expProp: number;
                export const readOnlyProp: string;
                export let impProp: { get: () => boolean | undefined; set: (value: boolean | undefined) => void };
            }
            """);
    }

    [Fact]
    public void MembersFromSameSpaceAreDeclaredUnderSameSpace ()
    {
        AddAssembly(
            With("Space", "public class Foo { }"),
            With("Space", "public class Bar { }"),
            WithClass("Space", "[Export] public static Foo GetFoo (Bar bar) => default;"));
        Execute();
        Contains("space.g.d.mts",
            """
            export namespace Class {
                export function getFoo(bar: Bar): Foo;
            }
            export interface Bar {
            }
            export interface Foo {
            }
            """);
    }

    [Fact]
    public void MembersFromDifferentSpacesAreDeclaredUnderRespectiveSpaces ()
    {
        AddAssembly(
            With("SpaceA", "public class Foo { }"),
            With("SpaceB", "public class Bar { }"),
            WithClass("[Export] public static SpaceA.Foo GetFoo (SpaceB.Bar bar) => default;"));
        Execute();
        Contains(
            """
            export namespace Class {
                export function getFoo(bar: space_b.Bar): space_a.Foo;
            }
            """);
    }

    [Fact]
    public void DifferentSpacesWithSameRootAreDeclaredIndividually ()
    {
        AddAssembly(
            WithClass("Nya.Bar", "[Export] public static void Fun () { }"),
            WithClass("Nya.Foo", "[Export] public static void Foo () { }"));
        Execute();
        Contains("nya/bar.g.d.mts",
            """
            export namespace Class {
                export function fun(): void;
            }
            """);
        Contains("nya/foo.g.d.mts",
            """
            export namespace Class {
                export function foo(): void;
            }
            """);
    }

    [Fact]
    public void WhenNoNamespaceTypesAreDeclaredUnderRoot ()
    {
        AddAssembly(
            With("public class Foo { }"),
            WithClass("[Import] public static void OnFoo (Foo foo) { }"));
        Execute();
        Contains(
            """
            export namespace Class {
                export let onFoo: (foo: Foo) => void;
            }
            export interface Foo {
            }
            """);
    }

    [Fact]
    public void NumericsTranslatedToNumber ()
    {
        var types = new[] { "byte", "sbyte", "ushort", "uint", "ulong", "short", "int", "decimal", "double", "float" };
        var csArgs = string.Join(", ", types.Select(n => $"{n} v{Array.IndexOf(types, n)}"));
        var tsArgs = string.Join(", ", types.Select(n => $"v{Array.IndexOf(types, n)}: number"));
        AddAssembly(WithClass($"[Export] public static void Num ({csArgs}) {{ }}"));
        Execute();
        Contains($"num({tsArgs})");
    }

    [Fact]
    public void Int64TranslatedToBigInt ()
    {
        AddAssembly(WithClass("[Export] public static void Foo (long bar) {}"));
        Execute();
        Contains("foo(bar: bigint): void");
    }

    [Fact]
    public void TaskTranslatedToPromise ()
    {
        AddAssembly(
            WithClass("[Export] public static Task<bool> AsyBool () => default;"),
            WithClass("[Export] public static Task AsyVoid () => default;"));
        Execute();
        Contains("asyBool(): Promise<boolean>");
        Contains("asyVoid(): Promise<void>");
    }

    [Fact]
    public void CharAndStringTranslatedToString ()
    {
        AddAssembly(WithClass("[Export] public static void Cha (char c, string s) {}"));
        Execute();
        Contains("cha(c: string, s: string): void");
    }

    [Fact]
    public void BoolTranslatedToBoolean ()
    {
        AddAssembly(WithClass("[Export] public static void Boo (bool b) {}"));
        Execute();
        Contains("boo(b: boolean): void");
    }

    [Fact]
    public void DateTimeTranslatedToDate ()
    {
        AddAssembly(WithClass("[Export] public static void Doo (DateTime time) {}"));
        Execute();
        Contains("doo(time: Date): void");
    }

    [Fact]
    public void ListAndArrayTranslatedToArray ()
    {
        AddAssembly(WithClass("[Export] public static List<string> Goo (DateTime[] d) => default;"));
        Execute();
        Contains("goo(d: Array<Date>): Array<string>");
    }

    [Fact]
    public void JaggedArrayAndListOfListsTranslatedToArrayOfArrays ()
    {
        AddAssembly(WithClass("[Export] public static List<List<string>> Goo (DateTime[][] d) => default;"));
        Execute();
        Contains("goo(d: Array<Array<Date>>): Array<Array<string>>");
    }

    [Fact]
    public void IntArraysTranslatedToRelatedTypes ()
    {
        AddAssembly(
            WithClass("[Export] public static void Uint8 (byte[] foo) {}"),
            WithClass("[Export] public static void Int8 (sbyte[] foo) {}"),
            WithClass("[Export] public static void Uint16 (ushort[] foo) {}"),
            WithClass("[Export] public static void Int16 (short[] foo) {}"),
            WithClass("[Export] public static void Uint32 (uint[] foo) {}"),
            WithClass("[Export] public static void Int32 (int[] foo) {}"),
            WithClass("[Export] public static void BigInt64 (long[] foo) {}"),
            WithClass("[Export] public static void Float32 (float[] foo) {}"),
            WithClass("[Export] public static void Float64 (double[] foo) {}"));
        Execute();
        TestedContent = ReadProjectFile("generated/index.g.d.mts");
        Contains("uint8(foo: Uint8Array): void");
        Contains("int8(foo: Int8Array): void");
        Contains("uint16(foo: Uint16Array): void");
        Contains("int16(foo: Int16Array): void");
        Contains("uint32(foo: Uint32Array): void");
        Contains("int32(foo: Int32Array): void");
        Contains("bigInt64(foo: BigInt64Array): void");
        Contains("float32(foo: Float32Array): void");
        Contains("float64(foo: Float64Array): void");
    }

    [Fact]
    public void OtherTypesAreTranslatedToAny ()
    {
        AddAssembly(WithClass("[Export] public static DBNull Method (IEnumerable<string> t) => default;"));
        Execute();
        Contains("method(t: any): any");
    }

    [Fact]
    public void DefinitionIsGeneratedForObjectType ()
    {
        AddAssembly(
            With("n", "public class Foo { public string S { get; set; } public int I { get; set; } }"),
            WithClass("n", "[Export] public static Foo Method (Foo t) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(t: Foo): Foo;
            }
            export interface Foo {
                s: string;
                i: number;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForInterfaceAndImplementation ()
    {
        AddAssembly(
            With("n", "public interface Interface { Interface Foo { get; } void Bar (Interface b); }"),
            With("n", "public class Base { }"),
            With("n", "public class Derived : Base, Interface { public Interface Foo { get; } public void Bar (Interface b) {} }"),
            WithClass("n", "[Export] public static Derived Method (Base b) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(b: Base): Derived;
            }
            export interface Base {
            }
            export interface Derived extends Base, Interface {
                readonly foo: Interface;
                bar(b: Interface): void;
            }
            export interface Interface {
                readonly foo: Interface;
                bar(b: Interface): void;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithListProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public List<Item> Items { get; } }"),
            WithClass("n", "[Export] public static Container Combine (List<Item> items) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function combine(items: Array<Item>): Container;
            }
            export interface Item {
            }
            export interface Container {
                readonly items: Array<Item>;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithJaggedArrayProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public Item[][] Items { get; } }"),
            WithClass("n", "[Export] public static Container Get () => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function get(): Container;
            }
            export interface Container {
                readonly items: Array<Array<Item>>;
            }
            export interface Item {
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyListProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyList<Item> Items { get; } }"),
            WithClass("n", "[Export] public static Container Combine (IReadOnlyList<Item> items) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function combine(items: Array<Item>): Container;
            }
            export interface Item {
            }
            export interface Container {
                readonly items: Array<Item>;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithDictionaryProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public Dictionary<string, Item> Items { get; } }"),
            WithClass("n", "[Export] public static Container Combine (Dictionary<string, Item> items) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function combine(items: Map<string, Item>): Container;
            }
            export interface Item {
            }
            export interface Container {
                readonly items: Map<string, Item>;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyDictionaryProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyDictionary<string, Item> Items { get; } }"),
            WithClass("n", "[Export] public static Container Combine (IReadOnlyDictionary<string, Item> items) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function combine(items: Map<string, Item>): Container;
            }
            export interface Item {
            }
            export interface Container {
                readonly items: Map<string, Item>;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithCollectionProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public ICollection<Item> Items { get; } }"),
            WithClass("n", "[Export] public static Container Combine (ICollection<Item> items) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function combine(items: Array<Item>): Container;
            }
            export interface Item {
            }
            export interface Container {
                readonly items: Array<Item>;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyCollectionProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyCollection<Item> Items { get; } }"),
            WithClass("n", "[Export] public static Container Combine (IReadOnlyCollection<Item> items) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function combine(items: Array<Item>): Container;
            }
            export interface Item {
            }
            export interface Container {
                readonly items: Array<Item>;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericClass ()
    {
        AddAssembly(
            With("n", "public class Generic<T> where T: notnull { public required T Value { get; set; } }"),
            With("n", "public class GenericNull<T> { public T? Value { get; } public T? Foo (T? t) => default; }"),
            WithClass("n", "[Export] public static void Method (Generic<string> a, GenericNull<int> b) { }"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(a: Generic<string>, b: GenericNull<number>): void;
            }
            export interface Generic<T> {
                value: T;
            }
            export interface GenericNull<T> {
                readonly value?: T;
                foo(t: T | undefined): T | null;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericRecord ()
    {
        AddAssembly(
            With("n", "public record Generic<T> where T: notnull { public T Value { get; set; } }"),
            With("n", "public record GenericNull<T> { public T? Value { get; set; } }"),
            WithClass("n", "[Export] public static void Method (Generic<string> a, GenericNull<int> b) { }"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(a: Generic<string>, b: GenericNull<number>): void;
            }
            export type Generic<T> = Readonly<{
                value: T;
            }>;
            export type GenericNull<T> = Readonly<{
                value?: T;
            }>;
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericInterface ()
    {
        AddAssembly(
            With("n", "public interface IGenericInterface<T> { public T Value { get; set; } }"),
            WithClass("n", "[Export] public static IGenericInterface<string> Method () => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(): IGenericInterface<string>;
            }
            export interface IGenericInterface<T> {
                value?: T;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForNestedGenericTypes ()
    {
        AddAssembly(
            With("Foo", "public class GenericClass<T> { public T Value { get; set; } }"),
            With("Bar", "public interface GenericInterface<T> { public T Value { get; set; } }"),
            WithClass("n", "[Export] public static void Method (Foo.GenericClass<Bar.GenericInterface<string>> p) { }"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(p: foo.GenericClass<bar.GenericInterface<string>>): void;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericClassWithMultipleTypeArguments ()
    {
        AddAssembly(
            With("n", "public class GenericClass<T1, T2> { public T1 Key { get; set; } public T2 Value { get; set; } }"),
            WithClass("n", "[Export] public static void Method (GenericClass<string, int> p) { }"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function method(p: GenericClass<string, number>): void;
            }
            export interface GenericClass<T1, T2> {
                key?: T1;
                value?: T2;
            }
            """);
    }

    [Fact]
    public void CanCrawlCustomTypes ()
    {
        AddAssembly(
            With("Space",
                """
                public class Nya { public bool Mew() => default; }
                public struct Struct { public double A { get; set; } public Nya Mew { get; } }
                public readonly struct ReadonlyStruct { public double A { get; init; } }
                public readonly record struct ReadonlyRecordStruct(double A);
                public record class RecordClass (ReadonlyRecordStruct Str);
                public record class RecordClassA (double A) : RecordClass(new ReadonlyRecordStruct(42));
                public record class RecordClassB (RecordClassA B) : RecordClassA(24);
                public enum Enum { A, B }
                public class Foo { public Struct S { get; } public ReadonlyStruct Rs { get; } }
                public class Bar : Foo { public Dictionary<string, RecordClassB> Rc { get; } }
                public class Baz : Bar { public List<Bar> Bars { get; } public Enum E { get; } }
                public class Key : Baz { }
                public class Class { [Export] public static Dictionary<Key, Baz> GetBaz () => default; }
                """));
        Execute();
        // 'Foo' and 'RecordClass' are not declared, because they don't directly appear on the interop boundary;
        // instead, their members are merged into 'Bar' and 'RecordClassA', who directly inherit (extend) them.
        Contains("space.g.d.mts",
            """
            export namespace Class {
                export function getBaz(): Map<Key, Baz>;
            }
            export interface Key extends Baz {
            }
            export interface Bar {
                readonly rc: Map<string, RecordClassB>;
                readonly s: Struct;
                readonly rs: ReadonlyStruct;
            }
            export interface Nya {
                mew(): boolean;
            }
            export interface Baz extends Bar {
                readonly bars: Array<Bar>;
                readonly e: Enum;
            }
            export enum Enum {
                A,
                B
            }
            export type ReadonlyRecordStruct = Readonly<{
                a: number;
            }>;
            export type ReadonlyStruct = Readonly<{
                a: number;
            }>;
            export type RecordClassA = Readonly<{
                a: number;
                str: ReadonlyRecordStruct;
            }>;
            export type RecordClassB = RecordClassA & Readonly<{
                b: RecordClassA;
            }>;
            export type Struct = Readonly<{
                a: number;
                mew: Nya;
            }>;
            """);
    }

    [Fact]
    public void StaticPropertiesAreIncluded ()
    {
        AddAssembly(
            WithClass("public class Foo { public static string Soo { get; } }"),
            WithClass("[Export] public static Foo Bar () => default;"));
        Execute();
        Contains(
            """
            export namespace Class {
                export function bar(): Class.Foo;
                export interface Foo {
                    readonly soo: string;
                }
            }
            """);
    }

    [Fact]
    public void IndexerPropertiesAreNotIncluded ()
    {
        AddAssembly(WithClass(
            """
            public record Foo
            {
                public bool this[int index] => true;
            }

            [Export] public static Foo Bar () => default;
            """));
        Execute();
        Contains(
            """
            export namespace Class {
                export function bar(): Class.Foo;
                export type Foo = Readonly<{
                }>;
            }
            """);
    }

    [Fact]
    public void SetOnlyPropertiesAreNotIncluded ()
    {
        AddAssembly(WithClass(
            """
            public record Foo
            {
                public bool SetOnly { set { } }
            }

            [Export] public static Foo Bar () => default;
            """));
        Execute();
        Contains(
            """
            export namespace Class {
                export function bar(): Class.Foo;
                export type Foo = Readonly<{
                }>;
            }
            """);
    }

    [Fact]
    public void ComputedPropertiesAreNotIncluded ()
    {
        AddAssembly(WithClass(
            """
            public record Foo
            {
                public bool Boo => true;
            }

            [Export] public static Foo Bar () => default;
            """));
        Execute();
        Contains(
            """
            export namespace Class {
                export function bar(): Class.Foo;
                export type Foo = Readonly<{
                }>;
            }
            """);
    }

    [Fact]
    public void GeneratesForMethodsInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExported))]
            [assembly:Import(typeof(IImported))]

            public record Info (string Value);

            public interface IExported { Info Inv (string str, Info info); }
            public interface IImported { Info Fun (string str, Info info); }
            """));
        Execute();
        Contains(
            """
            export namespace IExported {
                export function inv(str: string, info: Info): Info;
            }
            export namespace IImported {
                export let fun: (str: string, info: Info) => Info;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            """);
    }

    [Fact]
    public void GeneratesForMethodsInInstanced ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { Info Inv (string str, Info info); void Reset (); }
            public interface IImported { Info Fun (Info info, string str); }

            public class Class
            {
                [Export] public static Task<IExported> GetExported (IImported it) => default;
                [Import] public static Task<IImported> GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains(
            """
            export namespace Class {
                export function getExported(it: IImported): Promise<IExported>;
                export let getImported: (it: IExported) => Promise<IImported>;
            }
            export interface IImported {
                fun(info: Info, str: string): Info;
            }
            export interface IExported {
                inv(str: string, info: Info): Info;
                reset(): void;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            """);
    }

    [Fact]
    public void GeneratesForPropertiesInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]
            [assembly:Import(typeof(IImportedStatic))]

            public record Info (string Value);

            public interface IExportedStatic
            {
                Info State { get; set; }
                Info? Optional { get; }
                IExportedInstanced Exported { get; }
                IImportedInstanced Imported { set; }
            }

            public interface IImportedStatic
            {
                Info State { get; }
                IImportedInstanced Imported { get; }
                IExportedInstanced Exported { set; }
            }

            public interface IExportedInstanced {}
            public interface IImportedInstanced {}
            """));
        Execute();
        Contains(
            """
            export namespace IExportedStatic {
                export let state: Info;
                export const optional: Info | undefined;
                export const exported: IExportedInstanced;
                export let imported: IImportedInstanced;
            }
            export namespace IImportedStatic {
                export let state: { get: () => Info };
                export let imported: { get: () => IImportedInstanced };
                export let exported: { set: (value: IExportedInstanced) => void };
            }
            export interface IExportedInstanced {
            }
            export interface IImportedInstanced {
            }
            export type Info = Readonly<{
                value: string;
            }>;
            """);
    }

    [Fact]
    public void GeneratesForPropertiesInInstanced ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported
            {
                Info State { get; set; }
                IExported Exported { get; }
                IImported Imported { set; }
            }

            public interface IImported
            {
                Info State { get; set; }
                IImported Imported { get; }
                IExported Exported { set; }
            }

            public class Class
            {
                [Export] public static IExported GetExported (IImported it) => default;
                [Import] public static IImported GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains(
            """
            export namespace Class {
                export function getExported(it: IImported): IExported;
                export let getImported: (it: IExported) => IImported;
            }
            export interface IImported {
                state: Info;
                readonly imported: IImported;
                exported: IExported;
            }
            export interface IExported {
                state: Info;
                readonly exported: IExported;
                imported: IImported;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            """);
    }

    [Fact]
    public void GeneratesForEventsInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExported))]
            [assembly:Import(typeof(IImported))]

            public record Info (string Value);

            public interface IExported { event Action<string, Info, IExportedInstanced> Evt; }
            public interface IImported { event Action<string, Info, IImportedInstanced> Evt; }

            public interface IExportedInstanced {}
            public interface IImportedInstanced {}
            """));
        Execute();
        Contains(
            """
            export namespace IExported {
                export const evt: Event<[arg1: string, arg2: Info, arg3: IExportedInstanced]>;
            }
            export namespace IImported {
                export const evt: Event<[arg1: string, arg2: Info, arg3: IImportedInstanced]>;
            }
            export interface IExportedInstanced {
            }
            export interface IImportedInstanced {
            }
            export type Info = Readonly<{
                value: string;
            }>;
            """);
    }

    [Fact]
    public void GeneratesForEventsInInstanced ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { event Action<Info>? Changed; event Action? Done; }
            public interface IImported { event Action<IImported, Info, string>? Changed; }

            public class Class
            {
                [Export] public static IExported GetExported (IImported it) => default;
                [Import] public static IImported GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains(
            """
            export namespace Class {
                export function getExported(it: IImported): IExported;
                export let getImported: (it: IExported) => IImported;
            }
            export interface IImported {
                changed: Event<[arg1: IImported, arg2: Info, arg3: string]>;
            }
            export interface IExported {
                changed: Event<[obj: Info]>;
                done: Event<[]>;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            """);
    }

    [Fact]
    public void NullableMethodArgumentsUnionWithUndefined ()
    {
        AddAssembly(
            WithClass("[Export] public static void Foo (string? bar) { }"),
            WithClass("[Import] public static void Fun (int? nya) { }"));
        Execute();
        Contains("export function foo(bar: string | undefined): void;");
        Contains("export let fun: (nya: number | undefined) => void;");
    }

    [Fact]
    public void NullableMethodReturnTypesUnionWithNull ()
    {
        AddAssembly(
            WithClass("[Export] public static string? Foo () => default;"),
            WithClass("[Export] public static Task? Bar () => default;"),
            WithClass("[Export] public static Task<byte[]?> Baz () => default;"),
            WithClass("[Export] public static Task<byte[]?>? Quz () => default;"),
            WithClass("[Import] public static ValueTask<List<string>?> Nya () => default;"));
        Execute();
        TestedContent = ReadProjectFile("generated/index.g.d.mts");
        Contains("export function foo(): string | null;");
        Contains("export function bar(): Promise<void> | null;");
        Contains("export function baz(): Promise<Uint8Array | null>;");
        Contains("export function quz(): Promise<Uint8Array | null> | null;");
        Contains("export let nya: () => Promise<Array<string> | null>;");
    }

    [Fact]
    public void NullableCollectionElementTypesUnionWithNull ()
    {
        AddAssembly(
            With("public class Foo { }"),
            WithClass("[Import] public static List<Foo?>? Fun (int?[]? bar, Foo[]?[]? nya, Foo?[]?[]? far) => default;"));
        Execute();
        Contains(
            """
            export namespace Class {
                export let fun: (bar: Array<number | null> | undefined, nya: Array<Array<Foo> | null> | undefined, far: Array<Array<Foo | null> | null> | undefined) => Array<Foo | null> | null;
            }
            export interface Foo {
            }
            """);
    }

    [Fact]
    public void NullableCollectionElementTypesOfCustomTypeUnionWithNull ()
    {
        AddAssembly(
            With("public interface IFoo<T> { }"),
            With("public record Foo (List<List<IFoo<string>?>?>? Bar, IFoo<int>?[]?[]? Nya) : IFoo<bool>;"),
            WithClass("[Import] public static IFoo<bool> Fun (Foo foo) => default;"));
        Execute();
        Contains("bar?: Array<Array<IFoo<string> | null> | null>;");
        Contains("nya?: Array<Array<IFoo<number> | null> | null>;");
    }

    [Fact]
    public void NullableDictionaryValueTypesUnionWithNull ()
    {
        AddAssembly(
            WithClass("[Import] public static Dictionary<string, int?>? Fun (Dictionary<string, string?>? bar) => default;"));
        Execute();
        Contains("export let fun: (bar: Map<string, string | null> | undefined) => Map<string, number | null> | null;");
    }

    [Fact]
    public void NullablePropertiesHaveOptionalModificator ()
    {
        AddAssembly(
            With("n", "public class Foo { public bool? Bool { get; } }"),
            With("n", "public class Bar { public Foo? Foo { get; } }"),
            WithClass("n", "[Export] public static Foo FooBar (Bar bar) => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function fooBar(bar: Bar): Foo;
            }
            export interface Bar {
                readonly foo?: Foo;
            }
            export interface Foo {
                readonly bool?: boolean;
            }
            """);
    }

    [Fact]
    public void NullableEnumsAreCrawled ()
    {
        AddAssembly(
            With("n", "public enum Foo { A, B }"),
            With("n", "public class Bar { public Foo? Foo { get; } }"),
            WithClass("n", "[Export] public static Bar GetBar () => default;"));
        Execute();
        Contains("n.g.d.mts",
            """
            export namespace Class {
                export function getBar(): Bar;
            }
            export interface Bar {
                readonly foo?: Foo;
            }
            export enum Foo {
                A,
                B
            }
            """);
    }

    [Fact]
    public void DeeplyNestedEnumIsDeclared ()
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
            export namespace A {
                export namespace B {
                    export enum C {
                        X,
                        Y
                    }
                }
            }
            """);
    }

    [Fact]
    public void WhenTypeReferencedMultipleTimesItsDeclaredOnlyOnce ()
    {
        AddAssembly(
            With("public interface Foo { }"),
            With("public class Bar : Foo { public Foo Foo { get; } }"),
            With("public class Far : Bar { public Bar Bar { get; } }"),
            WithClass("[Export] public static Bar TakeFooGiveBar (Foo f) => default;"),
            WithClass("[Export] public static Foo TakeBarGiveFoo (Bar b) => default;"),
            WithClass("[Export] public static Far TakeAllGiveFar (Foo f, Bar b, Far ff) => default;"));
        Execute();
        TestedContent = ReadProjectFile("generated/index.g.d.mts");
        Once("export interface Foo");
        Once("export interface Bar");
        Once("export interface Far");
    }

    [Fact]
    public void IgnoresBindingsInGeneratedNamespace ()
    {
        AddAssembly(With("Bootsharp.Generated",
            """
            public record Record;
            public static class Exports { [Export] public static void Inv (Record r) {} }
            public static class Imports { [Import] public static void Fun () {} }
            """));
        Execute();
        DoesNotContain("bootsharp/generated.g.d.mts", "Record");
        DoesNotContain("bootsharp/generated.g.d.mts", "export function inv");
        DoesNotContain("bootsharp/generated.g.d.mts", "export let fun");
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

    [Fact]
    public void DeclarationsCrossNamespaceImportsEmitted ()
    {
        AddAssembly(With(
            """
            namespace Metadata { public record Value (int X); }
            namespace Syntax {
                public class Class {
                    [Export] public static Metadata.Value Get () => default!;
                }
            }
            """));
        Execute();
        Contains("syntax.g.d.mts", "import type * as metadata from \"./metadata.g.mjs\";");
        Contains("syntax.g.d.mts", "metadata.Value");
    }

    [Fact]
    public void DeclarationFileImportsRootNamespaceTypeFromPackageRoot ()
    {
        AddAssembly(With(
            """
            public record RootRecord (string Value);
            namespace Space
            {
                public class Class { [Export] public static RootRecord Get () => default!; }
            }
            """));
        Execute();
        Contains("space.g.d.mts", "import type * as index from \"./index.g.mjs\";");
        Contains("space.g.d.mts", "index.RootRecord");
    }

    [Fact]
    public void TypeDeclarationGroupsMultipleNestedTypes ()
    {
        AddAssembly(With(
            """
            public class Outer { public record A (int X); public record B (int Y); }
            public class Other { public record C (int Z); }
            public class Class
            {
                [Export] public static Outer.A GetA () => default!;
                [Export] public static Outer.B GetB () => default!;
                [Export] public static Other.C GetC () => default!;
            }
            """));
        Execute();
        Contains("export namespace Other {");
        Contains("export namespace Outer {");
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
            export namespace Foo {
                export const qux: Event<[]>;
                export let baz: Enum;
                export function bar(): Enum;
            }
            export enum Enum {
                A,
                B
            }
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
            export namespace Foo {
                export const quz: Event<[]>;
                export let qux: Enum;
                export function bar(e: Enum): void;
                export let baz: (e: Enum) => void;
            }
            export enum Enum {
                A,
                B
            }
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
            export namespace Class {
                export function get(): Foo;
            }
            export interface Foo {
                qux: Event<[]>;
                baz: Enum;
                bar(e: Enum): void;
            }
            export enum Enum {
                A,
                B
            }
            """);
    }

    [Fact]
    public void GeneratesJsDocsOverCsDocs ()
    {
        AddAssembly(With(
            """
            /// <summary>
            /// Payload kind.
            /// </summary>
            public enum Kind
            {
                /// <summary>First kind.</summary>
                First,
                /// <summary>Second kind.</summary>
                Second
            }

            /// <summary>
            /// A payload sent across interop.
            /// </summary>
            /// <remarks>Visible in generated TypeScript.</remarks>
            public record Payload<T>
            {
                /// <summary>The payload name.</summary>
                public string Name { get; init; }
            }

            /// <summary>
            /// Event handler payload.
            /// </summary>
            public class HandlerArgs : EventArgs;

            /// <summary>Payload changed callback.</summary>
            /// <param name="payload">Payload from custom delegate.</param>
            /// <param name="label">Label from custom delegate.</param>
            public delegate void PayloadChanged (Payload<int> payload, string label);

            /// <summary>
            /// Exported instance API.
            /// </summary>
            public interface IExportedInstanced
            {
                /// <summary>Current state.</summary>
                int State { get; }

                /// <summary>Invokes instance.</summary>
                /// <param name="value">Value to pass.</param>
                void Inv (string value);
            }

            /// <summary>
            /// Static interop API.
            /// </summary>
            public partial class Class
            {
                /// <summary>Exports completion signal.</summary>
                [Export] public static event Action<bool>? ExpEvt;
                /// <summary>Imports completion signal.</summary>
                [Import] public static event Action<string, int>? ImpEvt;
                /// <summary>Exports payload changes.</summary>
                [Export] public static event PayloadChanged? PayloadChanged;
                /// <summary>Imports handler signal.</summary>
                /// <param name="sender">Sender from event handler.</param>
                /// <param name="e">Payload from event handler.</param>
                [Import] public static event EventHandler<HandlerArgs>? HandlerEvt;

                /// <summary>Runs foo.</summary>
                /// <param name="function">Function value.</param>
                /// <param name="names">Names to run.</param>
                /// <returns>Computed value.</returns>
                [Export] public static int Foo (List<int?> function, string[] names) => 0;

                /// <summary>Gets payload.</summary>
                [Export] public static Payload<int> Get (Kind kind) => default;

                /// <summary>Gets exported instance.</summary>
                [Export] public static IExportedInstanced GetExported () => default;

                /// <summary>Receives foo.</summary>
                /// <param name="count">Count to receive.</param>
                [Import] public static void OnFoo (Payload<int> count) { }

                /// <param name="value">Value without summary.</param>
                [Import] public static void OnParamOnly (string value) { }
            }
            """));
        Execute();
        Contains(
            """
            /**
             * Payload kind.
             */
            export enum Kind {
                /**
                 * First kind.
                 */
                First,
                /**
                 * Second kind.
                 */
                Second
            }
            """);
        Contains(
            """
            /**
             * A payload sent across interop.
             */
            export type Payload<T> = Readonly<{
                /**
                 * The payload name.
                 */
                name: string;
            }>;
            """);
        Contains(
            """
            /**
             * Exported instance API.
             */
            export interface IExportedInstanced {
                /**
                 * Current state.
                 */
                readonly state: number;
                /**
                 * Invokes instance.
                 * @param value Value to pass.
                 */
                inv(value: string): void;
            }
            """);
        Contains(
            """
            /**
             * Static interop API.
             */
            export namespace Class {
                /**
                 * Exports completion signal.
                 */
                export const expEvt: Event<[obj: boolean]>;
                /**
                 * Imports completion signal.
                 */
                export const impEvt: Event<[arg1: string, arg2: number]>;
                /**
                 * Exports payload changes.
                 * @param payload Payload from custom delegate.
                 * @param label Label from custom delegate.
                 */
                export const payloadChanged: Event<[payload: Payload<number>, label: string]>;
                /**
                 * Imports handler signal.
                 * @param sender Sender from event handler.
                 * @param e Payload from event handler.
                 */
                export const handlerEvt: Event<[sender: any | undefined, e: HandlerArgs]>;
                /**
                 * Runs foo.
                 * @param fn Function value.
                 * @param names Names to run.
                 * @returns Computed value.
                 */
                export function foo(fn: Array<number | null>, names: Array<string>): number;
                /**
                 * Gets payload.
                 */
                export function get(kind: Kind): Payload<number>;
                /**
                 * Gets exported instance.
                 */
                export function getExported(): IExportedInstanced;
                /**
                 * Receives foo.
                 * @param count Count to receive.
                 */
                export let onFoo: (count: Payload<number>) => void;
                /**
                 * @param value Value without summary.
                 */
                export let onParamOnly: (value: string) => void;
            }
            """);
    }
}
