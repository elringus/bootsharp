namespace Bootsharp.Publish.Test;

public class DeclarationTest : PackTest
{
    protected override string TestedContent => GeneratedDeclarations;

    [Fact]
    public void ImportsEventType ()
    {
        Execute();
        Contains("""import type { Event } from "./event";""");
    }

    [Fact]
    public void DeclaresNamespace ()
    {
        AddAssembly(WithClass("Foo", "[JSInvokable] public static void Bar () { }"));
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
        AddAssembly(WithClass("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Bar.Nya.Class {
                export function bar(): void;
            }
            """);
    }

    [Fact]
    public void FunctionDeclarationIsExportedForInvokableMethod ()
    {
        AddAssembly(WithClass("Foo", "[JSInvokable] public static void Foo () { }"));
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
        AddAssembly(WithClass("Foo", "[JSFunction] public static void OnFoo () { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Class {
                export let onFoo: () => void;
            }
            """);
    }

    [Fact]
    public void EventPropertiesAreExportedForEventMethods ()
    {
        AddAssembly(
            WithClass("Foo", "[JSEvent] public static void OnFoo () { }"),
            WithClass("Foo", "[JSEvent] public static void OnBar (string baz) { }"),
            WithClass("Foo", "[JSEvent] public static void OnFar (int yaz, bool? nya) { }"));
        Execute();
        Contains(
            """
            export namespace Foo.Class {
                export const onFoo: Event<[]>;
                export const onBar: Event<[baz: string]>;
                export const onFar: Event<[yaz: number, nya: boolean | undefined]>;
            }
            """);
    }

    [Fact]
    public void MembersFromSameSpaceAreDeclaredUnderSameSpace ()
    {
        AddAssembly(
            With("Space", "public class Foo { }"),
            With("Space", "public class Bar { }"),
            WithClass("Space", "[JSInvokable] public static Foo GetFoo (Bar bar) => default;"));
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
            WithClass("[JSInvokable] public static SpaceA.Foo GetFoo (SpaceB.Bar bar) => default;"));
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

            export namespace Global.Class {
                export function getFoo(bar: SpaceB.Bar): SpaceA.Foo;
            }
            """);
    }

    [Fact]
    public void DifferentSpacesWithSameRootAreDeclaredIndividually ()
    {
        AddAssembly(
            WithClass("Nya.Bar", "[JSInvokable] public static void Fun () { }"),
            WithClass("Nya.Foo", "[JSInvokable] public static void Foo () { }"));
        Execute();
        Contains("export namespace Nya.Bar.Class {\n    export function fun(): void;\n}");
        Contains("export namespace Nya.Foo.Class {\n    export function foo(): void;\n}");
    }

    [Fact]
    public void WhenNoSpaceTypesAreDeclaredUnderGlobalSpace ()
    {
        AddAssembly(
            With("public class Foo { }"),
            WithClass("[JSFunction] public static void OnFoo (Foo foo) { }"));
        Execute();
        Contains("export namespace Global {\n    export interface Foo {\n    }\n}");
        Contains("export namespace Global.Class {\n    export let onFoo: (foo: Global.Foo) => void;\n}");
    }

    [Fact]
    public void NamespaceAttributeOverrideSpaceNames ()
    {
        AddAssembly(
            With("""[assembly:JSNamespace(@"Foo\.Bar\.(\S+)", "$1")]"""),
            With("Foo.Bar.Nya", "public class Nya { }"),
            WithClass("Foo.Bar.Fun", "[JSFunction] public static void OnFun (Nya.Nya nya) { }"));
        Execute();
        Contains("export namespace Nya {\n    export interface Nya {\n    }\n}");
        Contains("export namespace Fun.Class {\n    export let onFun: (nya: Nya.Nya) => void;\n}");
    }

    [Fact]
    public void NumericsTranslatedToNumber ()
    {
        var types = new[] { "byte", "sbyte", "ushort", "uint", "ulong", "short", "int", "decimal", "double", "float" };
        var csArgs = string.Join(", ", types.Select(n => $"{n} v{Array.IndexOf(types, n)}"));
        var tsArgs = string.Join(", ", types.Select(n => $"v{Array.IndexOf(types, n)}: number"));
        AddAssembly(WithClass($"[JSInvokable] public static void Num ({csArgs}) {{ }}"));
        Execute();
        Contains($"num({tsArgs})");
    }

    [Fact]
    public void Int64TranslatedToBigInt ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Foo (long bar) {}"));
        Execute();
        Contains("foo(bar: bigint): void");
    }

    [Fact]
    public void TaskTranslatedToPromise ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static Task<bool> AsyBool () => default;"),
            WithClass("[JSInvokable] public static Task AsyVoid () => default;"));
        Execute();
        Contains("asyBool(): Promise<boolean>");
        Contains("asyVoid(): Promise<void>");
    }

    [Fact]
    public void CharAndStringTranslatedToString ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Cha (char c, string s) {}"));
        Execute();
        Contains("cha(c: string, s: string): void");
    }

    [Fact]
    public void BoolTranslatedToBoolean ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Boo (bool b) {}"));
        Execute();
        Contains("boo(b: boolean): void");
    }

    [Fact]
    public void DateTimeTranslatedToDate ()
    {
        AddAssembly(WithClass("[JSInvokable] public static void Doo (DateTime time) {}"));
        Execute();
        Contains("doo(time: Date): void");
    }

    [Fact]
    public void ListAndArrayTranslatedToArray ()
    {
        AddAssembly(WithClass("[JSInvokable] public static List<string> Goo (DateTime[] d) => default;"));
        Execute();
        Contains("goo(d: Array<Date>): Array<string>");
    }

    [Fact]
    public void JaggedArrayAndListOfListsTranslatedToArrayOfArrays ()
    {
        AddAssembly(WithClass("[JSInvokable] public static List<List<string>> Goo (DateTime[][] d) => default;"));
        Execute();
        Contains("goo(d: Array<Array<Date>>): Array<Array<string>>");
    }

    [Fact]
    public void IntArraysTranslatedToRelatedTypes ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static void Uint8 (byte[] foo) {}"),
            WithClass("[JSInvokable] public static void Int8 (sbyte[] foo) {}"),
            WithClass("[JSInvokable] public static void Uint16 (ushort[] foo) {}"),
            WithClass("[JSInvokable] public static void Int16 (short[] foo) {}"),
            WithClass("[JSInvokable] public static void Uint32 (uint[] foo) {}"),
            WithClass("[JSInvokable] public static void Int32 (int[] foo) {}"),
            WithClass("[JSInvokable] public static void BigInt64 (long[] foo) {}"));
        Execute();
        Contains("uint8(foo: Uint8Array): void");
        Contains("int8(foo: Int8Array): void");
        Contains("uint16(foo: Uint16Array): void");
        Contains("int16(foo: Int16Array): void");
        Contains("uint32(foo: Uint32Array): void");
        Contains("int32(foo: Int32Array): void");
        Contains("bigInt64(foo: BigInt64Array): void");
    }

    [Fact]
    public void DefinitionIsGeneratedForObjectType ()
    {
        AddAssembly(
            With("n", "public class Foo { public string S { get; set; } public int I { get; set; } }"),
            WithClass("n", "[JSInvokable] public static Foo Method (Foo t) => default;"));
        Execute();
        Matches(@"export interface Foo {\s*s: string;\s*i: number;\s*}");
        Contains("method(t: n.Foo): n.Foo");
    }

    [Fact]
    public void DefinitionIsGeneratedForInterfaceAndImplementation ()
    {
        AddAssembly(
            With("n", "public interface Interface { Interface Foo { get; } void Bar (Interface b); }"),
            With("n", "public class Base { }"),
            With("n", "public class Derived : Base, Interface { public Interface Foo { get; } public void Bar (Interface b) {} }"),
            WithClass("n", "[JSInvokable] public static Derived Method (Interface b) => default;"));
        Execute();
        Matches(@"export interface Interface {\s*foo: n.Interface;\s*}");
        Matches(@"export interface Base {\s*}");
        Matches(@"export interface Derived extends n.Base, n.Interface {\s*foo: n.Interface;\s*}");
        Contains("method(b: n.Interface): n.Derived");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithListProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public List<Item> Items { get; } }"),
            WithClass("n", "[JSInvokable] public static Container Combine (List<Item> items) => default;"));
        Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export interface Container {\s*items: Array<n.Item>;\s*}");
        Contains("combine(items: Array<n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithJaggedArrayProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public Item[][] Items { get; } }"),
            WithClass("n", "[JSInvokable] public static Container Get () => default;"));
        Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export interface Container {\s*items: Array<Array<n.Item>>;\s*}");
        Contains("get(): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyListProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyList<Item> Items { get; } }"),
            WithClass("n", "[JSInvokable] public static Container Combine (IReadOnlyList<Item> items) => default;"));
        Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export interface Container {\s*items: Array<n.Item>;\s*}");
        Contains("combine(items: Array<n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithDictionaryProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public Dictionary<string, Item> Items { get; } }"),
            WithClass("n", "[JSInvokable] public static Container Combine (Dictionary<string, Item> items) => default;"));
        Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export interface Container {\s*items: Map<string, n.Item>;\s*}");
        Contains("combine(items: Map<string, n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyDictionaryProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyDictionary<string, Item> Items { get; } }"),
            WithClass("n", "[JSInvokable] public static Container Combine (IReadOnlyDictionary<string, Item> items) => default;"));
        Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export interface Container {\s*items: Map<string, n.Item>;\s*}");
        Contains("combine(items: Map<string, n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericClass ()
    {
        AddAssembly(
            With("n", "public class Generic<T> where T: notnull { public T Value { get; set; } }"),
            With("n", "public class GenericNull<T> { public T Value { get; set; } }"),
            WithClass("n", "[JSInvokable] public static void Method (Generic<string> a, GenericNull<int> b) { }"));
        Execute();
        Contains(
            """
            export namespace n {
                export interface Generic<T> {
                    value: T;
                }
                export interface GenericNull<T> {
                    value?: T;
                }
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
            With("n", "public interface GenericInterface<T> { public T Value { get; set; } }"),
            WithClass("n", "[JSInvokable] public static GenericInterface<string> Method () => default;"));
        Execute();
        Matches(@"export interface GenericInterface<T> {\s*value\?: T;\s*}");
        Contains("method(): n.GenericInterface<string>");
    }

    [Fact]
    public void DefinitionIsGeneratedForNestedGenericTypes ()
    {
        AddAssembly(
            With("Foo", "public class GenericClass<T> { public T Value { get; set; } }"),
            With("Bar", "public interface GenericInterface<T> { public T Value { get; set; } }"),
            WithClass("n", "[JSInvokable] public static void Method (Foo.GenericClass<Bar.GenericInterface<string>> p) { }"));
        Execute();
        Matches(@"export namespace Foo {\s*export interface GenericClass<T> {\s*value\?: T;\s*}\s*}");
        Matches(@"export namespace Bar {\s*export interface GenericInterface<T> {\s*value\?: T;\s*}\s*}");
        Contains("method(p: Foo.GenericClass<Bar.GenericInterface<string>>): void");
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericClassWithMultipleTypeArguments ()
    {
        AddAssembly(
            WithClass("n", "public class GenericClass<T1, T2> { public T1 Key { get; set; } public T2 Value { get; set; } }"),
            WithClass("n", "[JSInvokable] public static void Method (GenericClass<string, int> p) { }"));
        Execute();
        Matches(@"export interface GenericClass<T1, T2> {\s*key\?: T1;\s*value\?: T2;\s*}");
        Contains("method(p: n.Class.GenericClass<string, number>): void");
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
                public record class RecordClass(double A);
                public enum Enum { A, B }
                public class Foo { public Struct S { get; } public ReadonlyStruct Rs { get; } }
                public class Bar : Foo { public ReadonlyRecordStruct Rrs { get; } public RecordClass Rc { get; } }
                public class Baz { public List<Bar> Bars { get; } public Enum E { get; } }
                public class Class { [JSInvokable] public static Baz GetBaz () => default; }
                """));
        Execute();
        Contains(
            """
            export namespace Space {
                export interface Baz {
                    bars: Array<Space.Bar>;
                    e: Space.Enum;
                }
                export interface Bar extends Space.Foo {
                    rrs: Space.ReadonlyRecordStruct;
                    rc: Space.RecordClass;
                }
                export interface ReadonlyRecordStruct {
                    a: number;
                }
                export interface RecordClass {
                    a: number;
                }
                export interface Struct {
                    a: number;
                }
                export interface ReadonlyStruct {
                    a: number;
                }
                export interface Foo {
                    s: Space.Struct;
                    rs: Space.ReadonlyStruct;
                }
                export enum Enum {
                    A,
                    B
                }
            }

            export namespace Space.Class {
                export function getBaz(): Space.Baz;
            }
            """);
    }

    [Fact]
    public void OtherTypesAreTranslatedToAny ()
    {
        AddAssembly(WithClass("[JSInvokable] public static DBNull Method (IEnumerable<string> t) => default;"));
        Execute();
        Contains("method(t: any): any");
    }

    [Fact]
    public void StaticPropertiesAreNotIncluded ()
    {
        AddAssembly(
            WithClass("public class Foo { public static string Soo { get; } }"),
            WithClass("[JSInvokable] public static Foo Bar () => default;"));
        Execute();
        Matches(@"export interface Foo {\s*}");
    }

    [Fact]
    public void ExpressionPropertiesAreNotIncluded ()
    {
        AddAssembly(
            WithClass("public class Foo { public bool Boo => true; }"),
            WithClass("[JSInvokable] public static Foo Bar () => default;"));
        Execute();
        Matches(@"export interface Foo {\s*}");
    }

    [Fact]
    public void NullableMethodArgumentsUnionWithUndefined ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static void Foo (string? bar) { }"),
            WithClass("[JSFunction] public static void Fun (int? nya) { }"));
        Execute();
        Contains("export function foo(bar: string | undefined): void;");
        Contains("export let fun: (nya: number | undefined) => void;");
    }

    [Fact]
    public void NullableMethodReturnTypesUnionWithNull ()
    {
        AddAssembly(
            WithClass("[JSInvokable] public static string? Foo () => default;"),
            WithClass("[JSInvokable] public static Task<byte[]?> Bar () => default;"),
            WithClass("[JSFunction] public static ValueTask<List<string>?> Nya () => default;"));
        Execute();
        Contains("export function foo(): string | null;");
        Contains("export function bar(): Promise<Uint8Array | null>;");
        Contains("export let nya: () => Promise<Array<string> | null>;");
    }

    [Fact]
    public void NullableCollectionElementTypesUnionWithNull ()
    {
        AddAssembly(
            With("public class Foo { }"),
            WithClass("[JSFunction] public static List<Foo?>? Fun (int?[]? bar, Foo[]?[]? nya, Foo?[]?[]? far) => default;"));
        Execute();
        Contains(
            """
            export namespace Global {
                export interface Foo {
                }
            }

            export namespace Global.Class {
                export let fun: (bar: Array<number | null> | undefined, nya: Array<Array<Global.Foo> | null> | undefined, far: Array<Array<Global.Foo | null> | null> | undefined) => Array<Global.Foo | null> | null;
            }
            """);
    }

    [Fact]
    public void NullableCollectionElementTypesOfCustomTypeUnionWithNull ()
    {
        AddAssembly(
            With("public interface IFoo<T> { }"),
            With("public record Foo (List<List<IFoo<string>?>?>? Bar, IFoo<int>?[]?[]? Nya) : IFoo<bool>;"),
            WithClass("[JSFunction] public static IFoo<bool> Fun (Foo foo) => default;"));
        Execute();
        Contains("bar?: Array<Array<Global.IFoo<string> | null> | null>;");
        Contains("nya?: Array<Array<Global.IFoo<number> | null> | null>;");
    }

    [Fact]
    public void NullablePropertiesHaveOptionalModificator ()
    {
        AddAssembly(
            With("n", "public class Foo { public bool? Bool { get; } }"),
            With("n", "public class Bar { public Foo? Foo { get; } }"),
            WithClass("n", "[JSInvokable] public static Foo FooBar (Bar bar) => default;"));
        Execute();
        Contains(
            """
            export namespace n {
                export interface Bar {
                    foo?: n.Foo;
                }
                export interface Foo {
                    bool?: boolean;
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
            WithClass("n", "[JSInvokable] public static Bar GetBar () => default;"));
        Execute();
        Contains(
            """
            export namespace n {
                export interface Bar {
                    foo?: n.Foo;
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
            WithClass("[JSInvokable] public static Bar TakeFooGiveBar (Foo f) => default;"),
            WithClass("[JSInvokable] public static Foo TakeBarGiveFoo (Bar b) => default;"),
            WithClass("[JSInvokable] public static Far TakeAllGiveFar (Foo f, Bar b, Far ff) => default;"));
        Execute();
        Assert.Single(Matches("export interface Foo"));
        Assert.Single(Matches("export interface Bar"));
        Assert.Single(Matches("export interface Far"));
    }
}
