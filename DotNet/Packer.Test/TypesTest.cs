using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Packer.Test;

public class TypesTest : ContentTest
{
    protected override string TestedContent => Data.GeneratedTypes;

    [Fact]
    public void TypesContainInteropAndBootContentWithoutImport ()
    {
        Task.Execute();
        Contains(MockData.InteropTypeContent);
        Contains(MockData.BootTypeContent.Split('\n')[1]);
    }

    [Fact]
    public void TypesDontContainOtherContent ()
    {
        File.WriteAllText(Path.Combine(Data.JSDir, "other.d.ts"), "other");
        Task.Execute();
        Assert.DoesNotContain("other", Data.GeneratedTypes);
    }

    [Fact]
    public void WhenTypeResolveFailsExceptionIsThrown ()
    {
        File.Delete(Path.Combine(Data.JSDir, "interop.d.ts"));
        Assert.Throws<PackerException>(() => Task.Execute());
    }

    [Fact]
    public void TypesExportNamespace ()
    {
        Data.AddAssembly("Foo", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("export namespace Foo {");
    }

    [Fact]
    public void DotsInSpaceArePreserved ()
    {
        Data.AddAssembly("Foo.Bar.Nya", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("export namespace Foo.Bar.Nya {");
    }

    [Fact]
    public void FunctionDeclarationIsExportedForInvokableMethod ()
    {
        Data.AddAssembly("Foo", "[JSInvokable] public static void Foo () { }");
        Task.Execute();
        Contains("export namespace Foo {\n    export function Foo(): void;\n}");
    }

    [Fact]
    public void AssignableVariableIsExportedForFunctionCallback ()
    {
        Data.AddAssembly("Foo", "[JSFunction] public static void OnFoo () { }");
        Task.Execute();
        Contains("export namespace Foo {\n    export let OnFoo: () => void;\n}");
    }

    [Fact]
    public void MembersFromSameSpaceAreDeclaredUnderSameSpace ()
    {
        Data.AddAssembly(
            new MockClass { Space = "Foo", Name = "Foo" },
            new MockClass { Space = "Foo", Lines = new[] { "[JSInvokable] public static Foo GetFoo () => default;" } }
        );
        Task.Execute();
        Contains("export namespace Foo {\n    export class Foo {\n    }\n}");
        Contains("export namespace Foo {\n    export function GetFoo(): Foo.Foo;\n}");
    }

    [Fact]
    public void MembersFromDifferentSpacesAreDeclaredUnderRespectiveSpaces ()
    {
        Data.AddAssembly(
            new MockClass { Space = "Foo", Name = "Foo" },
            new MockClass { Space = "Bar", Lines = new[] { "[JSInvokable] public static Foo.Foo GetFoo () => default;" } }
        );
        Task.Execute();
        Contains("export namespace Foo {\n    export class Foo {\n    }\n}");
        Contains("export namespace Bar {\n    export function GetFoo(): Foo.Foo;\n}");
    }

    [Fact]
    public void MultipleSpacesAreDeclaredFromNewLine ()
    {
        Data.AddAssembly("a", "[JSInvokable] public static void Foo () { }");
        Data.AddAssembly("b", "[JSInvokable] public static void Bar () { }");
        Task.Execute();
        Contains("\nexport namespace b");
    }

    [Fact]
    public void DifferentSpacesWithSameRootAreDeclaredAsDifferentSpaces ()
    {
        Data.AddAssembly("Nya.Bar", "[JSInvokable] public static void Fun () { }");
        Data.AddAssembly("Nya.Foo", "[JSInvokable] public static void Foo () { }");
        Task.Execute();
        Contains("export namespace Nya.Bar {\n    export function Fun(): void;\n}");
        Contains("export namespace Nya.Foo {\n    export function Foo(): void;\n}");
    }

    [Fact]
    public void NumericsTranslatedToNumber ()
    {
        var nums = new[] { "byte", "sbyte", "ushort", "uint", "ulong", "short", "int", "long", "decimal", "double", "float" };
        var csArgs = string.Join(", ", nums.Select(n => $"{n} v{Array.IndexOf(nums, n)}"));
        var tsArgs = string.Join(", ", nums.Select(n => $"v{Array.IndexOf(nums, n)}: number"));
        Data.AddAssemblyTemp(new[] { $"[JSInvokable] public static void Num ({csArgs}) {{}}" });
        Task.Execute();
        Contains($"Num({tsArgs})");
    }

    [Fact]
    public void TaskTranslatedToPromise ()
    {
        Data.AddAssemblyTemp(
            "[JSInvokable] public static Task<bool> AsyBool () => default;",
            "[JSInvokable] public static ValueTask AsyVoid () => default;"
        );
        Task.Execute();
        Contains("AsyBool(): Promise<boolean>");
        Contains("AsyVoid(): Promise<void>");
    }

    [Fact]
    public void CharAndStringTranslatedToString ()
    {
        Data.AddAssemblyTemp(new[] { "[JSInvokable] public static void Cha (char c, string s) {}" });
        Task.Execute();
        Contains("Cha(c: string, s: string): void");
    }

    [Fact]
    public void BoolTranslatedToBoolean ()
    {
        Data.AddAssemblyTemp(new[] { "[JSInvokable] public static void Boo (bool b) {}" });
        Task.Execute();
        Contains("Boo(b: boolean): void");
    }

    [Fact]
    public void DateTimeTranslatedToDate ()
    {
        Data.AddAssemblyTemp(new[] { "[JSInvokable] public static void Doo (DateTime time) {}" });
        Task.Execute();
        Contains("Doo(time: Date): void");
    }

    [Fact]
    public void ListAndArrayTranslatedToArray ()
    {
        Data.AddAssemblyTemp(new[] { "[JSInvokable] public static List<string> Goo (DateTime[] d) => default;" });
        Task.Execute();
        Contains("Goo(d: Array<Date>): Array<string>");
    }

    [Fact]
    public void DefinitionIsGeneratedForObjectType ()
    {
        Data.AddAssembly("Space",
            "public class Foo { public string S { get; set; } public int I { get; set; } }",
            "[JSInvokable] public static Foo Method (Foo t) => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*s: string;\s*i: number;\s*}");
        Contains("Method(t: Space.Foo): Space.Foo");
    }

    [Fact]
    public void DefinitionIsGeneratedForInterfaceAndImplementation ()
    {
        Data.AddAssembly("Space",
            "public interface Base { Base Foo { get; } void Bar (Base b); }",
            "public class Derived : Base { public Base Foo { get; } public void Bar (Base b) {} }",
            "[JSInvokable] public static Derived Method (Base b) => default;"
        );
        Task.Execute();
        Matches(@"export interface Base {\s*foo: Space.Base;\s*}");
        Matches(@"export class Derived implements Space.Base {\s*foo: Space.Base;\s*}");
        Contains("Method(b: Space.Base): Space.Derived");
    }

    [Fact]
    public void DefinitionIsGeneratedForTypeWithListProperty ()
    {
        Data.AddAssembly("Space",
            "public interface Item { }",
            "public class Container { public List<Item> Items { get; } }",
            "[JSInvokable] public static Container Combine (List<Item> items) => default;"
        );
        Task.Execute();
        Matches(@"export interface Item {\s*}");
        Matches(@"export class Container {\s*items: Array<Space.Item>;\s*}");
        Contains("Combine(items: Array<Space.Item>): Space.Container");
    }

    [Fact]
    public void CanCrawlCustomTypes ()
    {
        Data.AddAssembly("Space",
            "public enum Nyam { A, B }",
            "public class Foo { public Nyam Nyam { get; } }",
            "public class Bar : Foo { }",
            "public class Barrel { public List<Bar> Bars { get; } }",
            "[JSInvokable] public static Barrel GetBarrel () => default;"
        );
        Task.Execute();
        Matches(@"export enum Nyam {\s*A,\s*B\s*}");
        Matches(@"export class Foo {\s*nyam: Space.Nyam;\s*}");
        Matches(@"export class Bar extends Space.Foo {\s*}");
    }

    [Fact]
    public void OtherTypesAreTranslatedToAny ()
    {
        Data.AddAssemblyTemp(new[] { "[JSInvokable] public static DBNull Method (DBNull t) => default;" });
        Task.Execute();
        Contains("Method(t: any): any");
    }

    [Fact]
    public void StaticPropertiesAreNotIncluded ()
    {
        Data.AddAssemblyTemp(
            "public class Foo { public static string Soo { get; } }",
            "[JSInvokable] public static Foo Bar () => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*}");
    }

    [Fact]
    public void ExpressionPropertiesAreNotIncluded ()
    {
        Data.AddAssemblyTemp(
            "public class Foo { public bool Boo => true; }",
            "[JSInvokable] public static Foo Bar () => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*}");
    }

    [Fact]
    public void NullablePropertiesHaveOptionalModificator ()
    {
        Data.AddAssembly("Space",
            "public class Foo { public bool? Bool { get; } }",
            "public class Bar { public Foo? Foo { get; } }",
            "[JSInvokable] public static Foo FooBar (Bar bar) => default;"
        );
        Task.Execute();
        Matches(@"export class Foo {\s*bool\?: boolean;\s*}");
        Matches(@"export class Bar {\s*foo\?: Space.Foo;\s*}");
    }

    [Fact]
    public void NullableEnumsAreCrawled ()
    {
        Data.AddAssembly("Space",
            "public enum Foo { A, B }",
            "public class Bar { public Foo? Foo { get; } }",
            "[JSInvokable] public static Bar GetBar () => default;"
        );
        Task.Execute();
        Matches(@"export enum Foo {\s*A,\s*B\s*}");
        Matches(@"export class Bar {\s*foo\?: Space.Foo;\s*}");
    }

    [Fact]
    public void WhenTypeReferencedMultipleTimesItsDeclaredOnlyOnce ()
    {
        Data.AddAssemblyTemp(
            "public interface Foo { }",
            "public class Bar: Foo { public Foo Foo { get; } }",
            "public class Far: Bar { public Bar Bar { get; } }",
            "[JSInvokable] public static Bar TakeFooGiveBar (Foo f) => default;",
            "[JSInvokable] public static Foo TakeBarGiveFoo (Bar b) => default;",
            "[JSInvokable] public static Far TakeAllGiveFar (Foo f, Bar b, Far ff) => default;"
        );
        Task.Execute();
        Assert.Single(Matches("export interface Foo"));
        Assert.Single(Matches("export class Bar"));
        Assert.Single(Matches("export class Far"));
    }

    [Fact]
    public void WhenInvalidNamespacePatternExceptionIsThrown ()
    {
        // Data.AddAssemblyWithClass("[JSInvokable] public static void Foo () { }");
        // Task.NamespacePattern = "?";
        // Assert.Throws<PackerException>(() => Task.Execute());
    }

    [Fact]
    public void NamespacePatternOverrideDeclaredSpaces ()
    {
        // Data.AddAssembly("company.product.asm.dll",
        //     "public class Foo { }",
        //     "[JSInvokable] public static Foo GetFoo () => default;"
        // );
        // Task.NamespacePattern = @"company\.product\.(\S+)=>$1";
        // Task.Execute();
        // Contains("export namespace asm {\n    export class Foo {\n    }\n}");
        // Contains("export namespace company.product.asm {\n    export function GetFoo(): asm.Foo;\n}");
    }
}
