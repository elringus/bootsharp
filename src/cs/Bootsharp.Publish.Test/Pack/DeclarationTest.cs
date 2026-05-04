namespace Bootsharp.Publish.Test;

public class DeclarationTest : PackTest
{
    protected override string TestedContent => GeneratedDeclarations;

    [Fact]
    public void ImportsEventTypes ()
    {
        Execute();
        Contains("""import type { EventBroadcaster, EventSubscriber } from "./event";""");
    }

    [Fact]
    public void DeclaresNamespace ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Bar () { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Class {
                export function bar(): void;
            }
            """);
    }

    [Fact]
    public void DotsInSpaceArePreserved ()
    {
        AddAssembly(WithClass("Foo.Bar.Nya", "[Export] public static void Bar () { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Bar.Nya.Class {
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
            export type Record = Readonly<{
            }>;
            export enum Enum {
                A,
                B
            }

            export namespace Class {
                export function inv(r: Record): Enum;
            }
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
            export namespace Foo {
                export type Bar = Readonly<{
                }>;
            }

            export namespace Class {
                export function inv(r: Foo.Bar): void;
            }
            """);
    }

    [Fact]
    public void FunctionDeclarationIsExportedForInvokableMethod ()
    {
        AddAssembly(WithClass("Foo", "[Export] public static void Foo () { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Class {
                export function foo(): void;
            }
            """);
    }

    [Fact]
    public void AssignableVariableIsExportedForFunctionCallback ()
    {
        AddAssembly(WithClass("Foo", "[Import] public static void OnFoo () { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Class {
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
        Contains(
            """
            export namespace Foo.Class {
                export const expEvt: EventSubscriber<[]>;
                export const evt: EventSubscriber<[obj: string]>;
                export const impEvt: EventBroadcaster<[arg1: number, arg2: boolean | undefined]>;
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
        Contains(
            """
            export namespace Space {
                export interface Bar {
                }
                export interface Foo {
                }
            }

            export namespace Space.Class {
                export function getFoo(bar: Space.Bar): Space.Foo;
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
            export namespace SpaceA {
                export interface Foo {
                }
            }
            export namespace SpaceB {
                export interface Bar {
                }
            }

            export namespace Class {
                export function getFoo(bar: SpaceB.Bar): SpaceA.Foo;
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
        Contains(
            """
            export namespace Nya.Bar.Class {
                export function fun(): void;
            }
            export namespace Nya.Foo.Class {
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
            export interface Foo {
            }

            export namespace Class {
                export let onFoo: (foo: Foo) => void;
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
        Contains(
            """
            export namespace n {
                export interface Foo {
                    s: string;
                    i: number;
                }
            }

            export namespace n.Class {
                export function method(t: n.Foo): n.Foo;
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
        Contains(
            """
            export namespace n {
                export interface Base {
                }
                export interface Derived extends n.Base, n.Interface {
                    readonly foo: n.Interface;
                    bar(b: n.Interface): void;
                }
                export interface Interface {
                    readonly foo: n.Interface;
                    bar(b: n.Interface): void;
                }
            }

            export namespace n.Class {
                export function method(b: n.Base): n.Derived;
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
        Contains(
            """
            export namespace n {
                export interface Item {
                }
                export interface Container {
                    readonly items: Array<n.Item>;
                }
            }

            export namespace n.Class {
                export function combine(items: Array<n.Item>): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Container {
                    readonly items: Array<Array<n.Item>>;
                }
                export interface Item {
                }
            }

            export namespace n.Class {
                export function get(): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Item {
                }
                export interface Container {
                    readonly items: Array<n.Item>;
                }
            }

            export namespace n.Class {
                export function combine(items: Array<n.Item>): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Item {
                }
                export interface Container {
                    readonly items: Map<string, n.Item>;
                }
            }

            export namespace n.Class {
                export function combine(items: Map<string, n.Item>): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Item {
                }
                export interface Container {
                    readonly items: Map<string, n.Item>;
                }
            }

            export namespace n.Class {
                export function combine(items: Map<string, n.Item>): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Item {
                }
                export interface Container {
                    readonly items: Array<n.Item>;
                }
            }

            export namespace n.Class {
                export function combine(items: Array<n.Item>): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Item {
                }
                export interface Container {
                    readonly items: Array<n.Item>;
                }
            }

            export namespace n.Class {
                export function combine(items: Array<n.Item>): n.Container;
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
        Contains(
            """
            export namespace n {
                export interface Generic<T> {
                    value: T;
                }
                export interface GenericNull<T> {
                    readonly value?: T;
                    foo(t: T | undefined): T | null;
                }
            }

            export namespace n.Class {
                export function method(a: n.Generic<string>, b: n.GenericNull<number>): void;
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
        Contains(
            """
            export namespace n {
                export type Generic<T> = Readonly<{
                    value: T;
                }>;
                export type GenericNull<T> = Readonly<{
                    value?: T;
                }>;
            }

            export namespace n.Class {
                export function method(a: n.Generic<string>, b: n.GenericNull<number>): void;
            }
            """);
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericInterface ()
    {
        AddAssembly(
            With("n", "public interface IGenericInterface<T> { public T Value { get; set; } }"),
            WithClass("n", "[Export] public static IGenericInterface<string> Method () => default;"));
        Execute();
        Contains(
            """
            export namespace n {
                export interface IGenericInterface<T> {
                    value?: T;
                }
            }

            export namespace n.Class {
                export function method(): n.IGenericInterface<string>;
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
        Contains(
            """
            export namespace Bar {
                export interface GenericInterface<T> {
                    value?: T;
                }
            }
            export namespace Foo {
                export interface GenericClass<T> {
                    value?: T;
                }
            }

            export namespace n.Class {
                export function method(p: Foo.GenericClass<Bar.GenericInterface<string>>): void;
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
        Contains(
            """
            export namespace n {
                export interface GenericClass<T1, T2> {
                    key?: T1;
                    value?: T2;
                }
            }

            export namespace n.Class {
                export function method(p: n.GenericClass<string, number>): void;
            }
            """);
    }

    [Fact]
    public void CanCrawlCustomTypes ()
    {
        AddAssembly(
            With("Space",
                """
                public struct Struct { public double A { get; set; } }
                public readonly struct ReadonlyStruct { public double A { get; init; } }
                public readonly record struct ReadonlyRecordStruct(double A);
                public record class RecordClassA (double A, ReadonlyRecordStruct Str);
                public record class RecordClassB (double B) : RecordClassA(42, new(24));
                public enum Enum { A, B }
                public class Foo { public Struct S { get; } public ReadonlyStruct Rs { get; } }
                public class Bar : Foo { public Dictionary<string, RecordClassB> Rc { get; } }
                public class Baz { public List<Bar> Bars { get; } public Enum E { get; } }
                public class Class { [Export] public static Dictionary<string, Baz> GetBaz () => default; }
                """));
        Execute();
        Contains(
            """
            export namespace Space {
                export interface Baz {
                    readonly bars: Array<Space.Bar>;
                    readonly e: Space.Enum;
                }
                export interface Bar extends Space.Foo {
                    readonly rc: Map<string, Space.RecordClassB>;
                }
                export type RecordClassB = Space.RecordClassA & Readonly<{
                    b: number;
                }>;
                export type ReadonlyRecordStruct = Readonly<{
                    a: number;
                }>;
                export type RecordClassA = Readonly<{
                    a: number;
                    str: Space.ReadonlyRecordStruct;
                }>;
                export type Struct = Readonly<{
                    a: number;
                }>;
                export type ReadonlyStruct = Readonly<{
                    a: number;
                }>;
                export interface Foo {
                    readonly s: Space.Struct;
                    readonly rs: Space.ReadonlyStruct;
                }
                export enum Enum {
                    A,
                    B
                }
            }

            export namespace Space.Class {
                export function getBaz(): Map<string, Space.Baz>;
            }
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
                export type Foo = Readonly<{
                }>;
            }
            """);
    }

    [Fact]
    public void ComputedPropertiesAreIncluded ()
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
                export type Foo = Readonly<{
                    boo: boolean;
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
            export type Info = Readonly<{
                value: string;
            }>;

            export namespace Exported {
                export function inv(str: string, info: Info): Info;
            }
            export namespace Imported {
                export let fun: (str: string, info: Info) => Info;
            }
            """);
    }

    [Fact]
    public void GeneratesForMethodsInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { Info Inv (string str, Info info); void Reset (); }
            public interface IImported { Info Fun (Info info, string str); }

            public class Class
            {
                [Export] public static Task<IExported> GetExported (IImported inst) => default;
                [Import] public static Task<IImported> GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            export interface IImported {
                fun(info: Info, str: string): Info;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            export interface IExported {
                inv(str: string, info: Info): Info;
                reset(): void;
            }

            export namespace Class {
                export function getExported(inst: IImported): Promise<IExported>;
                export let getImported: (inst: IExported) => Promise<IImported>;
            }
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
            export type Info = Readonly<{
                value: string;
            }>;
            export interface IExportedInstanced {
            }
            export interface IImportedInstanced {
            }

            export namespace ExportedStatic {
                export let state: Info;
                export const optional: Info | undefined;
                export const exported: IExportedInstanced;
                export let imported: IImportedInstanced;
            }
            export namespace ImportedStatic {
                export const state: Info;
                export const imported: IImportedInstanced;
                export let exported: IExportedInstanced;
            }
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
                [Export] public static IExported GetExported (IImported inst) => default;
                [Import] public static IImported GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            export interface IImported {
                state: Info;
                readonly imported: IImported;
                exported: IExported;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            export interface IExported {
                state: Info;
                readonly exported: IExported;
                imported: IImported;
            }

            export namespace Class {
                export function getExported(inst: IImported): IExported;
                export let getImported: (inst: IExported) => IImported;
            }
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
            export type Info = Readonly<{
                value: string;
            }>;
            export interface IExportedInstanced {
            }
            export interface IImportedInstanced {
            }

            export namespace Exported {
                export const evt: EventSubscriber<[arg1: string, arg2: Info, arg3: IExportedInstanced]>;
            }
            export namespace Imported {
                export const evt: EventBroadcaster<[arg1: string, arg2: Info, arg3: IImportedInstanced]>;
            }
            """);
    }

    [Fact]
    public void GeneratesForEventsInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { event Action<Info>? Changed; event Action? Done; }
            public interface IImported { event Action<IImported, Info, string>? Changed; }

            public class Class
            {
                [Export] public static IExported GetExported (IImported inst) => default;
                [Import] public static IImported GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
            export interface IImported {
                changed: EventBroadcaster<[arg1: IImported, arg2: Info, arg3: string]>;
            }
            export type Info = Readonly<{
                value: string;
            }>;
            export interface IExported {
                changed: EventSubscriber<[obj: Info]>;
                done: EventSubscriber<[]>;
            }

            export namespace Class {
                export function getExported(inst: IImported): IExported;
                export let getImported: (inst: IExported) => IImported;
            }
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
            export interface Foo {
            }

            export namespace Class {
                export let fun: (bar: Array<number | null> | undefined, nya: Array<Array<Foo> | null> | undefined, far: Array<Array<Foo | null> | null> | undefined) => Array<Foo | null> | null;
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
        Contains(
            """
            export namespace n {
                export interface Bar {
                    readonly foo?: n.Foo;
                }
                export interface Foo {
                    readonly bool?: boolean;
                }
            }

            export namespace n.Class {
                export function fooBar(bar: n.Bar): n.Foo;
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
        Contains(
            """
            export namespace n {
                export interface Bar {
                    readonly foo?: n.Foo;
                }
                export enum Foo {
                    A,
                    B
                }
            }

            export namespace n.Class {
                export function getBar(): n.Bar;
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
        Once("export interface Foo");
        Once("export interface Bar");
        Once("export interface Far");
    }

    [Fact]
    public void RespectsSpacePrefInStaticMembers ()
    {
        AddAssembly(
            With(
                """
                [assembly: Bootsharp.Preferences(
                    Space = [@"^Foo\.Bar\.(\S+)", "$1"]
                )]
                """),
            With("Foo.Bar.Nya", "public class Nya { }"),
            WithClass("Foo.Bar.Fun", "[Import] public static void OnFun (Nya.Nya nya) { }"));
        Execute();
        Contains(
            """
            export namespace Nya {
                export interface Nya {
                }
            }

            export namespace Fun.Class {
                export let onFun: (nya: Nya.Nya) => void;
            }
            """);
    }

    [Fact]
    public void RespectsSpacePrefInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Preferences(Space = [@".+", "Foo"])]
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public enum Enum { A, B }

            public interface IExported { void Inv (string s, Enum e); }
            public interface IImported { void Fun (string s, Enum e); }
            """));
        Execute();
        Contains(
            """
            export namespace Foo {
                export enum Enum {
                    A,
                    B
                }
            }

            export namespace Foo {
                export function inv(s: string, e: Foo.Enum): void;
                export let fun: (s: string, e: Foo.Enum) => void;
            }
            """);
    }

    [Fact]
    public void RespectsTypePreference ()
    {
        AddAssembly(With(
            """
            [assembly: Bootsharp.Preferences(
                Type = [@"Record", "Foo", @".+`.+", "Bar<T>"]
            )]

            public record Record;
            public record Generic<T>;

            public class Class
            {
                [Export] public static void Inv (Record r, Generic<string> g) {}
            }
            """));
        Execute();
        Contains(
            """
            export type Foo = Readonly<{
            }>;
            export type Bar<T> = Readonly<{
            }>;

            export namespace Class {
                export function inv(r: Foo, g: Bar<T>): void;
            }
            """);
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
        DoesNotContain("Record");
        DoesNotContain("export function inv");
        DoesNotContain("export let fun");
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
            public record Payload
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
            public delegate void PayloadChanged (Payload payload, string label);

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
                [Export] public static Payload Get (Kind kind) => default;

                /// <summary>Gets exported instance.</summary>
                [Export] public static IExportedInstanced GetExported () => default;

                /// <summary>Receives foo.</summary>
                /// <param name="count">Count to receive.</param>
                [Import] public static void OnFoo (int count) { }

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
            export type Payload = Readonly<{
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
                export const expEvt: EventSubscriber<[obj: boolean]>;
                /**
                 * Imports completion signal.
                 */
                export const impEvt: EventBroadcaster<[arg1: string, arg2: number]>;
                /**
                 * Exports payload changes.
                 * @param payload Payload from custom delegate.
                 * @param label Label from custom delegate.
                 */
                export const payloadChanged: EventSubscriber<[payload: Payload, label: string]>;
                /**
                 * Imports handler signal.
                 * @param sender Sender from event handler.
                 * @param e Payload from event handler.
                 */
                export const handlerEvt: EventBroadcaster<[sender: any | undefined, e: HandlerArgs]>;
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
                export function get(kind: Kind): Payload;
                /**
                 * Gets exported instance.
                 */
                export function getExported(): IExportedInstanced;
                /**
                 * Receives foo.
                 * @param count Count to receive.
                 */
                export let onFoo: (count: number) => void;
                /**
                 * @param value Value without summary.
                 */
                export let onParamOnly: (value: string) => void;
            }
            """);
    }
}
