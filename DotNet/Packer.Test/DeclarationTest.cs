using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Packer.Test;

public class DeclarationTest : ContentTest
{
    protected override string TestedContent => Data.GeneratedDeclaration;

    [Fact]
    public void ContainsRequiredLibraryDeclarations ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "event.d.ts"), "event");
        File.WriteAllText(Path.Combine(Data.JSDir, "interop.d.ts"), "interop");
        File.WriteAllText(Path.Combine(Data.JSDir, "boot.d.ts"), "boot");
        Task.Execute();
        Contains("event");
        Contains("interop");
        Contains("boot");
    }

    [Fact]
    public void DoesntContainOtherLibraryDeclarations ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "other.d.ts"), "other");
        Task.Execute();
        Assert.DoesNotContain("other", Data.GeneratedDeclaration);
    }

    [Fact]
    public void ResolvesImportForRequiredLibraryDeclarations ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "dep.d.ts"), "dep");
        File.WriteAllText(Path.Combine(Data.JSDir, "boot.d.ts"), "import from \"./dep\";\nboot");
        Task.Execute();
        Assert.DoesNotContain("import", TestedContent);
        Contains("dep");
        Contains("boot");
    }

    [Fact]
    public void WhenImportResolveFailsExceptionIsThrown ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "boot.d.ts"), "import from \"./dep\";\nboot");
        Assert.Throws<PackerException>(() => Task.Execute());
    }

    [Fact]
    public void WhenEmbeddedOverridesBootSignature ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "boot.d.ts"), "boot(bootData: BootData): Promise<void>");
        Task.EmbedBinaries = true;
        Task.Execute();
        Contains("boot(): Promise<void>");
    }

    [Fact]
    public void WhenSideLoadDoesntOverrideBootSignature ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "boot.d.ts"), "boot(bootData: BootData): Promise<void>");
        Task.EmbedBinaries = false;
        Task.Execute();
        Contains("boot(bootData: BootData): Promise<void>");
    }

    [Fact]
    public void WhenSideLoadHasBootUrisDeclarations ()
    {
        Task.EmbedBinaries = false;
        Task.Execute();
        Contains("export interface BootUris");
        Contains("export declare function getBootUris");
    }

    [Fact]
    public void DeclaresNamespace ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("export namespace Foo {");
    }

    [Fact]
    public void DotsInSpaceArePreserved ()
    {
        AddAssembly(With("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("export namespace Foo.Bar.Nya {");
    }

    [Fact]
    public void FunctionDeclarationIsExportedForInvokableMethod ()
    {
        AddAssembly(With("Foo", "[JSInvokable] public static void Foo () { }"));
        Task.Execute();
        Contains("export namespace Foo {\n    export function Foo(): void;\n}");
    }

    [Fact]
    public void AssignableVariableIsExportedForFunctionCallback ()
    {
        AddAssembly(With("Foo", "[JSFunction] public static void OnFoo () { }"));
        Task.Execute();
        Contains("export namespace Foo {\n    export let OnFoo: () => void;\n}");
    }

    [Fact]
    public void EventPropertyIsExportedForEventMethod ()
    {
        AddAssembly(With("Foo", "[JSEvent] public static void OnFoo () { }"));
        Task.Execute();
        Contains("export namespace Foo {\n    export const OnFoo: Event<[]>;\n}");
    }

    [Fact]
    public void GenericEventIsExportedForEventMethodWithArguments ()
    {
        AddAssembly(With("Foo", "[JSEvent] public static void OnFoo (string bar, int nya) { }"));
        Task.Execute();
        Contains("export namespace Foo {\n    export const OnFoo: Event<[string, number]>;\n}");
    }

    [Fact]
    public void MembersFromSameSpaceAreDeclaredUnderSameSpace ()
    {
        AddAssembly(
            With("Foo", "public class Foo { }"),
            With("Foo", "[JSInvokable] public static Foo GetFoo () => default;"));
        Task.Execute();
        Contains("export namespace Foo {\n    export class Foo {\n    }\n}");
        Contains("export namespace Foo {\n    export function GetFoo(): Foo.Foo;\n}");
    }

    [Fact]
    public void MembersFromDifferentSpacesAreDeclaredUnderRespectiveSpaces ()
    {
        AddAssembly(
            With("Foo", "public class Foo { }", false),
            With("Bar", "[JSInvokable] public static Foo.Foo GetFoo () => default;"));
        Task.Execute();
        Contains("export namespace Foo {\n    export class Foo {\n    }\n}");
        Contains("export namespace Bar {\n    export function GetFoo(): Foo.Foo;\n}");
    }

    [Fact]
    public void MultipleSpacesAreDeclaredFromNewLine ()
    {
        AddAssembly(
            With("a", "[JSInvokable] public static void Foo () { }"),
            With("b", "[JSInvokable] public static void Bar () { }"));
        Task.Execute();
        Contains("\nexport namespace b");
    }

    [Fact]
    public void DifferentSpacesWithSameRootAreDeclaredIndividually ()
    {
        AddAssembly(
            With("Nya.Bar", "[JSInvokable] public static void Fun () { }"),
            With("Nya.Foo", "[JSInvokable] public static void Foo () { }"));
        Task.Execute();
        Contains("export namespace Nya.Bar {\n    export function Fun(): void;\n}");
        Contains("export namespace Nya.Foo {\n    export function Foo(): void;\n}");
    }

    [Fact]
    public void WhenNoSpaceTypesAreDeclaredUnderBindingsSpace ()
    {
        AddAssembly(
            With("public class Foo { }", false),
            With("[JSFunction] public static void OnFoo (Foo foo) { }"));
        Task.Execute();
        Contains("export namespace Bindings {\n    export class Foo {\n    }\n}");
        Contains("export namespace Bindings {\n    export let OnFoo: (foo: Bindings.Foo) => void;\n}");
    }

    [Fact]
    public void NamespaceAttributeOverrideSpaceNames ()
    {
        AddAssembly(
            With(@"[assembly:JSNamespace(@""Foo\.Bar\.(\S+)"", ""$1"")]", false),
            With("Foo.Bar.Nya", "public class Nya { }", false),
            With("Foo.Bar.Fun", "[JSFunction] public static void OnFun (Nya.Nya nya) { }"));
        Task.Execute();
        Contains("export namespace Nya {\n    export class Nya {\n    }\n}");
        Contains("export namespace Fun {\n    export let OnFun: (nya: Nya.Nya) => void;\n}");
    }

    [Fact]
    public void NumericsTranslatedToNumber ()
    {
        var nums = new[] { "byte", "sbyte", "ushort", "uint", "ulong", "short", "int", "long", "decimal", "double", "float" };
        var csArgs = string.Join(", ", nums.Select(n => $"{n} v{Array.IndexOf(nums, n)}"));
        var tsArgs = string.Join(", ", nums.Select(n => $"v{Array.IndexOf(nums, n)}: number"));
        AddAssembly(With($"[JSInvokable] public static void Num ({csArgs}) {{ }}"));
        Task.Execute();
        Contains($"Num({tsArgs})");
    }

    [Fact]
    public void TaskTranslatedToPromise ()
    {
        AddAssembly(
            With("[JSInvokable] public static Task<bool> AsyBool () => default;"),
            With("[JSInvokable] public static ValueTask AsyVoid () => default;"));
        Task.Execute();
        Contains("AsyBool(): Promise<boolean>");
        Contains("AsyVoid(): Promise<void>");
    }

    [Fact]
    public void CharAndStringTranslatedToString ()
    {
        AddAssembly(With("[JSInvokable] public static void Cha (char c, string s) {}"));
        Task.Execute();
        Contains("Cha(c: string, s: string): void");
    }

    [Fact]
    public void BoolTranslatedToBoolean ()
    {
        AddAssembly(With("[JSInvokable] public static void Boo (bool b) {}"));
        Task.Execute();
        Contains("Boo(b: boolean): void");
    }

    [Fact]
    public void DateTimeTranslatedToDate ()
    {
        AddAssembly(With("[JSInvokable] public static void Doo (DateTime time) {}"));
        Task.Execute();
        Contains("Doo(time: Date): void");
    }

    [Fact]
    public void ListAndArrayTranslatedToArray ()
    {
        AddAssembly(With("[JSInvokable] public static List<string> Goo (DateTime[] d) => default;"));
        Task.Execute();
        Contains("Goo(d: Array<Date>): Array<string>");
    }

    [Fact]
    public void IntArraysTranslatedToRelatedTypes ()
    {
        AddAssembly(
            With("[JSInvokable] public static void Uint8 (byte[] foo) {}"),
            With("[JSInvokable] public static void Int8 (sbyte[] foo) {}"),
            With("[JSInvokable] public static void Uint16 (ushort[] foo) {}"),
            With("[JSInvokable] public static void Int16 (short[] foo) {}"),
            With("[JSInvokable] public static void Uint32 (uint[] foo) {}"),
            With("[JSInvokable] public static void Int32 (int[] foo) {}")
        );
        Task.Execute();
        Contains("Uint8(foo: Uint8Array): void");
        Contains("Int8(foo: Int8Array): void");
        Contains("Uint16(foo: Uint16Array): void");
        Contains("Int16(foo: Int16Array): void");
        Contains("Uint32(foo: Uint32Array): void");
        Contains("Int32(foo: Int32Array): void");
    }

    [Fact]
    public void DefinitionIsGeneratedForObjectType ()
    {
        AddAssembly(
            With("n", "public class Foo { public string S { get; set; } public int I { get; set; } }"),
            With("n", "[JSInvokable] public static Foo Method (Foo t) => default;"));
        Task.Execute();
        Matches(@"export class Foo {\s*s: string;\s*i: number;\s*}");
        Contains("Method(t: n.Foo): n.Foo");
    }

    [Fact]
    public void DefinitionIsGeneratedForInterfaceAndImplementation ()
    {
        AddAssembly(
            With("n", "public interface Base { Base Foo { get; } void Bar (Base b); }"),
            With("n", "public class Derived : Base { public Base Foo { get; } public void Bar (Base b) {} }"),
            With("n", "[JSInvokable] public static Derived Method (Base b) => default;"));
        Task.Execute();
        Matches(@"export interface Base {\s*foo: n.Base;\s*}");
        Matches(@"export class Derived implements n.Base {\s*foo: n.Base;\s*}");
        Contains("Method(b: n.Base): n.Derived");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithListProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public List<Item> Items { get; } }"),
            With("n", "[JSInvokable] public static Container Combine (List<Item> items) => default;"));
        Task.Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export class Container {\s*items: Array<n.Item>;\s*}");
        Contains("Combine(items: Array<n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyListProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyList<Item> Items { get; } }"),
            With("n", "[JSInvokable] public static Container Combine (IReadOnlyList<Item> items) => default;"));
        Task.Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export class Container {\s*items: Array<n.Item>;\s*}");
        Contains("Combine(items: Array<n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithDictionaryProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public Dictionary<string, Item> Items { get; } }"),
            With("n", "[JSInvokable] public static Container Combine (Dictionary<string, Item> items) => default;"));
        Task.Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export class Container {\s*items: Map<string, n.Item>;\s*}");
        Contains("Combine(items: Map<string, n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithReadOnlyDictionaryProperty ()
    {
        AddAssembly(
            With("n", "public interface Item { }"),
            With("n", "public class Container { public IReadOnlyDictionary<string, Item> Items { get; } }"),
            With("n", "[JSInvokable] public static Container Combine (IReadOnlyDictionary<string, Item> items) => default;"));
        Task.Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export class Container {\s*items: Map<string, n.Item>;\s*}");
        Contains("Combine(items: Map<string, n.Item>): n.Container");
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericClass ()
    {
        AddAssembly(
            With("n", "public class GenericClass<T> { public T Value { get; set; } }"),
            With("n", "[JSInvokable] public static void Method (GenericClass<string> p) { }"));
        Task.Execute();
        Matches(@"export class GenericClass<T> {\s*value: T;\s*}");
        Contains("Method(p: n.GenericClass<string>): void");
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericInterface ()
    {
        AddAssembly(
            With("n", "public interface GenericInterface<T> { public T Value { get; set; } }"),
            With("n", "[JSInvokable] public static GenericInterface<string> Method () => default;"));
        Task.Execute();
        Matches(@"export interface GenericInterface<T> {\s*value: T;\s*}");
        Contains("Method(): n.GenericInterface<string>");
    }

    [Fact]
    public void DefinitionIsGeneratedForNestedGenericTypes ()
    {
        AddAssembly(
            With("Foo", "public class GenericClass<T> { public T Value { get; set; } }", false),
            With("Bar", "public interface GenericInterface<T> { public T Value { get; set; } }", false),
            With("n", "[JSInvokable] public static void Method (Foo.GenericClass<Bar.GenericInterface<string>> p) { }"));
        Task.Execute();
        Matches(@"export namespace Foo {\s*export class GenericClass<T> {\s*value: T;\s*}\s*}");
        Matches(@"export namespace Bar {\s*export interface GenericInterface<T> {\s*value: T;\s*}\s*}");
        Contains("Method(p: Foo.GenericClass<Bar.GenericInterface<string>>): void");
    }

    [Fact]
    public void DefinitionIsGeneratedForGenericClassWithMultipleTypeArguments ()
    {
        AddAssembly(
            With("n", "public class GenericClass<T1, T2> { public T1 Key { get; set; } public T2 Value { get; set; } }"),
            With("n", "[JSInvokable] public static void Method (GenericClass<string, int> p) { }"));
        Task.Execute();
        Matches(@"export class GenericClass<T1, T2> {\s*key: T1;\s*value: T2;\s*}");
        Contains("Method(p: n.GenericClass<string, number>): void");
    }

    [Fact]
    public void CanCrawlCustomTypes ()
    {
        AddAssembly(
            With("n", "public enum Nyam { A, B }"),
            With("n", "public class Foo { public Nyam Nyam { get; } }"),
            With("n", "public class Bar : Foo { }"),
            With("n", "public class Barrel { public List<Bar> Bars { get; } }"),
            With("n", "[JSInvokable] public static Barrel GetBarrel () => default;"));
        Task.Execute();
        Matches(@"export enum Nyam {\s*A,\s*B\s*}");
        Matches(@"export class Foo {\s*nyam: n.Nyam;\s*}");
        Matches(@"export class Bar extends n.Foo {\s*}");
    }

    [Fact]
    public void OtherTypesAreTranslatedToAny ()
    {
        AddAssembly(With("[JSInvokable] public static DBNull Method (DBNull t) => default;"));
        Task.Execute();
        Contains("Method(t: any): any");
    }

    [Fact]
    public void StaticPropertiesAreNotIncluded ()
    {
        AddAssembly(
            With("public class Foo { public static string Soo { get; } }"),
            With("[JSInvokable] public static Foo Bar () => default;"));
        Task.Execute();
        Matches(@"export class Foo {\s*}");
    }

    [Fact]
    public void ExpressionPropertiesAreNotIncluded ()
    {
        AddAssembly(
            With("public class Foo { public bool Boo => true; }"),
            With("[JSInvokable] public static Foo Bar () => default;"));
        Task.Execute();
        Matches(@"export class Foo {\s*}");
    }

    [Fact]
    public void NullableMethodArgumentsUnionWithUndefined ()
    {
        AddAssembly(
            With("[JSInvokable] public static void Foo (string? bar) { }"),
            With("[JSFunction] public static void Fun (int? nya) { }")
        );
        Task.Execute();
        Contains("export function Foo(bar: string | undefined): void;");
        Contains("export let Fun: (nya: number | undefined) => void;");
    }

    [Fact]
    public void NullableMethodReturnTypesUnionWithUndefined ()
    {
        AddAssembly(
            With("[JSInvokable] public static string? Foo () => default;"),
            With("[JSInvokable] public static Task<byte[]?> Bar () => default;"),
            With("[JSFunction] public static ValueTask<List<string>?> Nya () => default;")
        );
        Task.Execute();
        Contains("export function Foo(): string | undefined;");
        Contains("export function Bar(): Promise<Uint8Array | undefined>;");
        Contains("export let Nya: () => Promise<Array<string> | undefined>;");
    }

    [Fact]
    public void NullablePropertiesHaveOptionalModificator ()
    {
        AddAssembly(
            With("n", "public class Foo { public bool? Bool { get; } }"),
            With("n", "public class Bar { public Foo? Foo { get; } }"),
            With("n", "[JSInvokable] public static Foo FooBar (Bar bar) => default;"));
        Task.Execute();
        Matches(@"export class Foo {\s*bool\?: boolean;\s*}");
        Matches(@"export class Bar {\s*foo\?: n.Foo;\s*}");
    }

    [Fact]
    public void NullableEnumsAreCrawled ()
    {
        AddAssembly(
            With("n", "public enum Foo { A, B }"),
            With("n", "public class Bar { public Foo? Foo { get; } }"),
            With("n", "[JSInvokable] public static Bar GetBar () => default;"));
        Task.Execute();
        Matches(@"export enum Foo {\s*A,\s*B\s*}");
        Matches(@"export class Bar {\s*foo\?: n.Foo;\s*}");
    }

    [Fact]
    public void NullableEventTypesUnionWithUndefined ()
    {
        AddAssembly(With("[JSEvent] public static void OnFoo (string? bar) { }"));
        Task.Execute();
        Contains("export const OnFoo: Event<[string | undefined]>;");
    }

    [Fact]
    public void WhenTypeReferencedMultipleTimesItsDeclaredOnlyOnce ()
    {
        AddAssembly(
            With("public interface Foo { }"),
            With("public class Bar : Foo { public Foo Foo { get; } }"),
            With("public class Far : Bar { public Bar Bar { get; } }"),
            With("[JSInvokable] public static Bar TakeFooGiveBar (Foo f) => default;"),
            With("[JSInvokable] public static Foo TakeBarGiveFoo (Bar b) => default;"),
            With("[JSInvokable] public static Far TakeAllGiveFar (Foo f, Bar b, Far ff) => default;"));
        Task.Execute();
        Assert.Single(Matches("export interface Foo"));
        Assert.Single(Matches("export class Bar"));
        Assert.Single(Matches("export class Far"));
    }
}
